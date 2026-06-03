import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { apiErrorMessage } from '../../core/http/api-error-message';
import { CaseApiService } from '../cases/case-api.service';
import { ApprovalQueueItem } from '../cases/case.models';

@Component({
  selector: 'app-approvals',
  imports: [DatePipe, FormsModule, RouterLink],
  template: `
    <section class="approvals-page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Approvals</p>
          <h1>Approval Queue</h1>
        </div>
        @if (canManageApprovals()) {
          <button type="button" (click)="loadApprovals()" [disabled]="loading()">
            {{ loading() ? 'Refreshing...' : 'Refresh' }}
          </button>
        }
      </header>

      @if (!canManageApprovals()) {
        <div class="state error" role="alert">You do not have access to approval queue actions.</div>
      } @else if (loading()) {
        <div class="state">Loading pending approvals...</div>
      } @else if (errorMessage()) {
        <div class="state error" role="alert">{{ errorMessage() }}</div>
      } @else if (approvals().length === 0) {
        <div class="state">No pending approvals.</div>
      } @else {
        <div class="approval-list">
          @for (approval of approvals(); track approval.id) {
            <article class="approval-item">
              <header>
                <div>
                  <a [routerLink]="['/cases', approval.caseId]">{{ approval.caseNumber }}</a>
                  <h2>{{ approval.caseTitle }}</h2>
                </div>
                <div class="badge-stack">
                  <span class="badge neutral">{{ approval.priority }}</span>
                  <span class="badge neutral">{{ approval.caseStatus }}</span>
                  @if (approval.isOverdue) {
                    <span class="badge overdue">Overdue</span>
                  }
                </div>
              </header>

              <dl>
                <div>
                  <dt>Requested by</dt>
                  <dd>{{ approval.requestedBy.displayName }}</dd>
                </div>
                <div>
                  <dt>Requested</dt>
                  <dd>{{ approval.requestedAtUtc | date: 'medium' }}</dd>
                </div>
                <div>
                  <dt>Assigned to</dt>
                  <dd>{{ approval.assignedTo?.displayName ?? 'Unassigned' }}</dd>
                </div>
                <div>
                  <dt>SLA due</dt>
                  <dd>{{ approval.dueAtUtc | date: 'medium' }}</dd>
                </div>
                <div class="full">
                  <dt>Request reason</dt>
                  <dd>{{ approval.requestReason }}</dd>
                </div>
              </dl>

              <label>
                <span>Reject reason</span>
                <textarea
                  rows="2"
                  maxlength="1000"
                  [ngModel]="rejectReasonFor(approval.id)"
                  (ngModelChange)="setRejectReason(approval.id, $event)"
                ></textarea>
              </label>

              @if (rowMessages()[approval.id]; as message) {
                <p class="form-success" role="status">{{ message }}</p>
              }
              @if (rowErrors()[approval.id]; as message) {
                <p class="form-error" role="alert">{{ message }}</p>
              }

              <div class="form-actions">
                <button
                  type="button"
                  class="primary"
                  [disabled]="actingApprovalId() === approval.id"
                  (click)="approve(approval)"
                >
                  {{ actingApprovalId() === approval.id ? 'Saving...' : 'Approve' }}
                </button>
                <button
                  type="button"
                  [disabled]="actingApprovalId() === approval.id"
                  (click)="reject(approval)"
                >
                  Reject
                </button>
              </div>
            </article>
          }
        </div>
      }
    </section>
  `,
  styles: `
    :host {
      display: block;
    }

    .approvals-page {
      display: grid;
      gap: 1rem;
    }

    .page-header,
    .approval-item,
    .state {
      border: 1px solid #dbe3eb;
      border-radius: 8px;
      background: #ffffff;
    }

    .page-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      padding: 1.35rem 1.5rem;
    }

    .eyebrow,
    h1,
    h2,
    p,
    dl,
    dd {
      margin: 0;
    }

    .eyebrow {
      color: #0f766e;
      font-size: 0.8rem;
      font-weight: 750;
      text-transform: uppercase;
    }

    h1 {
      color: #172026;
      font-size: 1.7rem;
      line-height: 1.2;
    }

    h2 {
      color: #172026;
      font-size: 1rem;
    }

    .approval-list {
      display: grid;
      gap: 1rem;
    }

    .approval-item {
      display: grid;
      gap: 1rem;
      padding: 1rem;
    }

    .approval-item header,
    .badge-stack,
    .form-actions {
      display: flex;
      align-items: flex-start;
      gap: 0.6rem;
      justify-content: space-between;
    }

    .badge-stack,
    .form-actions {
      align-items: center;
      justify-content: flex-start;
      flex-wrap: wrap;
    }

    a {
      color: #0f766e;
      font-weight: 800;
      text-decoration: none;
    }

    dl {
      display: grid;
      grid-template-columns: repeat(4, minmax(0, 1fr));
      gap: 0.85rem;
    }

    .full {
      grid-column: 1 / -1;
    }

    dt,
    label span {
      color: #4a5661;
      font-size: 0.78rem;
      font-weight: 800;
      text-transform: uppercase;
    }

    dd {
      color: #26323d;
      font-weight: 700;
      line-height: 1.45;
      margin-top: 0.25rem;
    }

    label {
      display: grid;
      gap: 0.55rem;
    }

    textarea,
    button {
      font: inherit;
    }

    textarea {
      width: 100%;
      border: 1px solid #c6d0d9;
      border-radius: 6px;
      color: #172026;
      padding: 0.65rem 0.7rem;
      resize: vertical;
    }

    button {
      min-height: 2.35rem;
      border: 1px solid #c6d0d9;
      border-radius: 6px;
      background: #ffffff;
      color: #26323d;
      cursor: pointer;
      font-weight: 750;
      padding: 0 0.8rem;
    }

    button:disabled {
      cursor: not-allowed;
      opacity: 0.6;
    }

    .primary {
      border-color: #0f766e;
      background: #0f766e;
      color: #ffffff;
    }

    .badge {
      display: inline-flex;
      align-items: center;
      min-height: 1.6rem;
      border-radius: 999px;
      font-size: 0.78rem;
      font-weight: 800;
      padding: 0.2rem 0.55rem;
      white-space: nowrap;
    }

    .neutral {
      background: #edf2f7;
      color: #4a5661;
    }

    .overdue,
    .error,
    .form-error {
      border-color: #fecaca;
      background: #fef2f2;
      color: #991b1b;
    }

    .overdue {
      background: #fee2e2;
    }

    .form-success {
      border: 1px solid #a7f3d0;
      border-radius: 8px;
      background: #ecfdf5;
      color: #065f46;
      padding: 0.75rem;
    }

    .form-error {
      border: 1px solid #fecaca;
      border-radius: 8px;
      padding: 0.75rem;
    }

    .state {
      color: #4a5661;
      padding: 2rem;
      text-align: center;
    }

    @media (max-width: 860px) {
      .page-header,
      .approval-item header {
        align-items: stretch;
        flex-direction: column;
      }

      dl {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class ApprovalsComponent {
  private readonly api = inject(CaseApiService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly approvals = signal<ApprovalQueueItem[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly actingApprovalId = signal<string | null>(null);
  readonly rejectReasons = signal<Record<string, string>>({});
  readonly rowMessages = signal<Record<string, string>>({});
  readonly rowErrors = signal<Record<string, string>>({});
  readonly canManageApprovals = () => this.authService.hasAnyRole(['Manager', 'Admin']);

  constructor() {
    if (this.canManageApprovals()) {
      this.loadApprovals();
    } else {
      this.loading.set(false);
    }
  }

  loadApprovals(): void {
    this.loading.set(true);
    this.errorMessage.set('');
    this.api
      .getPendingApprovals(1, 50)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (result) => this.approvals.set(result.items),
        error: (error: HttpErrorResponse) => {
          this.approvals.set([]);
          this.errorMessage.set(this.errorText(error));
        },
      });
  }

  setRejectReason(approvalId: string, reason: string): void {
    this.rejectReasons.update((current) => ({ ...current, [approvalId]: reason }));
  }

  rejectReasonFor(approvalId: string): string {
    return this.rejectReasons()[approvalId] || '';
  }

  approve(approval: ApprovalQueueItem): void {
    this.decide(approval, 'approve', null);
  }

  reject(approval: ApprovalQueueItem): void {
    const reason = (this.rejectReasons()[approval.id] ?? '').trim();
    if (!reason) {
      this.rowErrors.update((current) => ({
        ...current,
        [approval.id]: 'Decision reason is required for rejection.',
      }));
      return;
    }

    this.decide(approval, 'reject', reason);
  }

  private decide(
    approval: ApprovalQueueItem,
    action: 'approve' | 'reject',
    decisionReason: string | null,
  ): void {
    this.clearRowFeedback(approval.id);
    const operation =
      action === 'approve'
        ? this.api.approveApproval(approval.id, {
            decisionReason,
            rowVersion: approval.rowVersion,
          })
        : this.api.rejectApproval(approval.id, {
            decisionReason,
            rowVersion: approval.rowVersion,
          });

    this.actingApprovalId.set(approval.id);
    operation
      .pipe(
        finalize(() => this.actingApprovalId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.rowMessages.update((current) => ({
            ...current,
            [approval.id]: action === 'approve' ? 'Closure approved.' : 'Closure rejected.',
          }));
          this.loadApprovals();
        },
        error: (error: HttpErrorResponse) => {
          this.rowErrors.update((current) => ({
            ...current,
            [approval.id]: this.errorText(error),
          }));
        },
      });
  }

  private clearRowFeedback(approvalId: string): void {
    this.rowMessages.update((current) => {
      const next = { ...current };
      delete next[approvalId];
      return next;
    });
    this.rowErrors.update((current) => {
      const next = { ...current };
      delete next[approvalId];
      return next;
    });
  }

  private errorText(error: HttpErrorResponse): string {
    return apiErrorMessage(error, 'Approval queue could not be updated. Try again.', {
      403: 'You do not have access to approval queue actions.',
      409: 'This approval was updated by another user. Please refresh.',
    });
  }
}
