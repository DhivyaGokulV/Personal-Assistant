import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeToggleComponent } from './theme-toggle.component';

@Component({
  selector: 'app-header',
  imports: [CommonModule, RouterLink, ThemeToggleComponent],
  template: `
    <header class="app-header">
      <div class="container-fluid d-flex align-items-center gap-3 py-2">
        <a routerLink="/home" class="brand neon-text">PA<span class="brand-dot">.</span></a>
        <div class="ms-auto d-flex align-items-center gap-2">
          @if (auth.user(); as user) {
            <span class="d-none d-sm-inline text-muted-soft small">{{ user.displayName || user.email }}</span>
            <button type="button" class="btn-neon btn-sm" (click)="logout()">Sign out</button>
          }
          <app-theme-toggle />
        </div>
      </div>
    </header>
  `,
  styles: [`
    .app-header {
      border-bottom: 1px solid var(--border);
      background: var(--surface);
    }
    .brand {
      font-family: 'Segoe UI', sans-serif;
      font-weight: 700;
      letter-spacing: 0.04em;
      font-size: 1.15rem;
      text-decoration: none;
    }
    .brand-dot { color: var(--neon-cyan); }
    .btn-sm { padding: 0.3rem 0.75rem; font-size: 0.85rem; }
  `]
})
export class AppHeaderComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  logout(): void {
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }
}
