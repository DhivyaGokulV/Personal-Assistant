import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AccountsComponent } from './accounts.component';
import { CategoriesComponent } from './categories.component';
import { PaymentTypesComponent } from './payment-types.component';
import { TagsComponent } from './tags.component';

type SubTab = 'accounts' | 'categories' | 'payment-types' | 'tags';

@Component({
  selector: 'app-finance-settings',
  imports: [CommonModule, AccountsComponent, CategoriesComponent, PaymentTypesComponent, TagsComponent],
  template: `
    <div class="settings-shell">
      <ul class="nav nav-pills sub-tabs mb-3">
        <li class="nav-item"><button class="nav-link" [class.active]="active() === 'accounts'" (click)="active.set('accounts')">Accounts</button></li>
        <li class="nav-item"><button class="nav-link" [class.active]="active() === 'categories'" (click)="active.set('categories')">Categories</button></li>
        <li class="nav-item"><button class="nav-link" [class.active]="active() === 'payment-types'" (click)="active.set('payment-types')">Payment Types</button></li>
        <li class="nav-item"><button class="nav-link" [class.active]="active() === 'tags'" (click)="active.set('tags')">Tags</button></li>
      </ul>

      @switch (active()) {
        @case ('accounts')      { <app-finance-accounts /> }
        @case ('categories')    { <app-finance-categories /> }
        @case ('payment-types') { <app-finance-payment-types /> }
        @case ('tags')          { <app-finance-tags /> }
      }
    </div>
  `,
  styles: [`
    .sub-tabs .nav-link {
      color: var(--fg-muted); background: transparent;
      border: 1px solid var(--border-strong); border-radius: var(--radius-sm);
      padding: 0.35rem 0.85rem; font-size: 0.88rem;
    }
    .sub-tabs .nav-link.active {
      color: var(--neon); border-color: var(--neon);
      box-shadow: 0 0 8px var(--neon-soft);
      background: transparent;
    }
    .sub-tabs { gap: 0.5rem; }
  `]
})
export class FinanceSettingsComponent {
  readonly active = signal<SubTab>('accounts');
}
