import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of, tap } from 'rxjs';

import { CurrentUser, LoginResponse } from './auth.models';

const ACCESS_TOKEN_KEY = 'opsflow_access_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly currentUserSignal = signal<CurrentUser | null>(null);

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(
    () => this.currentUserSignal() !== null && this.token !== null,
  );

  get token(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  login(email: string, password: string): Observable<CurrentUser> {
    return this.http.post<LoginResponse>('/api/auth/login', { email, password }).pipe(
      tap((response) => this.storeToken(response.accessToken)),
      tap((response) => this.currentUserSignal.set(response.user)),
      map((response) => response.user),
    );
  }

  loginWithDemoAccount(email: string): Observable<CurrentUser> {
    return this.login(email, 'Password123!');
  }

  logout(): void {
    this.clearSession();
  }

  loadCurrentUser(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>('/api/auth/me').pipe(
      tap((user) => this.currentUserSignal.set(user)),
      catchError((error) => {
        this.clearSession();
        throw error;
      }),
    );
  }

  restoreSession(): Observable<boolean> {
    if (!this.token) {
      this.currentUserSignal.set(null);
      return of(false);
    }

    if (this.currentUserSignal()) {
      return of(true);
    }

    return this.loadCurrentUser().pipe(
      map(() => true),
      catchError(() => of(false)),
    );
  }

  hasRole(role: string): boolean {
    return this.currentUserSignal()?.roles.includes(role) ?? false;
  }

  hasAnyRole(roles: string[]): boolean {
    const userRoles = this.currentUserSignal()?.roles ?? [];
    return roles.some((role) => userRoles.includes(role));
  }

  private storeToken(accessToken: string): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
  }

  private clearSession(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    this.currentUserSignal.set(null);
  }
}
