import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-register',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-shell">
      <div class="auth-card neon-cyan">
        <h1 class="mb-1" style="color: var(--neon-cyan);">Create your account</h1>
        <p class="text-muted-soft mb-4">Just an email and a password to get started.</p>

        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="mb-3">
            <label class="form-label" for="displayName">Display name <span class="text-subtle small">(optional)</span></label>
            <input id="displayName" class="form-control" formControlName="displayName" autocomplete="name" />
          </div>
          <div class="mb-3">
            <label class="form-label" for="email">Email</label>
            <input id="email" type="email" class="form-control" formControlName="email" autocomplete="email" />
          </div>
          <div class="mb-3">
            <label class="form-label" for="password">Password</label>
            <input id="password" type="password" class="form-control" formControlName="password" autocomplete="new-password" />
            <div class="form-text small text-subtle">Minimum 8 characters.</div>
          </div>

          @if (errorMessage()) {
            <div class="alert alert-danger py-2 small mb-3">{{ errorMessage() }}</div>
          }

          <button type="submit" class="btn-neon w-100" [disabled]="form.invalid || submitting()">
            {{ submitting() ? 'Creating…' : 'Create account' }}
          </button>
        </form>

        <p class="mt-4 mb-0 text-center text-muted-soft small">
          Already have an account? <a routerLink="/login">Sign in</a>
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
      max-width: 420px;
      padding: 2rem;
    }
    h1 { font-size: 1.5rem; font-weight: 600; }
  `]
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    displayName: [''],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  submit(): void {
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.errorMessage.set(null);

    const { displayName, email, password } = this.form.getRawValue();
    this.auth.register({
      email,
      password,
      displayName: displayName.trim() ? displayName : undefined
    }).subscribe({
      next: () => this.router.navigateByUrl('/home'),
      error: err => {
        this.submitting.set(false);
        const msg = err?.error?.message
          ?? (Array.isArray(err?.error?.errors) ? err.error.errors.join(' ') : null)
          ?? 'Registration failed.';
        this.errorMessage.set(msg);
      }
    });
  }
}
