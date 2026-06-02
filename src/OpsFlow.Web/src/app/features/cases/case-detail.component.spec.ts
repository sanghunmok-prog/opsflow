import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { CaseApiService } from './case-api.service';
import { CaseDetailComponent } from './case-detail.component';

describe('CaseDetailComponent', () => {
  let fixture: ComponentFixture<CaseDetailComponent>;
  let api: FakeCaseApiService;
  let auth: FakeAuthService;

  beforeEach(async () => {
    api = new FakeCaseApiService();
    auth = new FakeAuthService();

    await TestBed.configureTestingModule({
      imports: [CaseDetailComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ id: 'case-1' }),
            },
          },
        },
        { provide: CaseApiService, useValue: api },
        { provide: AuthService, useValue: auth },
      ],
    }).compileComponents();
  });

  it('renders case metadata notes and timeline', async () => {
    fixture = TestBed.createComponent(CaseDetailComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('OPF-2026-0001');
    expect(compiled.textContent).toContain('Vendor exception');
    expect(compiled.textContent).toContain('Initial review completed.');
    expect(compiled.textContent).toContain('Case Created');
  });

  it('trims and submits note body then refreshes notes and timeline', async () => {
    fixture = TestBed.createComponent(CaseDetailComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const component = fixture.componentInstance;
    component.noteForm.setValue({ body: '  Reviewed the case.  ' });
    component.submitNote();

    expect(api.addedNotes).toEqual(['Reviewed the case.']);
    expect(api.notesCalls).toBe(2);
    expect(api.timelineCalls).toBe(2);
  });

  it('rejects whitespace note body before calling API', async () => {
    fixture = TestBed.createComponent(CaseDetailComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const component = fixture.componentInstance;
    component.noteForm.setValue({ body: '   ' });
    component.submitNote();

    expect(component.noteValidationMessage()).toContain('required');
    expect(api.addedNotes).toEqual([]);
  });

  it('hides assignment panel for Analysts', async () => {
    auth.roles = ['Analyst'];
    fixture = TestBed.createComponent(CaseDetailComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).not.toContain('Assignment');
    expect(api.analystCalls).toBe(0);
  });

  it('shows assignment panel for Managers and loads analysts', async () => {
    auth.roles = ['Manager'];
    fixture = TestBed.createComponent(CaseDetailComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Assignment');
    expect(compiled.textContent).toContain('Demo Analyst');
    expect(api.analystCalls).toBe(1);
  });

  it('submits assignment with analyst id and trimmed reason then refreshes detail and timeline', async () => {
    auth.roles = ['Manager'];
    fixture = TestBed.createComponent(CaseDetailComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const component = fixture.componentInstance;
    component.assignmentForm.setValue({
      assignedToUserId: 'analyst-2',
      reason: '  Reassign for coverage.  ',
    });
    component.submitAssignment();

    expect(api.assignedCases).toEqual([
      {
        caseId: 'case-1',
        assignedToUserId: 'analyst-2',
        reason: 'Reassign for coverage.',
        rowVersion: 'AAAA',
      },
    ]);
    expect(api.detailCalls).toBe(2);
    expect(api.timelineCalls).toBe(2);
  });

  it('rejects whitespace assignment reason before calling API', async () => {
    auth.roles = ['Manager'];
    fixture = TestBed.createComponent(CaseDetailComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const component = fixture.componentInstance;
    component.assignmentForm.setValue({ assignedToUserId: 'analyst-2', reason: '   ' });
    component.submitAssignment();

    expect(component.assignmentValidationMessage()).toContain('required');
    expect(api.assignedCases).toEqual([]);
  });
});

class FakeAuthService {
  roles: string[] = ['Manager'];

  hasAnyRole(roles: string[]): boolean {
    return roles.some((role) => this.roles.includes(role));
  }
}

class FakeCaseApiService {
  detailCalls = 0;
  analystCalls = 0;
  notesCalls = 0;
  timelineCalls = 0;
  addedNotes: string[] = [];
  assignedCases: Array<{
    caseId: string;
    assignedToUserId: string;
    reason: string;
    rowVersion?: string;
  }> = [];

  getCase(id: string) {
    this.detailCalls += 1;
    return of({
      id,
      caseNumber: 'OPF-2026-0001',
      title: 'Vendor exception',
      description: 'Synthetic internal case',
      caseType: { id: 'case-type-1', name: 'Vendor Approval Issue' },
      priority: 'High',
      status: 'Assigned',
      assignedTo: { id: 'user-1', displayName: 'Demo Analyst' },
      createdBy: { id: 'manager-1', displayName: 'Demo Manager' },
      createdAtUtc: '2026-06-01T00:00:00Z',
      updatedAtUtc: '2026-06-01T01:00:00Z',
      dueAtUtc: '2026-06-02T00:00:00Z',
      isOverdue: false,
      rowVersion: 'AAAA',
    });
  }

  getAnalysts() {
    this.analystCalls += 1;
    return of([
      { id: 'analyst-1', displayName: 'Demo Analyst', email: 'analyst1@opsflow.local' },
      { id: 'analyst-2', displayName: 'Backup Analyst', email: 'analyst2@opsflow.local' },
    ]);
  }

  assignCase(caseId: string, request: { assignedToUserId: string; reason: string; rowVersion?: string }) {
    this.assignedCases.push({ caseId, ...request });
    return of({
      id: caseId,
      caseNumber: 'OPF-2026-0001',
      title: 'Vendor exception',
      description: 'Synthetic internal case',
      caseType: { id: 'case-type-1', name: 'Vendor Approval Issue' },
      priority: 'High',
      status: 'Assigned',
      assignedTo: { id: request.assignedToUserId, displayName: 'Backup Analyst' },
      createdBy: { id: 'manager-1', displayName: 'Demo Manager' },
      createdAtUtc: '2026-06-01T00:00:00Z',
      updatedAtUtc: '2026-06-01T04:00:00Z',
      dueAtUtc: '2026-06-02T00:00:00Z',
      isOverdue: false,
      rowVersion: 'AAAB',
    });
  }

  getNotes(_caseId: string) {
    this.notesCalls += 1;
    return of([
      {
        id: 'note-1',
        body: 'Initial review completed.',
        createdBy: { id: 'user-1', displayName: 'Demo Analyst' },
        createdAtUtc: '2026-06-01T02:00:00Z',
      },
    ]);
  }

  addNote(_caseId: string, body: string) {
    this.addedNotes.push(body);
    return of({
      id: 'note-2',
      body,
      createdBy: { id: 'user-1', displayName: 'Demo Analyst' },
      createdAtUtc: '2026-06-01T03:00:00Z',
    });
  }

  getTimeline(_caseId: string) {
    this.timelineCalls += 1;
    return of([
      {
        id: 'timeline-1',
        action: 'CaseCreated',
        actor: { id: 'manager-1', displayName: 'Demo Manager' },
        createdAtUtc: '2026-06-01T00:00:00Z',
        description: 'Case created',
      },
    ]);
  }
}
