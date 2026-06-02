import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  AnalystLookup,
  ApprovalDecisionRequest,
  ApprovalDecisionResult,
  ApprovalQueueItem,
  ApprovalRequestResult,
  AssignCaseRequest,
  CaseListItem,
  CaseListQuery,
  CaseDetail,
  CaseNote,
  CaseTypeSummary,
  CreateCaseNoteRequest,
  CreateCaseRequest,
  CreateCaseResponse,
  PagedResult,
  RequestClosureRequest,
  TimelineItem,
  UpdateCaseStatusRequest,
} from './case.models';

@Injectable({ providedIn: 'root' })
export class CaseApiService {
  private readonly http = inject(HttpClient);

  getCases(query: CaseListQuery): Observable<PagedResult<CaseListItem>> {
    return this.http.get<PagedResult<CaseListItem>>('/api/cases', {
      params: this.buildCaseQueryParams(query),
    });
  }

  createCase(request: CreateCaseRequest): Observable<CreateCaseResponse> {
    return this.http.post<CreateCaseResponse>('/api/cases', request);
  }

  getCase(id: string): Observable<CaseDetail> {
    return this.http.get<CaseDetail>(`/api/cases/${id}`);
  }

  assignCase(caseId: string, request: AssignCaseRequest): Observable<CaseDetail> {
    return this.http.patch<CaseDetail>(`/api/cases/${caseId}/assign`, request);
  }

  updateStatus(caseId: string, request: UpdateCaseStatusRequest): Observable<CaseDetail> {
    return this.http.patch<CaseDetail>(`/api/cases/${caseId}/status`, request);
  }

  requestClosure(caseId: string, request: RequestClosureRequest): Observable<ApprovalRequestResult> {
    return this.http.post<ApprovalRequestResult>(`/api/cases/${caseId}/closure-request`, request);
  }

  getPendingApprovals(page = 1, pageSize = 20): Observable<PagedResult<ApprovalQueueItem>> {
    return this.http.get<PagedResult<ApprovalQueueItem>>('/api/approvals/pending', {
      params: new HttpParams().set('page', page).set('pageSize', pageSize),
    });
  }

  approveApproval(
    approvalId: string,
    request: ApprovalDecisionRequest,
  ): Observable<ApprovalDecisionResult> {
    return this.http.post<ApprovalDecisionResult>(`/api/approvals/${approvalId}/approve`, request);
  }

  rejectApproval(
    approvalId: string,
    request: ApprovalDecisionRequest,
  ): Observable<ApprovalDecisionResult> {
    return this.http.post<ApprovalDecisionResult>(`/api/approvals/${approvalId}/reject`, request);
  }

  getAnalysts(): Observable<AnalystLookup[]> {
    return this.http.get<AnalystLookup[]>('/api/users/analysts');
  }

  getNotes(caseId: string): Observable<CaseNote[]> {
    return this.http.get<CaseNote[]>(`/api/cases/${caseId}/notes`);
  }

  addNote(caseId: string, body: string): Observable<CaseNote> {
    const request: CreateCaseNoteRequest = { body };
    return this.http.post<CaseNote>(`/api/cases/${caseId}/notes`, request);
  }

  getTimeline(caseId: string): Observable<TimelineItem[]> {
    return this.http.get<TimelineItem[]>(`/api/cases/${caseId}/timeline`);
  }

  getCaseTypes(): Observable<CaseTypeSummary[]> {
    return this.http.get<CaseTypeSummary[]>('/api/case-types');
  }

  private buildCaseQueryParams(query: CaseListQuery): HttpParams {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize)
      .set('sortBy', query.sortBy)
      .set('sortDirection', query.sortDirection);

    const search = query.search?.trim();
    if (search) {
      params = params.set('search', search);
    }

    if (query.status) {
      params = params.set('status', query.status);
    }

    if (query.priority) {
      params = params.set('priority', query.priority);
    }

    if (query.overdue !== null && query.overdue !== undefined) {
      params = params.set('overdue', String(query.overdue));
    }

    return params;
  }
}
