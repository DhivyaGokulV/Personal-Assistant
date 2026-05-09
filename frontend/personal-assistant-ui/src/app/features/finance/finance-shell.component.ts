import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FinanceDashboardComponent } from './dashboard/dashboard.component';
import { ExpenseTrackerComponent } from './expense-tracker/expense-tracker.component';
import { BudgetsComponent } from './budgets/budgets.component';
import { FinanceSettingsComponent } from './settings/finance-settings.component';

type Tab = 'dashboard' | 'expense' | 'budget' | 'settings';

@Component({
  selector: 'app-finance',
  imports: [
    CommonModule,
    RouterLink,
    FinanceDashboardComponent,
    ExpenseTrackerComponent,
    BudgetsComponent,
    FinanceSettingsComponent
  ],
  template: `
    <section class="container py-4">
      <div class="d-flex align-items-center mb-3 gap-2">
        <a routerLink="/home" class="text-muted-soft small">← Home</a>
      </div>
      <h1 class="page-title mb-3">Finance Management</h1>

      <ul class="nav nav-tabs neon-tabs mb-3" role="tablist">
        <li class="nav-item"><button class="nav-link" [class.active]="active() === 'dashboard'" (click)="active.set('dashboard')">Dashboard</button></li>
        <li class="nav-item"><button class="nav-link" [class.active]="active() === 'expense'" (click)="active.set('expense')">Expense Tracker</button></li>
        <li class="nav-item"><button class="nav-link" [class.active]="active() === 'budget'" (click)="active.set('budget')">Budget</button></li>
        <li class="nav-item"><button class="nav-link" [class.active]="active() === 'settings'" (click)="active.set('settings')">Settings</button></li>
      </ul>

      @switch (active()) {
        @case ('dashboard') { <app-finance-dashboard /> }
        @case ('expense')   { <app-expense-tracker /> }
        @case ('budget')    { <app-finance-budgets /> }
        @case ('settings')  { <app-finance-settings /> }
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
export class FinanceShellComponent {
  readonly active = signal<Tab>('dashboard');
}
