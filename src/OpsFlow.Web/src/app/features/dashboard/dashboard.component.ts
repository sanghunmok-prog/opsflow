import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { DashboardApiService } from './dashboard-api.service';
import { DashboardBreakdownItem, DashboardBreakdowns, DashboardSummary } from './dashboard.models';

type DashboardCard = {
  label: string;
  value: number;
  kind: 'number' | 'hours' | 'percent';
  routerLink: string[] | null;
  queryParams?: Record<string, string>;
};

type BreakdownSection = {
  title: string;
  items: DashboardBreakdownItem[];
};

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  template: `
    <section class="dashboard-page">
      <header class="page-header">
        <div>
          <p class="eyebrow">{{ isAnalyst() ? 'My Workload' : 'Operations' }}</p>
          <h1>Dashboard</h1>
        </div>
        <button type="button" class="secondary" (click)="loadDashboard()" [disabled]="loading()">
          {{ loading() ? 'Refreshing...' : 'Refresh' }}
        </button>
      </header>

      @if (loading()) {
        <div class="state">Loading dashboard metrics...</div>
      } @else if (error()) {
        <div class="state error" role="alert">{{ error() }}</div>
      } @else if (isEmpty()) {
        <div class="state">No dashboard data is available for the current scope.</div>
      } @else {
        @if (summary(); as currentSummary) {
          <section class="metric-grid" aria-label="Dashboard summary">
            @for (card of cards(currentSummary); track card.label) {
              @if (card.routerLink) {
                <a class="metric-card" [routerLink]="card.routerLink" [queryParams]="card.queryParams ?? null">
                  <span>{{ card.label }}</span>
                  <strong>{{ formatCardValue(card) }}</strong>
                </a>
              } @else {
                <article class="metric-card">
                  <span>{{ card.label }}</span>
                  <strong>{{ formatCardValue(card) }}</strong>
                </article>
              }
            }
          </section>
        }

        @if (breakdowns(); as currentBreakdowns) {
          <section class="breakdown-grid" aria-label="Dashboard breakdowns">
            @for (section of breakdownSections(currentBreakdowns); track section.title) {
              <article class="breakdown-panel">
                <header>
                  <h2>{{ section.title }}</h2>
                </header>

                @if (section.items.length === 0) {
                  <p class="empty-row">No data in this scope.</p>
                } @else {
                  <div class="breakdown-list">
                    @for (item of section.items; track item.key) {
                      @if (item.routeQuery) {
                        <a class="breakdown-row" [routerLink]="['/cases']" [queryParams]="item.routeQuery">
                          <span>{{ item.label }}</span>
                          <strong>{{ item.count }}</strong>
                        </a>
                      } @else {
                        <div class="breakdown-row">
                          <span>{{ item.label }}</span>
                          <strong>{{ item.count }}</strong>
                        </div>
                      }
                    }
                  </div>
                }
              </article>
            }
          </section>
        }
      }
    </section>
  `,
  styles: `
    :host {
      display: block;
    }

    .dashboard-page {
      display: grid;
      gap: 1rem;
    }

    .page-header,
    .metric-card,
    .breakdown-panel,
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
      line-height: 1.25;
    }

    button {
      min-height: 2.35rem;
      border: 1px solid #c6d0d9;
      border-radius: 6px;
      cursor: pointer;
      font: inherit;
      font-weight: 750;
      padding: 0 0.8rem;
    }

    button:disabled {
      cursor: not-allowed;
      opacity: 0.6;
    }

    .secondary {
      background: #ffffff;
      color: #26323d;
    }

    .metric-grid {
      display: grid;
      grid-template-columns: repeat(5, minmax(0, 1fr));
      gap: 1rem;
    }

    .metric-card {
      display: grid;
      gap: 0.55rem;
      min-height: 7rem;
      padding: 1rem;
      text-decoration: none;
    }

    .metric-card span,
    .breakdown-row span {
      color: #4a5661;
      font-size: 0.85rem;
      font-weight: 750;
    }

    .metric-card strong {
      color: #172026;
      font-size: 1.75rem;
      line-height: 1.1;
    }

    a.metric-card:hover,
    a.breakdown-row:hover {
      border-color: #0f766e;
      background: #f0fdfa;
    }

    .breakdown-grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 1rem;
    }

    .breakdown-panel {
      overflow: hidden;
    }

    .breakdown-panel header {
      border-bottom: 1px solid #e5ebf0;
      padding: 0.95rem 1rem;
    }

    .breakdown-list {
      display: grid;
    }

    .breakdown-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      border-bottom: 1px solid #e5ebf0;
      color: inherit;
      padding: 0.85rem 1rem;
      text-decoration: none;
    }

    .breakdown-row:last-child {
      border-bottom: 0;
    }

    .breakdown-row strong {
      color: #172026;
      font-size: 1rem;
    }

    .state,
    .empty-row {
      color: #4a5661;
      padding: 2rem;
      text-align: center;
    }

    .empty-row {
      padding: 1.5rem 1rem;
    }

    .error {
      border-color: #fecaca;
      background: #fef2f2;
      color: #991b1b;
    }

    @media (max-width: 1100px) {
      .metric-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }
    }

    @media (max-width: 760px) {
      .page-header {
        align-items: stretch;
        flex-direction: column;
      }

      .metric-grid,
      .breakdown-grid {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class DashboardComponent {
  private readonly api = inject(DashboardApiService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly summary = signal<DashboardSummary | null>(null);
  readonly breakdowns = signal<DashboardBreakdowns | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly isAnalyst = computed(() => this.authService.hasRole('Analyst'));
  readonly isEmpty = computed(() => {
    const summary = this.summary();
    const breakdowns = this.breakdowns();
    return (
      !summary ||
      !breakdowns ||
      (summary.openCases === 0 &&
        summary.overdueOpenCases === 0 &&
        summary.pendingApprovals === 0 &&
        breakdowns.byStatus.length === 0 &&
        breakdowns.byPriority.length === 0 &&
        breakdowns.byCaseType.length === 0 &&
        breakdowns.byAssignee.length === 0)
    );
  });

  constructor() {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading.set(true);
    this.error.set('');
    forkJoin({
      summary: this.api.getSummary(),
      breakdowns: this.api.getBreakdowns(),
    })
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ summary, breakdowns }) => {
          this.summary.set(summary);
          this.breakdowns.set(breakdowns);
        },
        error: () => {
          this.summary.set(null);
          this.breakdowns.set(null);
          this.error.set('Dashboard metrics could not be loaded. Try refreshing the page.');
        },
      });
  }

  cards(summary: DashboardSummary): DashboardCard[] {
    return [
      {
        label: 'Open Cases',
        value: summary.openCases,
        kind: 'number',
        routerLink: ['/cases'],
      },
      {
        label: 'Overdue Open Cases',
        value: summary.overdueOpenCases,
        kind: 'number',
        routerLink: ['/cases'],
        queryParams: { overdue: 'true' },
      },
      {
        label: 'Pending Approvals',
        value: summary.pendingApprovals,
        kind: 'number',
        routerLink: this.isAnalyst() ? ['/cases'] : ['/approvals'],
        queryParams: this.isAnalyst() ? { status: 'PendingApproval' } : undefined,
      },
      {
        label: 'Average Open Age',
        value: summary.averageOpenAgeHours,
        kind: 'hours',
        routerLink: null,
      },
      {
        label: 'SLA Breach Rate',
        value: summary.slaBreachRate,
        kind: 'percent',
        routerLink: null,
      },
    ];
  }

  breakdownSections(breakdowns: DashboardBreakdowns): BreakdownSection[] {
    return [
      { title: 'By Status', items: breakdowns.byStatus },
      { title: 'By Priority', items: breakdowns.byPriority },
      { title: 'By Case Type', items: breakdowns.byCaseType },
      { title: 'By Assignee', items: breakdowns.byAssignee },
    ];
  }

  formatCardValue(card: DashboardCard): string {
    if (card.kind === 'percent') {
      return `${(card.value * 100).toLocaleString(undefined, {
        maximumFractionDigits: 1,
        minimumFractionDigits: 0,
      })}%`;
    }

    if (card.kind === 'hours') {
      return `${card.value.toLocaleString(undefined, {
        maximumFractionDigits: 1,
        minimumFractionDigits: 0,
      })}h`;
    }

    return card.value.toLocaleString(undefined, { maximumFractionDigits: 0 });
  }
}
