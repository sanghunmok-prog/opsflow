import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { CaseApiService } from './case-api.service';
import { CasesComponent } from './cases.component';
import { CaseListQuery, CreateCaseRequest } from './case.models';

describe('CasesComponent', () => {
  let fixture: ComponentFixture<CasesComponent>;
  let api: FakeCaseApiService;
  let auth: FakeAuthService;

  beforeEach(async () => {
    api = new FakeCaseApiService();
    auth = new FakeAuthService();

    await TestBed.configureTestingModule({
      imports: [CasesComponent],
      providers: [
        provideRouter([]),
        { provide: CaseApiService, useValue: api },
        { provide: AuthService, useValue: auth },
      ],
    }).compileComponents();
  });

  it('renders queue state', async () => {
    auth.roles = ['Manager'];
    fixture = TestBed.createComponent(CasesComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Case Queue');
    expect(compiled.textContent).toContain('OPF-2026-0001');
    expect(compiled.textContent).toContain('Overdue');
  });

  it('hides create button for Analysts', async () => {
    auth.roles = ['Analyst'];
    fixture = TestBed.createComponent(CasesComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).not.toContain('Create Case');
  });

  it('shows create button for Managers', async () => {
    auth.roles = ['Manager'];
    fixture = TestBed.createComponent(CasesComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Create Case');
  });

  it('calls createCase from the create form', async () => {
    auth.roles = ['Admin'];
    fixture = TestBed.createComponent(CasesComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const component = fixture.componentInstance;
    component.toggleCreateForm();
    component.createForm.setValue({
      title: 'New vendor issue',
      description: 'Needs review',
      caseTypeId: 'case-type-1',
      priority: 'High',
    });
    component.submitCreate();

    expect(api.createdRequests).toEqual([
      {
        title: 'New vendor issue',
        description: 'Needs review',
        caseTypeId: 'case-type-1',
        priority: 'High',
      },
    ]);
  });
});

class FakeAuthService {
  roles: string[] = [];

  hasRole(role: string): boolean {
    return this.roles.includes(role);
  }

  hasAnyRole(roles: string[]): boolean {
    return roles.some((role) => this.roles.includes(role));
  }
}

class FakeCaseApiService {
  createdRequests: CreateCaseRequest[] = [];

  getCases(_query: CaseListQuery) {
    return of({
      items: [
        {
          id: 'case-1',
          caseNumber: 'OPF-2026-0001',
          title: 'Vendor exception',
          caseType: { id: 'case-type-1', name: 'Vendor Approval Issue' },
          priority: 'High',
          status: 'Assigned',
          assignedTo: { id: 'user-1', displayName: 'Demo Analyst' },
          createdAtUtc: '2026-06-01T00:00:00Z',
          dueAtUtc: '2026-06-01T08:00:00Z',
          isOverdue: true,
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    });
  }

  getCaseTypes() {
    return of([{ id: 'case-type-1', name: 'Vendor Approval Issue' }]);
  }

  createCase(request: CreateCaseRequest) {
    this.createdRequests.push(request);
    return of({
      id: 'case-2',
      caseNumber: 'OPF-2026-0002',
      title: request.title,
      description: request.description ?? '',
      caseType: { id: request.caseTypeId, name: 'Vendor Approval Issue' },
      priority: request.priority,
      status: 'New',
      assignedTo: null,
      createdBy: { id: 'manager-1', displayName: 'Demo Manager' },
      createdAtUtc: '2026-06-01T00:00:00Z',
      updatedAtUtc: '2026-06-01T00:00:00Z',
      dueAtUtc: '2026-06-02T00:00:00Z',
      isOverdue: false,
      rowVersion: 'AAAA',
    });
  }
}
