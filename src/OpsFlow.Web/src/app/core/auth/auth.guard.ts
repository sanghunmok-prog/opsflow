import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { catchError, map, Observable, of } from 'rxjs';

import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (): Observable<boolean | UrlTree> | boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  if (authService.token) {
    return authService.restoreSession().pipe(
      map((isRestored) => (isRestored ? true : router.createUrlTree(['/login']))),
      catchError(() => of(router.createUrlTree(['/login']))),
    );
  }

  return router.createUrlTree(['/login']);
};

export const loginGuard: CanActivateFn = (): Observable<boolean | UrlTree> | boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return router.createUrlTree(['/dashboard']);
  }

  if (authService.token) {
    return authService.restoreSession().pipe(
      map((isRestored) => (isRestored ? router.createUrlTree(['/dashboard']) : true)),
      catchError(() => of(true)),
    );
  }

  return true;
};
