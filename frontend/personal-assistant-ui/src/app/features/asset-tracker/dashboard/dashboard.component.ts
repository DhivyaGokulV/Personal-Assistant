import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AssetTrackerApi } from '../asset-tracker.api';
import { AssetTrackerDashboard, fmtMoney, isoOffset } from '../asset-tracker.models';
import { LineChartComponent } from '../shared/line-chart.component';
import { PieChartComponent } from '../shared/pie-chart.component';

type Section = 'assets' | 'investments' | 'liabilities';

@Component({
  selector: 'app-asset-tracker-dashboard',
  imports: [CommonModule, FormsModule, LineChartComponent, PieChartComponent],
  template: `
    <div class="dash-shell">
      <!-- Net worth + presets -->
      <div class="surface p-3 mb-3">
        <div class="d-flex flex-wrap align-items-end gap-3">
          <div>
            <div class="kpi-label">Net Worth</div>
            <div class="net-worth" [ngClass]="netWorthTone()">{{ view() ? fmtMoney(view()!.netWorth) : '—' }}</div>
          </div>
          <div class="ms-auto d-flex flex-wrap gap-2 align-items-end">
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
            <div class="presets">
              <button class="btn-link-soft btn-sm" type="button" (click)="setRange(-7)">7d</button>
              <button class="btn-link-soft btn-sm" type="button" (click)="setRange(-30)">30d</button>
              <button class="btn-link-soft btn-sm" type="button" (click)="setRange(-90)">90d</button>
              <button class="btn-link-soft btn-sm" type="button" (click)="setRange(-365)">1y</button>
              <button class="btn-link-soft btn-sm" type="button" (click)="setAllTime()">All</button>
            </div>
          </div>
        </div>
      </div>

      <!-- KPI tiles -->
      @if (view(); as v) {
        <div class="row g-3 mb-3">
          <div class="col-12 col-md-4">
            <button type="button" class="kpi-tile" [class.is-selected]="section() === 'assets'" (click)="section.set('assets')">
              <div class="kpi-label">Assets</div>
              <div class="kpi-value tone-cyan">{{ fmtMoney(v.totalAssets) }}</div>
            </button>
          </div>
          <div class="col-12 col-md-4">
            <button type="button" class="kpi-tile" [class.is-selected]="section() === 'investments'" (click)="section.set('investments')">
              <div class="kpi-label">Investments</div>
              <div class="kpi-value tone-violet">{{ fmtMoney(v.totalInvestments) }}</div>
            </button>
          </div>
          <div class="col-12 col-md-4">
            <button type="button" class="kpi-tile" [class.is-selected]="section() === 'liabilities'" (click)="section.set('liabilities')">
              <div class="kpi-label">Liabilities</div>
              <div class="kpi-value tone-red">{{ fmtMoney(v.totalLiabilities) }}</div>
            </button>
          </div>
        </div>

        <!-- Diversification + history for selected section -->
        <h3 class="section-title">{{ sectionTitle() }} — Diversification</h3>
        <div class="surface p-3 mb-3">
          <app-pie-chart [data]="currentBreakdown()" />
        </div>

        <h3 class="section-title">{{ sectionTitle() }} — Trend</h3>
        <div class="surface p-3 mb-3">
          <app-line-chart [series]="currentSeries()" [color]="currentColor()" />
        </div>

        <h3 class="section-title">Net Worth — Trend</h3>
        <div class="surface p-3">
          <app-line-chart [series]="v.netWorthSeries" color="var(--neon)" />
        </div>
      } @else if (loading()) {
        <div class="text-muted-soft small">Loading…</div>
      } @else if (errorMessage()) {
        <div class="alert alert-danger small">{{ errorMessage() }}</div>
      }
    </div>
  `,
  styles: [`
    .kpi-label {
      font-size: 0.78rem; color: var(--fg-muted);
      text-transform: uppercase; letter-spacing: 0.05em;
      margin-bottom: 0.4rem;
    }
    .net-worth {
      font-size: 2.1rem; font-weight: 700; line-height: 1;
      text-shadow: 0 0 10px var(--neon-soft);
    }
    .net-worth.tone-positive { color: var(--neon); }
    .net-worth.tone-negative { color: var(--danger); }

    .presets { display: inline-flex; gap: 0.25rem; flex-wrap: wrap; }

    .kpi-tile {
      width: 100%;
      padding: 1rem 1.1rem;
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: var(--radius-md);
      cursor: pointer;
      text-align: left;
      transition: all 160ms ease;
    }
    .kpi-tile:hover { border-color: var(--border-strong); }
    .kpi-tile.is-selected {
      border-color: var(--neon);
      box-shadow: 0 0 12px var(--neon-soft);
    }
    .kpi-value { font-size: 1.4rem; font-weight: 600; }
    .tone-cyan { color: var(--neon-cyan); }
    .tone-violet { color: var(--primary); }
    .tone-red { color: var(--danger); }

    .section-title {
      font-size: 0.85rem; font-weight: 600;
      margin: 0.5rem 0;
      color: var(--fg-muted);
      text-transform: uppercase; letter-spacing: 0.05em;
    }

    .btn-link-soft { background: transparent; border: 1px solid var(--border-strong); color: var(--fg-muted); padding: 0.3rem 0.6rem; border-radius: var(--radius-sm); cursor: pointer; transition: all 120ms ease; font-size: 0.8rem; }
    .btn-link-soft:hover { color: var(--neon); border-color: var(--neon); }
  `]
})
export class AssetTrackerDashboardComponent {
  private readonly api = inject(AssetTrackerApi);
  fmtMoney = fmtMoney;

  from = isoOffset(-90);
  to = isoOffset(0);

  readonly view = signal<AssetTrackerDashboard | null>(null);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly section = signal<Section>('assets');

  readonly netWorthTone = computed(() => {
    const v = this.view();
    if (!v) return '';
    return v.netWorth >= 0 ? 'tone-positive' : 'tone-negative';
  });

  sectionTitle(): string {
    return this.section() === 'assets' ? 'Assets' : this.section() === 'investments' ? 'Investments' : 'Liabilities';
  }

  currentBreakdown() {
    const v = this.view();
    if (!v) return [];
    return this.section() === 'assets' ? v.assetsBreakdown
      : this.section() === 'investments' ? v.investmentsBreakdown
      : v.liabilitiesBreakdown;
  }

  currentSeries() {
    const v = this.view();
    if (!v) return [];
    return this.section() === 'assets' ? v.assetsSeries
      : this.section() === 'investments' ? v.investmentsSeries
      : v.liabilitiesSeries;
  }

  currentColor(): string {
    return this.section() === 'assets' ? 'var(--neon-cyan)'
      : this.section() === 'investments' ? 'var(--primary)'
      : 'var(--danger)';
  }

  constructor() { this.load(); }

  setRange(daysOffset: number): void {
    this.from = isoOffset(daysOffset);
    this.to = isoOffset(0);
    this.load();
  }

  setAllTime(): void {
    this.from = '2020-01-01';
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
