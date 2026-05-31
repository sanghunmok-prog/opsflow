import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';

import { authGuard } from './auth.guard';

describe('authGuard', () => {
  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [provideRouter([])],
    });
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('redirects unauthenticated users to login', () => {
    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

    expect(result).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/login');
  });
});
