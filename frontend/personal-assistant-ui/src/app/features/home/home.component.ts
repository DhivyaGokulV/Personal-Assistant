import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

interface ModuleTile {
  title: string;
  description: string;
  icon: string;
  route: string | null;
  enabled: boolean;
}

@Component({
  selector: 'app-home',
  imports: [CommonModule, RouterLink],
  template: `
    <section class="home-shell">
      <div class="container py-4">
        <div class="mb-4">
          <h1 class="page-title">Welcome back</h1>
          <p class="text-muted-soft mb-0">Choose a module to get started.</p>
        </div>

        <div class="row g-3">
          @for (tile of tiles; track tile.title) {
            <div class="col-12 col-sm-6 col-md-4 col-lg-3">
              <a *ngIf="tile.enabled && tile.route as r"
                 [routerLink]="r"
                 class="tile neon"
                 [attr.aria-label]="tile.title">
                <div class="tile-icon">{{ tile.icon }}</div>
                <div class="tile-title">{{ tile.title }}</div>
                <div class="tile-desc">{{ tile.description }}</div>
              </a>
              <div *ngIf="!tile.enabled" class="tile tile-disabled">
                <div class="tile-icon">{{ tile.icon }}</div>
                <div class="tile-title">{{ tile.title }}</div>
                <div class="tile-desc">{{ tile.description }}</div>
                <span class="badge-soon">Coming soon</span>
              </div>
            </div>
          }
        </div>
      </div>
    </section>
  `,
  styles: [`
    .home-shell { min-height: calc(100vh - 56px); background: var(--bg); }
    .page-title { font-size: 1.6rem; font-weight: 600; margin: 0; }
    .tile {
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
      padding: 1.25rem;
      height: 100%;
      min-height: 150px;
      text-decoration: none;
      color: var(--fg);
      position: relative;
      overflow: hidden;
    }
    .tile-icon { font-size: 1.1rem; line-height: 1; font-weight: 700; color: var(--neon-cyan); }
    .tile-title { font-weight: 600; font-size: 1.05rem; }
    .tile-desc { font-size: 0.85rem; color: var(--fg-muted); }
    .tile-disabled {
      background: var(--surface);
      border: 1px dashed var(--border-strong);
      border-radius: var(--radius-md);
      opacity: 0.7;
      cursor: not-allowed;
    }
    .badge-soon {
      position: absolute;
      top: 0.75rem;
      right: 0.75rem;
      font-size: 0.7rem;
      letter-spacing: 0.05em;
      padding: 0.15rem 0.5rem;
      border: 1px solid var(--border-strong);
      border-radius: 999px;
      color: var(--fg-subtle);
      text-transform: uppercase;
    }
  `]
})
export class HomeComponent {
  readonly tiles: ModuleTile[] = [
    { title: 'Task Management', description: 'Daily, periodic and to-do tasks', icon: 'OK', route: '/tasks', enabled: true },
    { title: 'Finance Management', description: 'Track spending and budgets', icon: 'INR', route: '/finance', enabled: true },
    { title: 'Asset Tracker', description: 'Assets, investments, liabilities', icon: 'AS', route: '/assets', enabled: true },
    { title: 'Time Tracker', description: 'Where your hours go', icon: 'TM', route: '/time', enabled: true },
    { title: 'Health & Workouts', description: 'Movement, nutrition, measurements', icon: 'HL', route: '/health', enabled: true },
    { title: 'Goal Tracker', description: 'Plans, goals and steps', icon: 'GO', route: '/goals', enabled: true },
    { title: 'Passwords', description: 'Client-encrypted vault', icon: 'PW', route: '/passwords', enabled: true },
    { title: 'Wardrobe Manager', description: 'Organize outfits and clothing', icon: 'WM', route: null, enabled: false },
    { title: 'Notes', description: 'Quick capture and longform', icon: 'NT', route: null, enabled: false }
  ];
}
