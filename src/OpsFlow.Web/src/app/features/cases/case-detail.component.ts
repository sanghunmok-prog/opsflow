import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';

import { CaseApiService } from './case-api.service';
import { CaseDetail, CaseNote, TimelineItem } from './case.models';

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
    label {
      display: grid;
      gap: 0.55rem;
    }

    button,
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

    textarea {
      width: 100%;
      border: 1px solid #c6d0d9;
      border-radius: 6px;
      color: #172026;
      padding: 0.65rem 0.7rem;
      resize: vertical;
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
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);

  readonly caseDetail = signal<CaseDetail | null>(null);
  readonly notes = signal<CaseNote[]>([]);
  readonly timeline = signal<TimelineItem[]>([]);
  readonly detailLoading = signal(true);
  readonly notesLoading = signal(false);
  readonly timelineLoading = signal(false);
  readonly savingNote = signal(false);
  readonly detailError = signal('');
  readonly notesError = signal('');
  readonly timelineError = signal('');
  readonly noteValidationMessage = signal('');
  readonly noteSaveMessage = signal('');
  readonly noteSaveError = signal('');

  readonly noteForm = this.fb.nonNullable.group({
    body: ['', [Validators.required, Validators.maxLength(2000)]],
  });

  private readonly caseId = this.route.snapshot.paramMap.get('id') ?? '';

  constructor() {
    this.loadDetail();
    this.loadNotesAndTimeline();
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

  actionLabel(action: TimelineItem['action']): string {
    return action === 'CaseCreated' ? 'Case Created' : 'Note Added';
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
}
