import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, of, Subject, throwError } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { DashboardApiService } from './dashboard-api.service';
import { DashboardComponent } from './dashboard.component';
import { DashboardBreakdowns, DashboardSummary } from './dashboard.models';

describe('DashboardComponent', () => {
  let api: FakeDashboardApiService;
  let auth: FakeAuthService;

  beforeEach(async () => {
    api = new FakeDashboardApiService();
    auth = new FakeAuthService();

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        { provide: DashboardApiService, useValue: api },
        { provide: AuthService, useValue: auth },
      ],
    }).compileComponents();
  });

  it('calls summary and breakdown endpoints and renders metric cards', async () => {
    auth.roles = ['Manager'];
    const fixture = TestBed.createComponent(DashboardComponent);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.summaryCalls).toBe(1);
    expect(api.breakdownCalls).toBe(1);
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Open Cases');
    expect(compiled.textContent).toContain('By Status');
    expect(compiled.textContent).toContain('Assigned');
  });

  it('renders loading state', () => {
    api.summary$ = new Subject<DashboardSummary>();
    api.breakdowns$ = new Subject<DashboardBreakdowns>();
    const fixture = TestBed.createComponent(DashboardComponent);

    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Loading dashboard metrics');
  });

  it('renders error state', async () => {
    api.summary$ = throwError(() => new Error('failed'));
    const fixture = TestBed.createComponent(DashboardComponent);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'Dashboard metrics could not be loaded',
    );
  });

  it('links Manager pending approvals card to approvals', async () => {
    auth.roles = ['Manager'];
    const fixture = TestBed.createComponent(DashboardComponent);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(linkHrefs(fixture)).toContain('/approvals');
  });

  it('does not link Analyst pending approvals card to approvals', async () => {
    auth.roles = ['Analyst'];
    const fixture = TestBed.createComponent(DashboardComponent);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const hrefs = linkHrefs(fixture);
    expect(hrefs).not.toContain('/approvals');
    expect(hrefs).toContain('/cases?status=PendingApproval');
  });

  it('uses cases query params for dashboard drill-down links', async () => {
    auth.roles = ['Admin'];
    const fixture = TestBed.createComponent(DashboardComponent);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const hrefs = linkHrefs(fixture);
    expect(hrefs).toContain('/cases?overdue=true');
    expect(hrefs).toContain('/cases?status=Assigned');
    expect(hrefs).toContain('/cases?priority=High');
    expect(hrefs).toContain('/cases?caseTypeId=case-type-1');
    expect(hrefs).toContain('/cases?assignedToUserId=analyst-1');
  });
});

function linkHrefs(fixture: ComponentFixture<DashboardComponent>): string[] {
  return Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('a')).map(
    (link) => link.getAttribute('href') ?? '',
  );
}

class FakeAuthService {
  roles: string[] = [];

  hasRole(role: string): boolean {
    return this.roles.includes(role);
  }

  hasAnyRole(roles: string[]): boolean {
    return roles.some((role) => this.roles.includes(role));
  }
}

class FakeDashboardApiService {
  summaryCalls = 0;
  breakdownCalls = 0;
  summary$: Observable<DashboardSummary> = of({
    openCases: 5,
    overdueOpenCases: 2,
    pendingApprovals: 1,
    averageOpenAgeHours: 12.25,
    slaBreachRate: 0.4,
  });
  breakdowns$: Observable<DashboardBreakdowns> = of({
    byStatus: [{ key: 'Assigned', label: 'Assigned', count: 2, routeQuery: { status: 'Assigned' } }],
    byPriority: [{ key: 'High', label: 'High', count: 1, routeQuery: { priority: 'High' } }],
    byCaseType: [
      {
        key: 'case-type-1',
        label: 'Vendor Approval Issue',
        count: 1,
        routeQuery: { caseTypeId: 'case-type-1' },
      },
    ],
    byAssignee: [
      {
        key: 'analyst-1',
        label: 'Demo Analyst',
        count: 1,
        routeQuery: { assignedToUserId: 'analyst-1' },
      },
    ],
  });

  getSummary(): Observable<DashboardSummary> {
    this.summaryCalls += 1;
    return this.summary$;
  }

  getBreakdowns(): Observable<DashboardBreakdowns> {
    this.breakdownCalls += 1;
    return this.breakdowns$;
  }
}
