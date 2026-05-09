import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AssetTrackerDashboardComponent } from './dashboard/dashboard.component';
import { AssetsComponent } from './assets/assets.component';
import { InvestmentsComponent } from './investments/investments.component';
import { LiabilitiesComponent } from './liabilities/liabilities.component';

type Tab = 'dashboard' | 'assets' | 'investments' | 'liabilities';

@Component({
  selector: 'app-asset-tracker',
  imports: [
    CommonModule,
    RouterLink,
    AssetTrackerDashboardComponent,
    AssetsComponent,
    InvestmentsComponent,
    LiabilitiesComponent
  ],
  template: `
    <section class="container py-4">
      <div class="d-flex align-items-center mb-3 gap-2">
        <a routerLink="/home" class="text-muted-soft small">← Home</a>
      </div>
      <h1 class="page-title mb-3">Asset Tracker</h1>

      <ul class="nav nav-tabs neon-tabs mb-3" role="tablist">
        <li class="nav-item"><button class="nav-link" [class.active]="active() === 'dashboard'" (click)="active.set('dashboard')">Dashboard</button></li>
        <li class="nav-item"><button class="nav-link" [class.active]="active() === 'assets'" (click)="active.set('assets')">Assets</button></li>
        <li class="nav-item"><button class="nav-link" [class.active]="active() === 'investments'" (click)="active.set('investments')">Investments</button></li>
        <li class="nav-item"><button class="nav-link" [class.active]="active() === 'liabilities'" (click)="active.set('liabilities')">Liabilities</button></li>
      </ul>

      @switch (active()) {
        @case ('dashboard')   { <app-asset-tracker-dashboard /> }
        @case ('assets')      { <app-asset-tracker-assets /> }
        @case ('investments') { <app-asset-tracker-investments /> }
        @case ('liabilities') { <app-asset-tracker-liabilities /> }
      }
    </section>
  `,
  styles: [`
    .page-title { font-size: 1.5rem; font-weight: 600; }
    .neon-tabs .nav-link {
      color: var(--fg-muted);
      background: transparent;
      border: 1px solid transparent;
      border-bottom: none;
      border-radius: var(--radius-sm) var(--radius-sm) 0 0;
      padding: 0.5rem 1rem;
    }
    .neon-tabs .nav-link.active {
      color: var(--fg);
      background: var(--surface);
      border: 1px solid var(--neon);
      border-bottom: 1px solid var(--surface);
      box-shadow: 0 -3px 12px var(--neon-soft);
    }
  `]
})
export class AssetTrackerShellComponent {
  readonly active = signal<Tab>('dashboard');
}
