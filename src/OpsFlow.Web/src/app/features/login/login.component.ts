import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';

interface DemoAccount {
  label: string;
  email: string;
}

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly demoAccounts: DemoAccount[] = [
    { label: 'Admin', email: 'admin@opsflow.local' },
    { label: 'Manager', email: 'manager@opsflow.local' },
    { label: 'Analyst 1', email: 'analyst1@opsflow.local' },
    { label: 'Analyst 2', email: 'analyst2@opsflow.local' },
  ];

  readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  login(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password } = this.form.getRawValue();
    this.submitLogin(email, password);
  }

  useDemoAccount(account: DemoAccount): void {
    this.form.setValue({
      email: account.email,
      password: 'Password123!',
    });
    this.submitLogin(account.email, 'Password123!');
  }

  private submitLogin(email: string, password: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.login(email, password).subscribe({
      next: () => {
        this.router.navigateByUrl('/dashboard');
      },
      error: () => {
        this.errorMessage.set('Invalid email or password.');
        this.isLoading.set(false);
      },
      complete: () => {
        this.isLoading.set(false);
      },
    });
  }
}
