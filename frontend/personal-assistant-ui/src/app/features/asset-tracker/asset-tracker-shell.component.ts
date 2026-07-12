import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { InvestmentsComponent } from './investments/investments.component';
import { PreciousMetalsComponent } from './precious-metals/precious-metals.component';
import { PossessionsComponent } from './possessions/possessions.component';
import { LiabilityAccountsComponent } from './liability-accounts/liability-accounts.component';

type MainTab = 'dashboard' | 'assets' | 'liabilities' | 'settings';
type AssetTab = 'investments' | 'real-estate' | 'precious-metals' | 'jewellery' | 'personal-assets';
type LiabilityTab = 'loans' | 'debts' | 'mortgages' | 'credit-cards';

@Component({
  selector: 'app-asset-tracker',
  imports: [CommonModule, RouterLink, InvestmentsComponent, PreciousMetalsComponent, PossessionsComponent, LiabilityAccountsComponent],
  template: `
    <section class="container py-4">
      <a routerLink="/home" class="text-muted-soft small text-decoration-none">← Home</a>
      <div class="d-flex align-items-center gap-2 mt-2 mb-3">
        <h1 class="page-title mb-0">Assets &amp; Liabilities</h1>
      </div>

      <nav class="nav nav-tabs neon-tabs mb-3" aria-label="Assets and liabilities sections">
        @for (tab of mainTabs; track tab.key) {
          <button type="button" class="nav-link" [class.active]="main() === tab.key" (click)="main.set(tab.key)">
            {{ tab.label }} @if (tab.key !== 'assets') { <span class="soon">Soon</span> }
          </button>
        }
      </nav>

      @switch (main()) {
        @case ('assets') {
          <nav class="subnav mb-4" aria-label="Asset sections">
            @for (tab of assetTabs; track tab.key) {
              <button type="button" [class.active]="assetTab() === tab.key" (click)="assetTab.set(tab.key)">
                {{ tab.label }} @if (tab.comingSoon) { <span class="soon">Soon</span> }
              </button>
            }
          </nav>
          @if (assetTab() === 'investments') {
            <app-asset-tracker-investments />
          } @else if (assetTab() === 'precious-metals') {
            <app-asset-tracker-precious-metals />
          } @else if (assetTab() === 'jewellery') {
            <app-asset-tracker-possessions mode="jewellery" />
          } @else if (assetTab() === 'personal-assets') {
            <app-asset-tracker-possessions mode="personal-assets" />
          } @else {
            <ng-container *ngTemplateOutlet="comingSoon; context: { $implicit: assetLabel() }" />
          }
        }
        @case ('liabilities') {
          <nav class="subnav mb-4" aria-label="Liability sections">
            @for (tab of liabilityTabs; track tab.key) {
              <button type="button" [class.active]="liabilityTab() === tab.key" (click)="liabilityTab.set(tab.key)">
                {{ tab.label }} @if (tab.comingSoon) { <span class="soon">Soon</span> }
              </button>
            }
          </nav>
          @if (liabilityTab() === 'loans') {
            <app-asset-tracker-liability-accounts mode="loans" />
          } @else if (liabilityTab() === 'debts') {
            <app-asset-tracker-liability-accounts mode="debts" />
          } @else if (liabilityTab() === 'credit-cards') {
            <app-asset-tracker-liability-accounts mode="credit-cards" />
          } @else {
            <ng-container *ngTemplateOutlet="comingSoon; context: { $implicit: liabilityLabel() }" />
          }
        }
        @case ('dashboard') { <ng-container *ngTemplateOutlet="comingSoon; context: { $implicit: 'Dashboard' }" /> }
        @case ('settings') { <ng-container *ngTemplateOutlet="comingSoon; context: { $implicit: 'Settings' }" /> }
      }

      <ng-template #comingSoon let-name>
        <div class="surface coming-soon">
          <span class="eyebrow">Coming soon</span>
          <h2>{{ name }}</h2>
          <p class="text-muted-soft mb-0">This section is planned but not available in the current release.</p>
        </div>
      </ng-template>
    </section>
  `,
  styles: [`
    .page-title { font-size: 1.55rem; font-weight: 650; }
    .neon-tabs { gap: .2rem; }
    .neon-tabs .nav-link { color: var(--fg-muted); background: transparent; border: 1px solid transparent; }
    .neon-tabs .nav-link.active { color: var(--fg); background: var(--surface); border-color: var(--neon); box-shadow: 0 -2px 10px var(--neon-soft); }
    .soon { margin-left: .35rem; padding: .08rem .35rem; border: 1px solid var(--border-strong); border-radius: 999px; color: var(--fg-subtle); font-size: .6rem; text-transform: uppercase; letter-spacing: .04em; }
    .subnav { display: flex; gap: .45rem; overflow-x: auto; padding-bottom: .2rem; }
    .subnav button { white-space: nowrap; border: 1px solid var(--border); border-radius: 999px; padding: .42rem .8rem; color: var(--fg-muted); background: var(--surface); }
    .subnav button.active { color: var(--neon); border-color: var(--neon); }
    .coming-soon { padding: 3rem 1.25rem; text-align: center; }
    .coming-soon h2 { margin: .35rem 0; font-size: 1.35rem; }
    .eyebrow { color: var(--neon); font-size: .72rem; letter-spacing: .12em; text-transform: uppercase; }
  `]
})
export class AssetTrackerShellComponent {
  readonly main = signal<MainTab>('assets');
  readonly assetTab = signal<AssetTab>('investments');
  readonly liabilityTab = signal<LiabilityTab>('loans');
  readonly mainTabs: { key: MainTab; label: string }[] = [
    { key: 'dashboard', label: 'Dashboard' }, { key: 'assets', label: 'Assets' },
    { key: 'liabilities', label: 'Liabilities' }, { key: 'settings', label: 'Settings' }
  ];
  readonly assetTabs: { key: AssetTab; label: string; comingSoon?: boolean }[] = [
    { key: 'investments', label: 'Investments' }, { key: 'real-estate', label: 'Real Estate', comingSoon: true },
    { key: 'precious-metals', label: 'Precious Metals' }, { key: 'jewellery', label: 'Jewellery' },
    { key: 'personal-assets', label: 'Personal Assets' }
  ];
  readonly liabilityTabs: { key: LiabilityTab; label: string; comingSoon?: boolean }[] = [
    { key: 'loans', label: 'Loans' }, { key: 'debts', label: 'Debts' },
    { key: 'mortgages', label: 'Mortgages', comingSoon: true }, { key: 'credit-cards', label: 'Credit Cards' }
  ];
  assetLabel = () => this.assetTabs.find(x => x.key === this.assetTab())?.label ?? '';
  liabilityLabel = () => this.liabilityTabs.find(x => x.key === this.liabilityTab())?.label ?? '';
}
