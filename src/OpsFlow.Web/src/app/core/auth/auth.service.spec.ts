import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';

import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('stores the access token and current user after login', () => {
    service.login('manager@opsflow.local', 'Password123!').subscribe((user) => {
      expect(user.email).toBe('manager@opsflow.local');
      expect(service.token).toBe('demo-token');
      expect(service.isAuthenticated()).toBe(true);
    });

    const request = httpMock.expectOne('/api/auth/login');
    expect(request.request.method).toBe('POST');
    request.flush({
      accessToken: 'demo-token',
      tokenType: 'Bearer',
      expiresAtUtc: '2026-05-31T00:00:00Z',
      user: {
        id: '00000000-0000-0000-0000-000000000001',
        email: 'manager@opsflow.local',
        displayName: 'Demo Manager',
        roles: ['Manager'],
      },
    });
  });

  it('clears token and user on logout', () => {
    localStorage.setItem('opsflow_access_token', 'demo-token');

    service.logout();

    expect(service.token).toBeNull();
    expect(service.currentUser()).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
  });
});
