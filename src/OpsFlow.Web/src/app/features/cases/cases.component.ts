import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { CaseApiService } from './case-api.service';
import {
  CaseListItem,
  CaseListQuery,
  CasePriority,
  CaseSortBy,
  CaseStatus,
  CaseTypeSummary,
  PagedResult,
  SortDirection,
} from './case.models';

const DEFAULT_QUERY: CaseListQuery = {
  page: 1,
  pageSize: 20,
  sortBy: 'createdAtUtc',
  sortDirection: 'desc',
  overdue: null,
};

const STATUS_OPTIONS: CaseStatus[] = [
  'New',
  'Assigned',
  'InReview',
  'WaitingInfo',
  'Resolved',
  'PendingApproval',
  'Closed',
  'Reopened',
];

const PRIORITY_OPTIONS: CasePriority[] = ['Critical', 'High', 'Medium', 'Low'];
const SORT_OPTIONS: CaseSortBy[] = ['caseNumber', 'createdAtUtc', 'dueAtUtc', 'priority', 'status'];

@Component({
  selector: 'app-cases',
  imports: [DatePipe, ReactiveFormsModule],
  template: `
    <section class="case-queue">
      <header class="page-header">
        <div>
          <p class="eyebrow">{{ isAnalyst() ? 'My Cases' : 'Cases' }}</p>
          <h1>Case Queue</h1>
        </div>
        <div class="header-actions">
          <button type="button" class="secondary" (click)="refresh()" [disabled]="loading()">
            Refresh
          </button>
          @if (canCreateCases()) {
            <button type="button" class="primary" (click)="toggleCreateForm()">
              {{ showCreateForm() ? 'Close Form' : 'Create Case' }}
            </button>
          }
        </div>
      </header>

      @if (successMessage()) {
        <p class="alert success" role="status">{{ successMessage() }}</p>
      }

      @if (showCreateForm() && canCreateCases()) {
        <section class="create-panel" aria-label="Create case">
          <form [formGroup]="createForm" (ngSubmit)="submitCreate()">
            <div class="form-grid">
              <label>
                <span>Title</span>
                <input type="text" formControlName="title" maxlength="200" />
              </label>

              <label>
                <span>Case type</span>
                <select formControlName="caseTypeId">
                  <option value="">Select case type</option>
                  @for (caseType of caseTypes(); track caseType.id) {
                    <option [value]="caseType.id">{{ caseType.name }}</option>
                  }
                </select>
              </label>

              <label>
                <span>Priority</span>
                <select formControlName="priority">
                  <option value="">Select priority</option>
                  @for (priority of priorities; track priority) {
                    <option [value]="priority">{{ priority }}</option>
                  }
                </select>
              </label>

              <label class="description">
                <span>Description</span>
                <textarea formControlName="description" rows="4" maxlength="4000"></textarea>
              </label>
            </div>

            @if (createValidationMessage()) {
              <p class="form-error">{{ createValidationMessage() }}</p>
            }

            @if (createError()) {
              <p class="form-error">{{ createError() }}</p>
            }

            <div class="form-actions">
              <button type="button" class="secondary" (click)="toggleCreateForm()">Cancel</button>
              <button type="submit" class="primary" [disabled]="creating()">Create Case</button>
            </div>
          </form>
        </section>
      }

      <form class="filters" [formGroup]="filterForm" (ngSubmit)="applyFilters()">
        <label class="search">
          <span>Search</span>
          <input type="search" formControlName="search" placeholder="Case number, title, description" />
        </label>

        <label>
          <span>Status</span>
          <select formControlName="status">
            <option value="">All statuses</option>
            @for (status of statuses; track status) {
              <option [value]="status">{{ status }}</option>
            }
          </select>
        </label>

        <label>
          <span>Priority</span>
          <select formControlName="priority">
            <option value="">All priorities</option>
            @for (priority of priorities; track priority) {
              <option [value]="priority">{{ priority }}</option>
            }
          </select>
        </label>

        <label>
          <span>Overdue</span>
          <select formControlName="overdue">
            <option value="">All</option>
            <option value="true">Overdue</option>
            <option value="false">Not overdue</option>
          </select>
        </label>

        <label>
          <span>Sort by</span>
          <select formControlName="sortBy">
            @for (sort of sorts; track sort) {
              <option [value]="sort">{{ sortLabels[sort] }}</option>
            }
          </select>
        </label>

        <label>
          <span>Direction</span>
          <select formControlName="sortDirection">
            <option value="desc">Descending</option>
            <option value="asc">Ascending</option>
          </select>
        </label>

        <div class="filter-actions">
          <button type="submit" class="primary">Apply</button>
          <button type="button" class="secondary" (click)="clearFilters()">Clear</button>
        </div>
      </form>

      @if (error()) {
        <p class="alert error" role="alert">{{ error() }}</p>
      }

      <section class="queue-table" aria-live="polite">
        @if (loading()) {
          <div class="state">Loading cases...</div>
        } @else if (cases().length === 0 && !error()) {
          <div class="state">No cases match the current filters.</div>
        } @else {
          <table>
            <thead>
              <tr>
                <th>
                  <button type="button" class="sort-button" (click)="sortBy('caseNumber')">
                    Case {{ sortIndicator('caseNumber') }}
                  </button>
                </th>
                <th>Title</th>
                <th>Type</th>
                <th>
                  <button type="button" class="sort-button" (click)="sortBy('priority')">
                    Priority {{ sortIndicator('priority') }}
                  </button>
                </th>
                <th>
                  <button type="button" class="sort-button" (click)="sortBy('status')">
                    Status {{ sortIndicator('status') }}
                  </button>
                </th>
                <th>Assignee</th>
                <th>
                  <button type="button" class="sort-button" (click)="sortBy('createdAtUtc')">
                    Created {{ sortIndicator('createdAtUtc') }}
                  </button>
                </th>
                <th>
                  <button type="button" class="sort-button" (click)="sortBy('dueAtUtc')">
                    SLA Due {{ sortIndicator('dueAtUtc') }}
                  </button>
                </th>
              </tr>
            </thead>
            <tbody>
              @for (caseItem of cases(); track caseItem.id) {
                <tr>
                  <td class="case-number">{{ caseItem.caseNumber }}</td>
                  <td>{{ caseItem.title }}</td>
                  <td>{{ caseItem.caseType.name }}</td>
                  <td>
                    <span class="badge" [class.priority-critical]="caseItem.priority === 'Critical'"
                      [class.priority-high]="caseItem.priority === 'High'"
                      [class.priority-medium]="caseItem.priority === 'Medium'"
                      [class.priority-low]="caseItem.priority === 'Low'">
                      {{ caseItem.priority }}
                    </span>
                  </td>
                  <td>
                    <span class="badge neutral">{{ caseItem.status }}</span>
                  </td>
                  <td>{{ caseItem.assignedTo?.displayName ?? 'Unassigned' }}</td>
                  <td>{{ caseItem.createdAtUtc | date: 'mediumDate' }}</td>
                  <td>
                    <span class="due-date">{{ caseItem.dueAtUtc | date: 'short' }}</span>
                    @if (caseItem.isOverdue) {
                      <span class="badge overdue">Overdue</span>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        }
      </section>

      <footer class="pagination">
        <span>{{ pageSummary() }}</span>
        <div class="pagination-actions">
          <button
            type="button"
            class="secondary"
            (click)="goToPage(query().page - 1)"
            [disabled]="loading() || query().page <= 1"
          >
            Previous
          </button>
          <button
            type="button"
            class="secondary"
            (click)="goToPage(query().page + 1)"
            [disabled]="loading() || query().page >= totalPages()"
          >
            Next
          </button>
        </div>
      </footer>
    </section>
  `,
  styles: `
    :host {
      display: block;
    }

    .case-queue {
      display: grid;
      gap: 1rem;
    }

    .page-header,
    .filters,
    .create-panel,
    .queue-table,
    .pagination {
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

    .header-actions,
    .form-actions,
    .filter-actions,
    .pagination-actions {
      display: flex;
      gap: 0.65rem;
      align-items: center;
      flex-wrap: wrap;
    }

    button,
    input,
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

    .primary {
      border-color: #0f766e;
      background: #0f766e;
      color: #ffffff;
    }

    .secondary {
      background: #ffffff;
      color: #26323d;
    }

    .filters,
    .create-panel form {
      display: grid;
      gap: 1rem;
      padding: 1rem;
    }

    .filters {
      grid-template-columns: minmax(16rem, 1.5fr) repeat(5, minmax(8rem, 1fr)) auto;
      align-items: end;
    }

    .form-grid {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 1rem;
    }

    label {
      display: grid;
      gap: 0.35rem;
      color: #4a5661;
      font-size: 0.85rem;
      font-weight: 750;
    }

    input,
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

    .description {
      grid-column: 1 / -1;
    }

    .alert,
    .form-error {
      border-radius: 8px;
      padding: 0.8rem 1rem;
      line-height: 1.45;
    }

    .success {
      border: 1px solid #a7f3d0;
      background: #ecfdf5;
      color: #065f46;
    }

    .error,
    .form-error {
      border: 1px solid #fecaca;
      background: #fef2f2;
      color: #991b1b;
    }

    .queue-table {
      overflow-x: auto;
    }

    table {
      width: 100%;
      border-collapse: collapse;
      min-width: 980px;
    }

    th,
    td {
      border-bottom: 1px solid #e5ebf0;
      padding: 0.85rem 0.9rem;
      text-align: left;
      vertical-align: top;
    }

    th {
      background: #f8fafc;
      color: #4a5661;
      font-size: 0.8rem;
      text-transform: uppercase;
    }

    td {
      color: #26323d;
      line-height: 1.4;
    }

    .sort-button {
      min-height: auto;
      border: 0;
      background: transparent;
      color: inherit;
      padding: 0;
      text-align: left;
      text-transform: inherit;
    }

    .case-number {
      color: #0f766e;
      font-weight: 800;
      white-space: nowrap;
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

    .priority-critical {
      background: #fee2e2;
      color: #991b1b;
    }

    .priority-high {
      background: #ffedd5;
      color: #9a3412;
    }

    .priority-medium {
      background: #fef3c7;
      color: #92400e;
    }

    .priority-low,
    .neutral {
      background: #edf2f7;
      color: #4a5661;
    }

    .overdue {
      margin-left: 0.4rem;
      background: #fee2e2;
      color: #991b1b;
    }

    .due-date {
      white-space: nowrap;
    }

    .state {
      color: #4a5661;
      padding: 2rem;
      text-align: center;
    }

    .pagination {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      padding: 1rem;
      color: #4a5661;
      font-weight: 700;
    }

    @media (max-width: 960px) {
      .page-header,
      .pagination {
        align-items: stretch;
        flex-direction: column;
      }

      .filters,
      .form-grid {
        grid-template-columns: 1fr;
      }

      .filter-actions,
      .header-actions,
      .pagination-actions {
        width: 100%;
      }

      .filter-actions button,
      .header-actions button,
      .pagination-actions button {
        flex: 1;
      }
    }
  `,
})
export class CasesComponent {
  private readonly api = inject(CaseApiService);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);

  readonly statuses = STATUS_OPTIONS;
  readonly priorities = PRIORITY_OPTIONS;
  readonly sorts = SORT_OPTIONS;
  readonly sortLabels: Record<CaseSortBy, string> = {
    caseNumber: 'Case number',
    createdAtUtc: 'Created date',
    dueAtUtc: 'SLA due date',
    priority: 'Priority',
    status: 'Status',
  };

  readonly query = signal<CaseListQuery>(DEFAULT_QUERY);
  readonly cases = signal<CaseListItem[]>([]);
  readonly totalCount = signal(0);
  readonly totalPages = signal(1);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly showCreateForm = signal(false);
  readonly creating = signal(false);
  readonly createError = signal('');
  readonly createValidationMessage = signal('');
  readonly successMessage = signal('');
  readonly caseTypes = signal<CaseTypeSummary[]>([]);

  readonly isAnalyst = computed(() => this.authService.hasRole('Analyst'));
  readonly canCreateCases = computed(() => this.authService.hasAnyRole(['Manager', 'Admin']));
  readonly pageSummary = computed(() => {
    const totalCount = this.totalCount();
    const query = this.query();
    if (totalCount === 0) {
      return 'No cases';
    }

    const start = (query.page - 1) * query.pageSize + 1;
    const end = Math.min(query.page * query.pageSize, totalCount);
    return `${start}-${end} of ${totalCount} cases`;
  });

  readonly filterForm = this.fb.nonNullable.group({
    search: [''],
    status: [''],
    priority: [''],
    overdue: [''],
    sortBy: ['createdAtUtc'],
    sortDirection: ['desc'],
  });

  readonly createForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(4000)]],
    caseTypeId: ['', Validators.required],
    priority: ['', Validators.required],
  });

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const query = this.queryFromParams(params);
      this.query.set(query);
      this.patchFilterForm(query);
      this.loadCases(query);
    });
  }

  refresh(): void {
    this.loadCases(this.query());
  }

  applyFilters(): void {
    const value = this.filterForm.getRawValue();
    this.navigateWithQuery({
      ...this.query(),
      page: 1,
      search: value.search,
      status: value.status as CaseStatus | '',
      priority: value.priority as CasePriority | '',
      overdue: this.parseOverdue(value.overdue),
      sortBy: value.sortBy as CaseSortBy,
      sortDirection: value.sortDirection as SortDirection,
    });
  }

  clearFilters(): void {
    this.filterForm.reset({
      search: '',
      status: '',
      priority: '',
      overdue: '',
      sortBy: DEFAULT_QUERY.sortBy,
      sortDirection: DEFAULT_QUERY.sortDirection,
    });
    this.navigateWithQuery(DEFAULT_QUERY);
  }

  sortBy(sortBy: CaseSortBy): void {
    const current = this.query();
    const nextDirection: SortDirection =
      current.sortBy === sortBy && current.sortDirection === 'desc' ? 'asc' : 'desc';
    this.navigateWithQuery({ ...current, page: 1, sortBy, sortDirection: nextDirection });
  }

  sortIndicator(sortBy: CaseSortBy): string {
    const current = this.query();
    if (current.sortBy !== sortBy) {
      return '';
    }

    return current.sortDirection === 'asc' ? '▲' : '▼';
  }

  goToPage(page: number): void {
    const boundedPage = Math.min(Math.max(page, 1), this.totalPages());
    this.navigateWithQuery({ ...this.query(), page: boundedPage });
  }

  toggleCreateForm(): void {
    this.showCreateForm.update((visible) => !visible);
    this.createError.set('');
    this.createValidationMessage.set('');

    if (this.showCreateForm() && this.caseTypes().length === 0) {
      this.loadCaseTypes();
    }
  }

  submitCreate(): void {
    this.createError.set('');
    this.createValidationMessage.set('');
    this.successMessage.set('');

    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      this.createValidationMessage.set('Title, case type, and priority are required.');
      return;
    }

    const request = this.createForm.getRawValue();
    this.creating.set(true);
    this.api
      .createCase({
        title: request.title.trim(),
        description: request.description.trim(),
        caseTypeId: request.caseTypeId,
        priority: request.priority as CasePriority,
      })
      .pipe(
        finalize(() => this.creating.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (created) => {
          this.successMessage.set(`Created ${created.caseNumber}.`);
          this.showCreateForm.set(false);
          this.createForm.reset({ title: '', description: '', caseTypeId: '', priority: '' });
          this.refresh();
        },
        error: () => {
          this.createError.set('Case could not be created. Check the fields and try again.');
        },
      });
  }

  private loadCases(query: CaseListQuery): void {
    this.loading.set(true);
    this.error.set('');
    this.api
      .getCases(query)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (result: PagedResult<CaseListItem>) => {
          this.cases.set(result.items);
          this.totalCount.set(result.totalCount);
          this.totalPages.set(Math.max(result.totalPages, 1));
        },
        error: () => {
          this.cases.set([]);
          this.totalCount.set(0);
          this.totalPages.set(1);
          this.error.set('Cases could not be loaded. Try refreshing the queue.');
        },
      });
  }

  private loadCaseTypes(): void {
    this.api
      .getCaseTypes()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (caseTypes) => this.caseTypes.set(caseTypes),
        error: () => this.createError.set('Case types could not be loaded. Try again.'),
      });
  }

  private navigateWithQuery(query: CaseListQuery): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: this.toRouteQueryParams(query),
    });
  }

  private queryFromParams(params: { get(name: string): string | null }): CaseListQuery {
    const sortBy = this.asSortBy(params.get('sortBy'));
    const sortDirection = params.get('sortDirection') === 'asc' ? 'asc' : 'desc';

    return {
      page: this.asPositiveInt(params.get('page'), DEFAULT_QUERY.page),
      pageSize: this.asPositiveInt(params.get('pageSize'), DEFAULT_QUERY.pageSize),
      search: params.get('search') ?? '',
      status: this.asStatus(params.get('status')),
      priority: this.asPriority(params.get('priority')),
      overdue: this.parseOverdue(params.get('overdue') ?? ''),
      sortBy,
      sortDirection,
    };
  }

  private toRouteQueryParams(query: CaseListQuery): Record<string, string | number | null> {
    return {
      page: query.page === DEFAULT_QUERY.page ? null : query.page,
      pageSize: query.pageSize === DEFAULT_QUERY.pageSize ? null : query.pageSize,
      search: query.search?.trim() || null,
      status: query.status || null,
      priority: query.priority || null,
      overdue: query.overdue === null || query.overdue === undefined ? null : String(query.overdue),
      sortBy: query.sortBy === DEFAULT_QUERY.sortBy ? null : query.sortBy,
      sortDirection: query.sortDirection === DEFAULT_QUERY.sortDirection ? null : query.sortDirection,
    };
  }

  private patchFilterForm(query: CaseListQuery): void {
    this.filterForm.setValue(
      {
        search: query.search ?? '',
        status: query.status ?? '',
        priority: query.priority ?? '',
        overdue: query.overdue === null || query.overdue === undefined ? '' : String(query.overdue),
        sortBy: query.sortBy,
        sortDirection: query.sortDirection,
      },
      { emitEvent: false },
    );
  }

  private parseOverdue(value: string | null): boolean | null {
    if (value === 'true') {
      return true;
    }

    if (value === 'false') {
      return false;
    }

    return null;
  }

  private asPositiveInt(value: string | null, fallback: number): number {
    const parsed = Number(value);
    return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback;
  }

  private asStatus(value: string | null): CaseStatus | '' {
    return STATUS_OPTIONS.includes(value as CaseStatus) ? (value as CaseStatus) : '';
  }

  private asPriority(value: string | null): CasePriority | '' {
    return PRIORITY_OPTIONS.includes(value as CasePriority) ? (value as CasePriority) : '';
  }

  private asSortBy(value: string | null): CaseSortBy {
    return SORT_OPTIONS.includes(value as CaseSortBy) ? (value as CaseSortBy) : DEFAULT_QUERY.sortBy;
  }
}
