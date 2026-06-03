import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { DashboardApiService } from './dashboard-api.service';

describe('DashboardApiService', () => {
  let service: DashboardApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(DashboardApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('gets dashboard summary', () => {
    service.getSummary().subscribe();

    const request = httpMock.expectOne('/api/dashboard/summary');
    expect(request.request.method).toBe('GET');
    request.flush({
      openCases: 2,
      overdueOpenCases: 1,
      pendingApprovals: 1,
      averageOpenAgeHours: 4,
      slaBreachRate: 0.5,
    });
  });

  it('gets dashboard breakdowns', () => {
    service.getBreakdowns().subscribe();

    const request = httpMock.expectOne('/api/dashboard/breakdowns');
    expect(request.request.method).toBe('GET');
    request.flush({
      byStatus: [],
      byPriority: [],
      byCaseType: [],
      byAssignee: [],
    });
  });
});
