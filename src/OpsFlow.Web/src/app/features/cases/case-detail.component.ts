import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { CaseApiService } from './case-api.service';
import { AnalystLookup, CaseDetail, CaseNote, CaseStatus, TimelineItem } from './case.models';

@Component({
  selector: 'app-case-detail',
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  template: `
    <section class="case-detail">
      <a class="back-link" routerLink="/cases">Back to Cases</a>

      @if (detailLoading()) {
        <div class="state">Loading case detail...</div>
      } @else if (detailError()) {
        <div class="state error" role="alert">{{ detailError() }}</div>
      } @else if (caseDetail(); as detail) {
        <header class="detail-header">
          <div>
            <p class="eyebrow">{{ detail.caseNumber }}</p>
            <h1>{{ detail.title }}</h1>
          </div>
          <div class="status-stack">
            <span class="badge neutral">{{ detail.status }}</span>
            @if (detail.isOverdue) {
              <span class="badge overdue">Overdue</span>
            }
          </div>
        </header>

        <section class="metadata" aria-label="Case metadata">
          <div>
            <span>Description</span>
            <p>{{ detail.description || 'No description provided.' }}</p>
          </div>
          <div>
            <span>Priority</span>
            <p>{{ detail.priority }}</p>
          </div>
          <div>
            <span>Case type</span>
            <p>{{ detail.caseType.name }}</p>
          </div>
          <div>
            <span>Assigned to</span>
            <p>{{ detail.assignedTo?.displayName ?? 'Unassigned' }}</p>
          </div>
          <div>
            <span>Created by</span>
            <p>{{ detail.createdBy.displayName }}</p>
          </div>
          <div>
            <span>Created</span>
            <p>{{ detail.createdAtUtc | date: 'medium' }}</p>
          </div>
          <div>
            <span>Updated</span>
            <p>{{ detail.updatedAtUtc | date: 'medium' }}</p>
          </div>
          <div>
            <span>SLA due</span>
            <p>{{ detail.dueAtUtc | date: 'medium' }}</p>
          </div>
        </section>

        @if (canAssignCases()) {
          <section class="panel assignment-panel" aria-label="Assignment">
            <header class="panel-header">
              <h2>Assignment</h2>
            </header>

            <div class="assignment-current">
              <span>Current assignee</span>
              <p>{{ detail.assignedTo?.displayName ?? 'Unassigned' }}</p>
            </div>

            @if (analystsLoading()) {
              <div class="state compact">Loading analysts...</div>
            } @else if (analystsError()) {
              <div class="state compact error" role="alert">{{ analystsError() }}</div>
            } @else {
              <form [formGroup]="assignmentForm" (ngSubmit)="submitAssignment()" class="assignment-form">
                <label>
                  <span>Analyst</span>
                  <select formControlName="assignedToUserId">
                    <option value="">Select analyst</option>
                    @for (analyst of analysts(); track analyst.id) {
                      <option [value]="analyst.id">
                        {{ analyst.displayName }} ({{ analyst.email }})
                      </option>
                    }
                  </select>
                </label>

                <label>
                  <span>Reason</span>
                  <textarea formControlName="reason" rows="3" maxlength="500"></textarea>
                </label>

                @if (assignmentValidationMessage()) {
                  <p class="form-error">{{ assignmentValidationMessage() }}</p>
                }
                @if (assignmentSaveMessage()) {
                  <p class="form-success" role="status">{{ assignmentSaveMessage() }}</p>
                }
                @if (assignmentSaveError()) {
                  <p class="form-error" role="alert">{{ assignmentSaveError() }}</p>
                }

                <div class="form-actions">
                  <button type="submit" class="primary" [disabled]="assigningCase() || analysts().length === 0">
                    {{ assigningCase() ? 'Assigning...' : assignmentButtonLabel() }}
                  </button>
                </div>
              </form>
            }
          </section>
        }

        @if (canShowStatusPanel()) {
          <section class="panel status-panel" aria-label="Status transition">
            <header class="panel-header">
              <h2>Status</h2>
            </header>

            <div class="assignment-current">
              <span>Current status</span>
              <p>{{ detail.status }}</p>
            </div>

            @if (allowedStatusTransitions().length === 0) {
              <div class="state compact">{{ statusUnavailableMessage() }}</div>
            } @else {
              <form [formGroup]="statusForm" (ngSubmit)="submitStatusUpdate()" class="status-form">
                <label>
                  <span>Next status</span>
                  <select formControlName="targetStatus">
                    <option value="">Select status</option>
                    @for (status of allowedStatusTransitions(); track status) {
                      <option [value]="status">{{ status }}</option>
                    }
                  </select>
                </label>

                <label>
                  <span>Reason</span>
                  <textarea formControlName="reason" rows="3" maxlength="1000"></textarea>
                </label>

                @if (statusValidationMessage()) {
                  <p class="form-error">{{ statusValidationMessage() }}</p>
                }
                @if (statusSaveMessage()) {
                  <p class="form-success" role="status">{{ statusSaveMessage() }}</p>
                }
                @if (statusSaveError()) {
                  <p class="form-error" role="alert">{{ statusSaveError() }}</p>
                }

                <div class="form-actions">
                  <button type="submit" class="primary" [disabled]="updatingStatus()">
                    {{ updatingStatus() ? 'Updating...' : 'Update Status' }}
                  </button>
                </div>
              </form>
            }
          </section>
        }

        <section class="content-grid">
          <section class="panel" aria-label="Notes">
            <header class="panel-header">
              <h2>Notes</h2>
            </header>

            <form [formGroup]="noteForm" (ngSubmit)="submitNote()" class="note-form">
              <label>
                <span>Add note</span>
                <textarea formControlName="body" rows="5" maxlength="2000"></textarea>
              </label>
              @if (noteValidationMessage()) {
                <p class="form-error">{{ noteValidationMessage() }}</p>
              }
              @if (noteSaveMessage()) {
                <p class="form-success" role="status">{{ noteSaveMessage() }}</p>
              }
              @if (noteSaveError()) {
                <p class="form-error" role="alert">{{ noteSaveError() }}</p>
              }
              <div class="form-actions">
                <button type="submit" class="primary" [disabled]="savingNote()">
                  {{ savingNote() ? 'Saving...' : 'Add Note' }}
                </button>
              </div>
            </form>

            @if (notesLoading()) {
              <div class="state compact">Loading notes...</div>
            } @else if (notesError()) {
              <div class="state compact error" role="alert">{{ notesError() }}</div>
            } @else if (notes().length === 0) {
              <div class="state compact">No notes have been added.</div>
            } @else {
              <ol class="notes-list">
                @for (note of notes(); track note.id) {
                  <li>
                    <p>{{ note.body }}</p>
                    <footer>
                      {{ note.createdBy.displayName }} &middot; {{ note.createdAtUtc | date: 'medium' }}
                    </footer>
                  </li>
                }
              </ol>
            }
          </section>

          <section class="panel" aria-label="Timeline">
            <header class="panel-header">
              <h2>Timeline</h2>
            </header>

            @if (timelineLoading()) {
              <div class="state compact">Loading timeline...</div>
            } @else if (timelineError()) {
              <div class="state compact error" role="alert">{{ timelineError() }}</div>
            } @else if (timeline().length === 0) {
              <div class="state compact">No timeline events have been recorded.</div>
            } @else {
              <ol class="timeline-list">
                @for (item of timeline(); track item.id) {
                  <li>
                    <div class="timeline-marker"></div>
                    <div>
                      <strong>{{ actionLabel(item.action) }}</strong>
                      <p>{{ item.description }}</p>
                      <footer>
                        {{ item.actor?.displayName ?? 'System' }} &middot; {{ item.createdAtUtc | date: 'medium' }}
                      </footer>
                    </div>
                  </li>
                }
              </ol>
            }
          </section>
        </section>
      }
    </section>
  `,
  styles: `
    :host {
      display: block;
    }

    .case-detail {
      display: grid;
      gap: 1rem;
    }

    .back-link {
      color: #0f766e;
      font-weight: 800;
      text-decoration: none;
      width: fit-content;
    }

    .detail-header,
    .metadata,
    .panel,
    .state {
      border: 1px solid #dbe3eb;
      border-radius: 8px;
      background: #ffffff;
    }

    .detail-header {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      padding: 1.35rem 1.5rem;
    }

    .eyebrow,
    h1,
    h2,
    p {
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

    .status-stack,
    .form-actions {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    .metadata {
      display: grid;
      grid-template-columns: repeat(4, minmax(0, 1fr));
      gap: 1rem;
      padding: 1rem;
    }

    .metadata div:first-child {
      grid-column: 1 / -1;
    }

    .metadata span,
    label span {
      color: #4a5661;
      font-size: 0.78rem;
      font-weight: 800;
      text-transform: uppercase;
    }

    .metadata p {
      color: #26323d;
      line-height: 1.45;
      margin-top: 0.25rem;
    }

    .content-grid {
      display: grid;
      grid-template-columns: minmax(0, 1.15fr) minmax(20rem, 0.85fr);
      gap: 1rem;
      align-items: start;
    }

    .panel {
      display: grid;
      gap: 1rem;
      padding: 1rem;
    }

    .panel-header {
      border-bottom: 1px solid #e5ebf0;
      padding-bottom: 0.75rem;
    }

    .note-form,
    .assignment-form,
    .status-form,
    label {
      display: grid;
      gap: 0.55rem;
    }

    button,
    select,
    textarea {
      font: inherit;
    }

    button {
      min-height: 2.35rem;
      border: 1px solid #c6d0d9;
      border-radius: 6px;
      cursor: pointer;
      font-weight: 750;
      padding: 0 0.8rem;
    }

    button:disabled {
      cursor: not-allowed;
      opacity: 0.6;
    }

    select,
    textarea {
      width: 100%;
      border: 1px solid #c6d0d9;
      border-radius: 6px;
      color: #172026;
      padding: 0.65rem 0.7rem;
    }

    textarea {
      resize: vertical;
    }

    .assignment-panel,
    .status-panel {
      gap: 0.85rem;
    }

    .assignment-current {
      display: grid;
      gap: 0.25rem;
    }

    .assignment-current span {
      color: #4a5661;
      font-size: 0.78rem;
      font-weight: 800;
      text-transform: uppercase;
    }

    .assignment-current p {
      color: #26323d;
      font-weight: 750;
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

    .compact {
      border-style: dashed;
      padding: 1.25rem;
    }

    .notes-list,
    .timeline-list {
      display: grid;
      gap: 0.8rem;
      list-style: none;
      margin: 0;
      padding: 0;
    }

    .notes-list li {
      border: 1px solid #e5ebf0;
      border-radius: 8px;
      padding: 0.9rem;
    }

    .notes-list p,
    .timeline-list p {
      color: #26323d;
      line-height: 1.45;
    }

    footer {
      color: #64707a;
      font-size: 0.85rem;
      font-weight: 650;
      margin-top: 0.55rem;
    }

    .timeline-list li {
      display: grid;
      grid-template-columns: 0.85rem 1fr;
      gap: 0.75rem;
    }

    .timeline-marker {
      width: 0.7rem;
      height: 0.7rem;
      border-radius: 50%;
      background: #0f766e;
      margin-top: 0.3rem;
    }

    @media (max-width: 960px) {
      .detail-header {
        align-items: stretch;
        flex-direction: column;
      }

      .metadata,
      .content-grid {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class CaseDetailComponent {
  private readonly api = inject(CaseApiService);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);

  readonly caseDetail = signal<CaseDetail | null>(null);
  readonly analysts = signal<AnalystLookup[]>([]);
  readonly notes = signal<CaseNote[]>([]);
  readonly timeline = signal<TimelineItem[]>([]);
  readonly detailLoading = signal(true);
  readonly analystsLoading = signal(false);
  readonly notesLoading = signal(false);
  readonly timelineLoading = signal(false);
  readonly savingNote = signal(false);
  readonly assigningCase = signal(false);
  readonly updatingStatus = signal(false);
  readonly detailError = signal('');
  readonly analystsError = signal('');
  readonly notesError = signal('');
  readonly timelineError = signal('');
  readonly noteValidationMessage = signal('');
  readonly noteSaveMessage = signal('');
  readonly noteSaveError = signal('');
  readonly assignmentValidationMessage = signal('');
  readonly assignmentSaveMessage = signal('');
  readonly assignmentSaveError = signal('');
  readonly statusValidationMessage = signal('');
  readonly statusSaveMessage = signal('');
  readonly statusSaveError = signal('');
  readonly canAssignCases = computed(() => this.authService.hasAnyRole(['Manager', 'Admin']));
  readonly canShowStatusPanel = computed(() => {
    const detail = this.caseDetail();
    if (!detail) {
      return false;
    }

    if (this.authService.hasAnyRole(['Manager', 'Admin'])) {
      return true;
    }

    const currentUser = this.authService.currentUser();
    return this.authService.hasAnyRole(['Analyst']) && detail.assignedTo?.id === currentUser?.id;
  });
  readonly allowedStatusTransitions = computed<CaseStatus[]>(() => {
    const detail = this.caseDetail();
    if (!detail || !this.canShowStatusPanel()) {
      return [];
    }

    if (this.authService.hasAnyRole(['Manager', 'Admin'])) {
      return this.managerTransitions(detail);
    }

    return this.analystTransitions(detail.status);
  });

  readonly noteForm = this.fb.nonNullable.group({
    body: ['', [Validators.required, Validators.maxLength(2000)]],
  });

  readonly assignmentForm = this.fb.nonNullable.group({
    assignedToUserId: ['', [Validators.required]],
    reason: ['', [Validators.required, Validators.maxLength(500)]],
  });

  readonly statusForm = this.fb.nonNullable.group({
    targetStatus: ['', [Validators.required]],
    reason: ['', [Validators.required, Validators.maxLength(1000)]],
  });

  private readonly caseId = this.route.snapshot.paramMap.get('id') ?? '';

  constructor() {
    this.loadDetail();
    this.loadNotesAndTimeline();
    if (this.canAssignCases()) {
      this.loadAnalysts();
    }
  }

  submitNote(): void {
    this.noteValidationMessage.set('');
    this.noteSaveMessage.set('');
    this.noteSaveError.set('');

    const body = this.noteForm.controls.body.value.trim();
    if (!body) {
      this.noteValidationMessage.set('Note body is required.');
      return;
    }

    if (body.length > 2000) {
      this.noteValidationMessage.set('Note body must be 2000 characters or fewer.');
      return;
    }

    this.savingNote.set(true);
    this.api
      .addNote(this.caseId, body)
      .pipe(
        finalize(() => this.savingNote.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.noteForm.reset({ body: '' });
          this.noteSaveMessage.set('Note added.');
          this.loadNotesAndTimeline();
        },
        error: () => {
          this.noteSaveError.set('Note could not be added. Try again.');
        },
      });
  }

  submitAssignment(): void {
    this.assignmentValidationMessage.set('');
    this.assignmentSaveMessage.set('');
    this.assignmentSaveError.set('');

    const assignedToUserId = this.assignmentForm.controls.assignedToUserId.value;
    const reason = this.assignmentForm.controls.reason.value.trim();
    const detail = this.caseDetail();

    if (!assignedToUserId) {
      this.assignmentValidationMessage.set('Analyst is required.');
      return;
    }

    if (!reason) {
      this.assignmentValidationMessage.set('Assignment reason is required.');
      return;
    }

    if (reason.length > 500) {
      this.assignmentValidationMessage.set('Assignment reason must be 500 characters or fewer.');
      return;
    }

    this.assigningCase.set(true);
    this.api
      .assignCase(this.caseId, {
        assignedToUserId,
        reason,
        rowVersion: detail?.rowVersion,
      })
      .pipe(
        finalize(() => this.assigningCase.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updatedDetail) => {
          this.caseDetail.set(updatedDetail);
          this.assignmentForm.reset({ assignedToUserId: '', reason: '' });
          this.assignmentSaveMessage.set('Assignment updated.');
          this.loadDetail();
          this.loadNotesAndTimeline();
        },
        error: (error: HttpErrorResponse) => {
          this.assignmentSaveError.set(this.assignmentErrorMessage(error));
        },
      });
  }

  submitStatusUpdate(): void {
    this.statusValidationMessage.set('');
    this.statusSaveMessage.set('');
    this.statusSaveError.set('');

    const detail = this.caseDetail();
    const targetStatus = this.statusForm.controls.targetStatus.value as CaseStatus | '';
    const reason = this.statusForm.controls.reason.value.trim();

    if (!detail) {
      this.statusValidationMessage.set('Case detail is required.');
      return;
    }

    if (!targetStatus) {
      this.statusValidationMessage.set('Next status is required.');
      return;
    }

    if (!reason) {
      this.statusValidationMessage.set('Status reason is required.');
      return;
    }

    if (reason.length > 1000) {
      this.statusValidationMessage.set('Status reason must be 1000 characters or fewer.');
      return;
    }

    this.updatingStatus.set(true);
    this.api
      .updateStatus(this.caseId, {
        targetStatus,
        reason,
        rowVersion: detail.rowVersion,
      })
      .pipe(
        finalize(() => this.updatingStatus.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updatedDetail) => {
          this.caseDetail.set(updatedDetail);
          this.statusForm.reset({ targetStatus: '', reason: '' });
          this.statusSaveMessage.set('Status updated.');
          this.loadDetail();
          this.loadNotesAndTimeline();
        },
        error: (error: HttpErrorResponse) => {
          this.statusSaveError.set(this.statusErrorMessage(error));
        },
      });
  }

  actionLabel(action: TimelineItem['action']): string {
    if (action === 'CaseCreated') {
      return 'Case Created';
    }

    if (action === 'Assigned') {
      return 'Assigned';
    }

    if (action === 'StatusChanged') {
      return 'Status Changed';
    }

    if (action === 'CaseReopened') {
      return 'Case Reopened';
    }

    return 'Note Added';
  }

  private loadDetail(): void {
    this.detailLoading.set(true);
    this.detailError.set('');
    this.api
      .getCase(this.caseId)
      .pipe(
        finalize(() => this.detailLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (detail) => this.caseDetail.set(detail),
        error: (error: HttpErrorResponse) => {
          this.caseDetail.set(null);
          this.detailError.set(this.friendlyAccessMessage(error));
        },
      });
  }

  private loadAnalysts(): void {
    this.analystsLoading.set(true);
    this.analystsError.set('');
    this.api
      .getAnalysts()
      .pipe(
        finalize(() => this.analystsLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (analysts) => this.analysts.set(analysts),
        error: () => {
          this.analysts.set([]);
          this.analystsError.set('Analysts could not be loaded. Try again.');
        },
      });
  }

  private loadNotesAndTimeline(): void {
    this.notesLoading.set(true);
    this.timelineLoading.set(true);
    this.notesError.set('');
    this.timelineError.set('');
    forkJoin({
      notes: this.api.getNotes(this.caseId),
      timeline: this.api.getTimeline(this.caseId),
    })
      .pipe(
        finalize(() => {
          this.notesLoading.set(false);
          this.timelineLoading.set(false);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ notes, timeline }) => {
          this.notes.set(notes);
          this.timeline.set(timeline);
        },
        error: (error: HttpErrorResponse) => {
          const message = this.friendlyAccessMessage(error);
          this.notes.set([]);
          this.timeline.set([]);
          this.notesError.set(message);
          this.timelineError.set(message);
        },
      });
  }

  private friendlyAccessMessage(error: HttpErrorResponse): string {
    if (error.status === 404) {
      return 'Case was not found.';
    }

    if (error.status === 403) {
      return 'You do not have access to this case.';
    }

    return 'Case information could not be loaded. Try again.';
  }

  assignmentButtonLabel(): string {
    return this.caseDetail()?.assignedTo ? 'Reassign' : 'Assign';
  }

  private assignmentErrorMessage(error: HttpErrorResponse): string {
    const serverMessage = error.error?.message;
    if (typeof serverMessage === 'string' && serverMessage.trim()) {
      return serverMessage;
    }

    if (error.status === 403) {
      return 'You do not have permission to assign cases.';
    }

    if (error.status === 404) {
      return 'Case was not found.';
    }

    return 'Assignment could not be saved. Try again.';
  }

  private statusErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 409) {
      return 'This case was updated by another user. Please refresh.';
    }

    const serverMessage = error.error?.message;
    if (typeof serverMessage === 'string' && serverMessage.trim()) {
      return serverMessage;
    }

    if (error.status === 403) {
      return 'You do not have permission to update this case status.';
    }

    if (error.status === 404) {
      return 'Case was not found.';
    }

    return 'Status could not be updated. Try again.';
  }

  private managerTransitions(detail: CaseDetail): CaseStatus[] {
    switch (detail.status) {
      case 'Assigned':
        return ['InReview', 'WaitingInfo'];
      case 'InReview':
        return ['WaitingInfo', 'Resolved'];
      case 'WaitingInfo':
        return ['InReview', 'Resolved'];
      case 'Resolved':
        return detail.priority === 'Low' || detail.priority === 'Medium' ? ['Closed'] : [];
      case 'Closed':
        return ['Reopened'];
      case 'Reopened':
        return ['InReview', 'WaitingInfo'];
      default:
        return [];
    }
  }

  private analystTransitions(status: CaseStatus): CaseStatus[] {
    switch (status) {
      case 'Assigned':
        return ['InReview', 'WaitingInfo'];
      case 'InReview':
        return ['WaitingInfo', 'Resolved'];
      case 'WaitingInfo':
        return ['InReview', 'Resolved'];
      case 'Reopened':
        return ['InReview'];
      default:
        return [];
    }
  }

  statusUnavailableMessage(): string {
    const detail = this.caseDetail();
    if (detail?.status === 'Resolved' && (detail.priority === 'High' || detail.priority === 'Critical')) {
      return 'High/Critical closure requires approval workflow.';
    }

    return 'No status transitions are available.';
  }
}
