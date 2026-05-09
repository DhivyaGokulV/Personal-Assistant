import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-login',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-shell">
      <div class="auth-card neon">
        <h1 class="neon-text mb-1">Sign in</h1>
        <p class="text-muted-soft mb-4">Welcome back to your personal assistant.</p>

        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="mb-3">
            <label class="form-label" for="email">Email</label>
            <input id="email" type="email" class="form-control" formControlName="email" autocomplete="email" />
          </div>
          <div class="mb-3">
            <label class="form-label" for="password">Password</label>
            <input id="password" type="password" class="form-control" formControlName="password" autocomplete="current-password" />
          </div>

          @if (errorMessage()) {
            <div class="alert alert-danger py-2 small mb-3">{{ errorMessage() }}</div>
          }

          <button type="submit" class="btn-neon w-100" [disabled]="form.invalid || submitting()">
            {{ submitting() ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>

        <p class="mt-4 mb-0 text-center text-muted-soft small">
          New here? <a routerLink="/register">Create an account</a>
        </p>
      </div>
    </div>
  `,
  styles: [`
    .auth-shell {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 1.25rem;
      background: var(--bg);
    }
    .auth-card {
      width: 100%;
      max-width: 400px;
      padding: 2rem;
    }
    h1 { font-size: 1.5rem; font-weight: 600; }
  `]
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  submit(): void {
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.errorMessage.set(null);
    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/home';
        this.router.navigateByUrl(returnUrl);
      },
      error: err => {
        this.submitting.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Sign in failed.');
      }
    });
  }
}
