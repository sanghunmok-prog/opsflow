import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CaseListItem,
  CaseListQuery,
  CaseTypeSummary,
  CreateCaseRequest,
  CreateCaseResponse,
  PagedResult,
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
