import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { FinanceApi } from '../finance.api';
import { DashboardView, GroupStat, fmtMoney } from '../finance.models';

function isoOffset(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

type GroupKey = 'category' | 'account' | 'payment' | 'tag';

@Component({
  selector: 'app-finance-dashboard',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="dash-shell">
      <!-- Period selector -->
      <div class="surface p-3 mb-3">
        <div class="d-flex flex-wrap align-items-end gap-2">
          <div>
            <label class="form-label small text-muted-soft mb-1">From</label>
            <input type="date" class="form-control form-control-sm" [(ngModel)]="from" />
          </div>
          <div>
            <label class="form-label small text-muted-soft mb-1">To</label>
            <input type="date" class="form-control form-control-sm" [(ngModel)]="to" />
          </div>
          <button class="btn-neon btn-sm" type="button" [disabled]="loading()" (click)="load()">
            {{ loading() ? 'Loading…' : 'Refresh' }}
          </button>
          <div class="ms-auto d-flex gap-2 flex-wrap">
            <button class="btn-link-soft btn-sm" type="button" (click)="setRange(-7)">Last 7d</button>
            <button class="btn-link-soft btn-sm" type="button" (click)="setRange(-30)">Last 30d</button>
            <button class="btn-link-soft btn-sm" type="button" (click)="setMonthToDate()">Month-to-date</button>
            <button class="btn-link-soft btn-sm" type="button" (click)="setRange(-90)">Last 90d</button>
          </div>
        </div>
      </div>

      @if (view(); as v) {
        <!-- Top KPI tiles -->
        <div class="row g-3 mb-3">
          <div class="col-12 col-md-3">
            <div class="kpi neon">
              <div class="kpi-label">Starting Standing</div>
              <div class="kpi-value">{{ fmtMoney(v.totalStartingStanding) }}</div>
              <div class="kpi-sub small text-muted-soft">as of {{ v.from }}</div>
            </div>
          </div>
          <div class="col-12 col-md-3">
            <div class="kpi neon-cyan">
              <div class="kpi-label">Current Standing</div>
              <div class="kpi-value">{{ fmtMoney(v.totalCurrentStanding) }}</div>
              <div class="kpi-sub small text-muted-soft">as of {{ v.to }}</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="kpi">
              <div class="kpi-label">Total Credits</div>
              <div class="kpi-value tone-green">{{ fmtMoney(v.totalCredits) }}</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="kpi">
              <div class="kpi-label">Total Debits</div>
              <div class="kpi-value tone-red">{{ fmtMoney(v.totalDebits) }}</div>
            </div>
          </div>
        </div>

        <!-- Per-account standings -->
        <h3 class="section-title">Accounts</h3>
        <div class="table-wrap surface mb-3">
          <table class="dash-table">
            <thead>
              <tr>
                <th>Account</th>
                <th class="text-end">Starting</th>
                <th class="text-end">Current</th>
                <th class="text-end">Δ Change</th>
              </tr>
            </thead>
            <tbody>
              @for (a of v.accounts; track a.accountId) {
                <tr>
                  <td><strong>{{ a.accountName }}</strong></td>
                  <td class="text-end">{{ fmtMoney(a.startingStanding) }}</td>
                  <td class="text-end">{{ fmtMoney(a.currentStanding) }}</td>
                  <td class="text-end" [ngClass]="a.delta >= 0 ? 'tone-green' : 'tone-red'">
                    {{ a.delta >= 0 ? '+' : '' }}{{ fmtMoney(a.delta) }}
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <!-- Stats group with sub-tabs -->
        <h3 class="section-title">Statistics</h3>
        <ul class="nav nav-pills sub-tabs mb-2">
          <li class="nav-item"><button class="nav-link" [class.active]="group() === 'category'" (click)="group.set('category')">By Category</button></li>
          <li class="nav-item"><button class="nav-link" [class.active]="group() === 'account'" (click)="group.set('account')">By Account</button></li>
          <li class="nav-item"><button class="nav-link" [class.active]="group() === 'payment'" (click)="group.set('payment')">By Payment</button></li>
          <li class="nav-item"><button class="nav-link" [class.active]="group() === 'tag'" (click)="group.set('tag')">By Tag</button></li>
        </ul>

        <div class="table-wrap surface">
          <table class="dash-table">
            <thead>
              <tr>
                <th>{{ groupHeader() }}</th>
                <th class="text-end">Debits</th>
                <th class="text-end">Credits</th>
                <th class="text-end">Net</th>
                <th>Share</th>
              </tr>
            </thead>
            <tbody>
              @for (row of selectedGroup(); track row.key) {
                <tr>
                  <td>{{ row.key }}</td>
                  <td class="text-end tone-red">{{ fmtMoney(row.debits) }}</td>
                  <td class="text-end tone-green">{{ fmtMoney(row.credits) }}</td>
                  <td class="text-end" [ngClass]="row.net >= 0 ? 'tone-green' : 'tone-red'">{{ fmtMoney(row.net) }}</td>
                  <td>
                    <div class="bar-wrap">
                      <div class="bar bar-debit" [style.width.%]="sharePercent(row.debits, totalDebitsForGroup())"></div>
                    </div>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="5" class="text-muted-soft text-center py-3">No data in this period.</td></tr>
              }
            </tbody>
          </table>
        </div>
      } @else if (errorMessage()) {
        <div class="alert alert-danger small mb-0">{{ errorMessage() }}</div>
      } @else if (loading()) {
        <div class="text-muted-soft small">Loading…</div>
      }
    </div>
  `,
  styles: [`
    .kpi {
      padding: 1rem 1.1rem;
      background: var(--surface);
      border-radius: var(--radius-md);
    }
    .kpi:not(.neon):not(.neon-cyan) {
      border: 1px solid var(--border);
    }
    .kpi-label { font-size: 0.78rem; color: var(--fg-muted); letter-spacing: 0.04em; text-transform: uppercase; margin-bottom: 0.4rem; }
    .kpi-value { font-size: 1.4rem; font-weight: 600; }
    .kpi-sub { margin-top: 0.2rem; }

    .section-title { font-size: 0.95rem; font-weight: 600; margin: 0.5rem 0; color: var(--fg-muted); text-transform: uppercase; letter-spacing: 0.05em; }

    .table-wrap { overflow: auto; border: 1px solid var(--border); border-radius: var(--radius-md); }
    .dash-table { width: 100%; border-collapse: collapse; font-size: 0.88rem; }
    .dash-table thead th { background: var(--surface-2); color: var(--fg-muted); font-weight: 600; text-align: left; padding: 0.5rem 0.85rem; border-bottom: 1px solid var(--border-strong); white-space: nowrap; }
    .dash-table tbody td { padding: 0.55rem 0.85rem; border-bottom: 1px solid var(--border); }
    .dash-table tbody tr:hover td { background: var(--surface-2); }

    .tone-green { color: var(--success); }
    .tone-red { color: var(--danger); }

    .bar-wrap { background: var(--surface-2); height: 6px; border-radius: 999px; min-width: 80px; }
    .bar { height: 6px; border-radius: 999px; }
    .bar-debit { background: var(--danger); }

    .sub-tabs .nav-link { color: var(--fg-muted); background: transparent; border: 1px solid var(--border-strong); border-radius: var(--radius-sm); padding: 0.3rem 0.7rem; font-size: 0.85rem; }
    .sub-tabs .nav-link.active { color: var(--neon); border-color: var(--neon); box-shadow: 0 0 8px var(--neon-soft); }
    .sub-tabs { gap: 0.4rem; }

    .btn-link-soft { background: transparent; border: 1px solid var(--border-strong); color: var(--fg-muted); padding: 0.3rem 0.7rem; border-radius: var(--radius-sm); cursor: pointer; transition: all 120ms ease; }
    .btn-link-soft:hover { color: var(--neon); border-color: var(--neon); }
  `]
})
export class FinanceDashboardComponent {
  private readonly api = inject(FinanceApi);
  readonly fmtMoney = fmtMoney;

  from = isoOffset(-30);
  to = isoOffset(0);

  readonly view = signal<DashboardView | null>(null);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly group = signal<GroupKey>('category');

  readonly selectedGroup = computed<GroupStat[]>(() => {
    const v = this.view();
    if (!v) return [];
    switch (this.group()) {
      case 'category': return v.byCategory;
      case 'account': return v.byAccount;
      case 'payment': return v.byPaymentType;
      case 'tag': return v.byTag;
    }
  });

  readonly totalDebitsForGroup = computed(() => {
    const max = Math.max(0, ...this.selectedGroup().map(r => r.debits));
    return max;
  });

  groupHeader(): string {
    return this.group() === 'category' ? 'Category'
      : this.group() === 'account' ? 'Account'
      : this.group() === 'payment' ? 'Payment Type'
      : 'Tag';
  }

  sharePercent(value: number, max: number): number {
    if (max <= 0) return 0;
    return Math.round((value / max) * 100);
  }

  constructor() { this.load(); }

  setRange(daysOffset: number): void {
    this.from = isoOffset(daysOffset);
    this.to = isoOffset(0);
    this.load();
  }

  setMonthToDate(): void {
    const d = new Date();
    const first = new Date(d.getFullYear(), d.getMonth(), 1);
    this.from = `${first.getFullYear()}-${String(first.getMonth() + 1).padStart(2, '0')}-01`;
    this.to = isoOffset(0);
    this.load();
  }

  async load(): Promise<void> {
    if (!this.from || !this.to) return;
    this.loading.set(true);
    this.errorMessage.set(null);
    try {
      this.view.set(await firstValueFrom(this.api.getDashboard(this.from, this.to)));
    } catch (err: any) {
      this.errorMessage.set(err?.error?.message ?? 'Failed to load dashboard.');
    } finally {
      this.loading.set(false);
    }
  }
}
