import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { AppShellComponent } from './app-shell.component';

describe('AppShellComponent', () => {
  let fixture: ComponentFixture<AppShellComponent>;
  let auth: FakeAuthService;

  beforeEach(async () => {
    auth = new FakeAuthService();

    await TestBed.configureTestingModule({
      imports: [AppShellComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }],
    }).compileComponents();
  });

  it('hides approval nav for Analysts', () => {
    auth.roles = ['Analyst'];
    fixture = TestBed.createComponent(AppShellComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).not.toContain('Approvals');
    expect(compiled.textContent).toContain('My Cases');
  });

  it('shows approval nav for Managers', () => {
    auth.roles = ['Manager'];
    fixture = TestBed.createComponent(AppShellComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Approvals');
  });

  it('shows approval nav for Admins', () => {
    auth.roles = ['Admin'];
    fixture = TestBed.createComponent(AppShellComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Approvals');
  });
});

class FakeAuthService {
  roles: string[] = ['Manager'];

  currentUser = () => {
    return {
      id: 'manager-1',
      email: 'manager@opsflow.local',
      displayName: 'Demo Manager',
      roles: this.roles,
    };
  };

  hasRole(role: string): boolean {
    return this.roles.includes(role);
  }

  hasAnyRole(roles: string[]): boolean {
    return roles.some((role) => this.roles.includes(role));
  }

  logout(): void {}
}
