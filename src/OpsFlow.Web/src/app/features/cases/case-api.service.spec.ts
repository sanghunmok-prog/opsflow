import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';

import { CaseApiService } from './case-api.service';

describe('CaseApiService', () => {
  let service: CaseApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(CaseApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('builds case query params for server-side queue requests', () => {
    service
      .getCases({
        page: 2,
        pageSize: 20,
        search: 'OPF-2026',
        status: 'Assigned',
        priority: 'High',
        overdue: true,
        sortBy: 'dueAtUtc',
        sortDirection: 'asc',
      })
      .subscribe();

    const request = httpMock.expectOne(
      (candidate) =>
        candidate.url === '/api/cases' &&
        candidate.params.get('page') === '2' &&
        candidate.params.get('pageSize') === '20' &&
        candidate.params.get('search') === 'OPF-2026' &&
        candidate.params.get('status') === 'Assigned' &&
        candidate.params.get('priority') === 'High' &&
        candidate.params.get('overdue') === 'true' &&
        candidate.params.get('sortBy') === 'dueAtUtc' &&
        candidate.params.get('sortDirection') === 'asc',
    );
    expect(request.request.method).toBe('GET');
    request.flush({ items: [], page: 2, pageSize: 20, totalCount: 0, totalPages: 0 });
  });

  it('posts create case requests without assignment or workflow fields', () => {
    const body = {
      title: 'Vendor exception',
      description: 'Synthetic internal case',
      caseTypeId: 'case-type-1',
      priority: 'High' as const,
    };

    service.createCase(body).subscribe();

    const request = httpMock.expectOne('/api/cases');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush({
      id: 'case-1',
      caseNumber: 'OPF-2026-0001',
      title: body.title,
      description: body.description,
      caseType: { id: body.caseTypeId, name: 'Vendor Approval Issue' },
      priority: 'High',
      status: 'New',
      assignedTo: null,
      createdBy: { id: 'user-1', displayName: 'Demo Manager' },
      createdAtUtc: '2026-06-01T00:00:00Z',
      updatedAtUtc: '2026-06-01T00:00:00Z',
      dueAtUtc: '2026-06-02T00:00:00Z',
      isOverdue: false,
      rowVersion: 'AAAA',
    });
  });

  it('gets case detail', () => {
    service.getCase('case-1').subscribe();

    const request = httpMock.expectOne('/api/cases/case-1');
    expect(request.request.method).toBe('GET');
    request.flush({
      id: 'case-1',
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
  });

  it('patches case assignment requests', () => {
    service
      .assignCase('case-1', {
        assignedToUserId: 'analyst-1',
        reason: 'Assigned for analyst review.',
        rowVersion: 'AAAA',
      })
      .subscribe();

    const request = httpMock.expectOne('/api/cases/case-1/assign');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({
      assignedToUserId: 'analyst-1',
      reason: 'Assigned for analyst review.',
      rowVersion: 'AAAA',
    });
    request.flush({
      id: 'case-1',
      caseNumber: 'OPF-2026-0001',
      title: 'Vendor exception',
      description: 'Synthetic internal case',
      caseType: { id: 'case-type-1', name: 'Vendor Approval Issue' },
      priority: 'High',
      status: 'Assigned',
      assignedTo: { id: 'analyst-1', displayName: 'Demo Analyst' },
      createdBy: { id: 'manager-1', displayName: 'Demo Manager' },
      createdAtUtc: '2026-06-01T00:00:00Z',
      updatedAtUtc: '2026-06-01T01:00:00Z',
      dueAtUtc: '2026-06-02T00:00:00Z',
      isOverdue: false,
      rowVersion: 'AAAB',
    });
  });

  it('gets active analysts lookup', () => {
    service.getAnalysts().subscribe();

    const request = httpMock.expectOne('/api/users/analysts');
    expect(request.request.method).toBe('GET');
    request.flush([
      { id: 'analyst-1', displayName: 'Demo Analyst', email: 'analyst1@opsflow.local' },
    ]);
  });

  it('gets case notes', () => {
    service.getNotes('case-1').subscribe();

    const request = httpMock.expectOne('/api/cases/case-1/notes');
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('posts trimmed note body through addNote caller input', () => {
    service.addNote('case-1', 'Reviewed the case.').subscribe();

    const request = httpMock.expectOne('/api/cases/case-1/notes');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ body: 'Reviewed the case.' });
    request.flush({
      id: 'note-1',
      body: 'Reviewed the case.',
      createdBy: { id: 'user-1', displayName: 'Demo Analyst' },
      createdAtUtc: '2026-06-01T00:00:00Z',
    });
  });

  it('gets case timeline', () => {
    service.getTimeline('case-1').subscribe();

    const request = httpMock.expectOne('/api/cases/case-1/timeline');
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('posts closure request with reason and rowVersion', () => {
    service
      .requestClosure('case-1', {
        requestReason: 'Work is complete.',
        rowVersion: 'AAAA',
      })
      .subscribe();

    const request = httpMock.expectOne('/api/cases/case-1/closure-request');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      requestReason: 'Work is complete.',
      rowVersion: 'AAAA',
    });
    request.flush({
      id: 'approval-1',
      caseId: 'case-1',
      caseNumber: 'OPF-2026-0001',
      caseTitle: 'Vendor exception',
      priority: 'High',
      caseStatus: 'PendingApproval',
      approvalStatus: 'Pending',
      requestReason: 'Work is complete.',
      requestedBy: { id: 'user-1', displayName: 'Demo Analyst' },
      requestedAtUtc: '2026-06-01T00:00:00Z',
      rowVersion: 'AAAB',
    });
  });

  it('gets pending approvals with paging params', () => {
    service.getPendingApprovals(2, 10).subscribe();

    const request = httpMock.expectOne(
      (candidate) =>
        candidate.url === '/api/approvals/pending' &&
        candidate.params.get('page') === '2' &&
        candidate.params.get('pageSize') === '10',
    );
    expect(request.request.method).toBe('GET');
    request.flush({ items: [], page: 2, pageSize: 10, totalCount: 0, totalPages: 0 });
  });

  it('posts approve and reject approval requests', () => {
    service.approveApproval('approval-1', { decisionReason: null, rowVersion: 'AAAA' }).subscribe();
    service
      .rejectApproval('approval-2', {
        decisionReason: 'More review is required.',
        rowVersion: 'AAAB',
      })
      .subscribe();

    const approveRequest = httpMock.expectOne('/api/approvals/approval-1/approve');
    expect(approveRequest.request.method).toBe('POST');
    expect(approveRequest.request.body).toEqual({ decisionReason: null, rowVersion: 'AAAA' });
    approveRequest.flush({
      approvalId: 'approval-1',
      caseId: 'case-1',
      caseNumber: 'OPF-2026-0001',
      caseTitle: 'Vendor exception',
      approvalStatus: 'Approved',
      caseStatus: 'Closed',
      decisionReason: null,
      reviewedBy: { id: 'manager-1', displayName: 'Demo Manager' },
      decisionAtUtc: '2026-06-01T00:00:00Z',
      rowVersion: 'AAAC',
    });

    const rejectRequest = httpMock.expectOne('/api/approvals/approval-2/reject');
    expect(rejectRequest.request.method).toBe('POST');
    expect(rejectRequest.request.body).toEqual({
      decisionReason: 'More review is required.',
      rowVersion: 'AAAB',
    });
    rejectRequest.flush({
      approvalId: 'approval-2',
      caseId: 'case-2',
      caseNumber: 'OPF-2026-0002',
      caseTitle: 'Vendor exception',
      approvalStatus: 'Rejected',
      caseStatus: 'InReview',
      decisionReason: 'More review is required.',
      reviewedBy: { id: 'manager-1', displayName: 'Demo Manager' },
      decisionAtUtc: '2026-06-01T00:00:00Z',
      rowVersion: 'AAAD',
    });
  });
});
