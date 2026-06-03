import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { DashboardBreakdowns, DashboardSummary } from './dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly http = inject(HttpClient);

  getSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>('/api/dashboard/summary');
  }

  getBreakdowns(): Observable<DashboardBreakdowns> {
    return this.http.get<DashboardBreakdowns>('/api/dashboard/breakdowns');
  }
}
