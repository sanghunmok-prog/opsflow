import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const token = authService.token;
  const isLoginRequest = request.url.includes('/api/auth/login');

  const authenticatedRequest =
    token && !isLoginRequest
      ? request.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`,
          },
        })
      : request;

  return next(authenticatedRequest).pipe(
    catchError((error) => {
      if (error.status === 401 && !isLoginRequest) {
        authService.logout();
      }

      return throwError(() => error);
    }),
  );
};
