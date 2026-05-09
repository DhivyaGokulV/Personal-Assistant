import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AssetTrackerApi } from '../asset-tracker.api';
import {
  Investment,
  InvestmentDetail,
  InvestmentGroup,
  InvestmentPriceEntry,
  InvestmentStatus,
  InvestmentTx,
  InvestmentTxType,
  ReportFormat,
  fmtMoney,
  fmtUnits,
  isoToday
} from '../asset-tracker.models';
import { TagPickerComponent } from '../shared/tag-picker.component';
import { LineChartComponent } from '../shared/line-chart.component';

interface GroupForm { id?: string; name: string; description: string; tagId: string | null; status: InvestmentStatus; }
interface InvForm {
  id?: string;
  groupId: string;
  name: string;
  description: string;
  tagId: string | null;
  unit: string;
  currentPrice: number | null;
}
interface TxForm { id?: string; date: string; type: InvestmentTxType; units: number; price: number | null; note: string; }
interface PriceForm { id?: string; asOf: string; price: number; note: string; }

type ModalTab = 'analytics' | 'history';

@Component({
  selector: 'app-asset-tracker-investments',
  imports: [CommonModule, FormsModule, TagPickerComponent, LineChartComponent],
  template: `
    <div class="inv-shell">
      <!-- Aggregate strip -->
      @if (totals(); as t) {
        <div class="surface p-3 mb-3">
          <div class="d-flex flex-wrap gap-3">
            <div>
              <div class="kpi-label">Invested</div>
              <div class="kpi-value">{{ fmtMoney(t.invested) }}</div>
            </div>
            <div>
              <div class="kpi-label">Current Value</div>
              <div class="kpi-value tone-cyan">{{ fmtMoney(t.current) }}</div>
            </div>
            <div>
              <div class="kpi-label">Profit / Loss</div>
              <div class="kpi-value" [ngClass]="t.pl >= 0 ? 'tone-green' : 'tone-red'">{{ fmtMoney(t.pl) }}</div>
            </div>
          </div>
        </div>
      }

      <!-- Top bar -->
      <div class="d-flex flex-wrap align-items-center gap-2 mb-3">
        <label class="form-label small text-muted-soft mb-0">Filter:</label>
        <select class="form-select form-select-sm w-auto" [ngModel]="statusFilter()" (ngModelChange)="onStatusChange($event)">
          <option [ngValue]="1">Active</option>
          <option [ngValue]="2">Inactive</option>
          <option [ngValue]="0">All</option>
        </select>
        <div class="ms-auto d-flex gap-2 flex-wrap">
          <button class="btn-link-soft btn-sm" type="button" (click)="downloadReport('Csv')">CSV</button>
          <button class="btn-link-soft btn-sm" type="button" (click)="downloadReport('Xlsx')">Excel</button>
          <button class="btn-link-soft btn-sm" type="button" (click)="downloadReport('Pdf')">PDF</button>
          <button class="btn-neon btn-sm" type="button" (click)="openCreateGroup()">+ New Investment Group</button>
        </div>
      </div>

      <!-- Group form -->
      @if (groupForm(); as f) {
        <div class="surface p-3 mb-3">
          <div class="row g-2 align-items-end">
            <div class="col-12 col-md-4">
              <label class="form-label small">Name</label>
              <input class="form-control form-control-sm" [(ngModel)]="f.name" />
            </div>
            <div class="col-12 col-md-4">
              <label class="form-label small">Description</label>
              <input class="form-control form-control-sm" [(ngModel)]="f.description" />
            </div>
            <div class="col-6 col-md-2">
              <label class="form-label small">Tag</label>
              <app-tag-picker [value]="f.tagId" (valueChange)="f.tagId = $event" />
            </div>
            <div class="col-6 col-md-1">
              <label class="form-label small">Status</label>
              <select class="form-select form-select-sm" [(ngModel)]="f.status">
                <option [ngValue]="1">Active</option>
                <option [ngValue]="2">Inactive</option>
              </select>
            </div>
            <div class="col-12 col-md-1 d-grid">
              <button class="btn-neon btn-sm" type="button" [disabled]="!f.name.trim() || saving()" (click)="saveGroup()">Save</button>
            </div>
          </div>
        </div>
      }

      <!-- Groups -->
      @if (loading()) { <div class="text-muted-soft small">Loading…</div> }
      @else if (groups().length === 0) {
        <div class="surface p-4 text-center text-muted-soft">No investment groups yet.</div>
      } @else {
        @for (g of groups(); track g.id) {
          <article class="group-card neon mb-3">
            <header class="group-header">
              <div>
                <div class="d-flex align-items-center gap-2 flex-wrap">
                  <h3 class="group-title">{{ g.name }}</h3>
                  @if (g.tag; as t) {
                    <span class="chip" [style.color]="t.color" [style.borderColor]="t.color">{{ t.name }}</span>
                  }
                  @if (g.status === 2) { <span class="status-pill is-inactive">Inactive</span> }
                </div>
                @if (g.description) { <p class="group-desc">{{ g.description }}</p> }
              </div>
              <div class="ms-auto d-flex align-items-center gap-3">
                <div class="text-end small">
                  <div class="kv-label">Invested</div>
                  <div class="kv-val">{{ fmtMoney(g.totalInvested) }}</div>
                </div>
                <div class="text-end small">
                  <div class="kv-label">Current</div>
                  <div class="kv-val tone-cyan">{{ fmtMoney(g.totalCurrentValue) }}</div>
                </div>
                <div class="text-end small">
                  <div class="kv-label">P/L</div>
                  <div class="kv-val" [ngClass]="g.profitLoss >= 0 ? 'tone-green' : 'tone-red'">{{ fmtMoney(g.profitLoss) }}</div>
                </div>
                <div class="d-flex gap-1">
                  <button class="icon-btn" type="button" title="Edit" (click)="startEditGroup(g)">✎</button>
                  <button class="icon-btn icon-danger" type="button" title="Delete" (click)="deleteGroup(g)">🗑</button>
                </div>
                <button class="btn-link-soft btn-sm" type="button" (click)="openCreateInvestment(g)">+ Investment</button>
              </div>
            </header>

            <!-- Investment form -->
            @if (invForm()?.groupId === g.id) {
              <div class="form-row">
                @if (invForm(); as f) {
                  <div class="row g-2 align-items-end">
                    <div class="col-12 col-md-3">
                      <label class="form-label small">Name *</label>
                      <input class="form-control form-control-sm" [(ngModel)]="f.name" />
                    </div>
                    <div class="col-12 col-md-3">
                      <label class="form-label small">Description</label>
                      <input class="form-control form-control-sm" [(ngModel)]="f.description" />
                    </div>
                    <div class="col-6 col-md-2">
                      <label class="form-label small">Tag</label>
                      <app-tag-picker [value]="f.tagId" (valueChange)="f.tagId = $event" />
                    </div>
                    <div class="col-6 col-md-2">
                      <label class="form-label small">Unit *</label>
                      <input class="form-control form-control-sm" [(ngModel)]="f.unit" placeholder="unit / gram / share" />
                    </div>
                    @if (!f.id) {
                      <div class="col-6 col-md-2">
                        <label class="form-label small">Current Price *</label>
                        <input type="number" min="0" step="0.0001" class="form-control form-control-sm" [(ngModel)]="f.currentPrice" />
                      </div>
                    }
                    <div class="col-12 d-flex gap-2 justify-content-end">
                      <button class="btn-link-soft btn-sm" type="button" (click)="invForm.set(null)">Cancel</button>
                      <button class="btn-neon btn-sm" type="button" [disabled]="!isInvValid(f) || saving()" (click)="saveInvestment()">{{ f.id ? 'Save Changes' : 'Create' }}</button>
                    </div>
                  </div>
                }
              </div>
            }

            @if (investmentsForGroup(g.id).length === 0) {
              <div class="text-muted-soft small px-2 py-2">No investments in this group for the current filter.</div>
            } @else {
              <div class="table-wrap">
                <table class="inv-table">
                  <thead>
                    <tr>
                      <th>Name</th>
                      <th class="text-end">Holding</th>
                      <th class="text-end">Current Value</th>
                      <th class="text-end">Current Price</th>
                      <th class="text-end">Invested</th>
                      <th class="text-end">P/L</th>
                      <th>Status</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (i of investmentsForGroup(g.id); track i.id) {
                      <tr>
                        <td>
                          <button type="button" class="link-name" (click)="openInvestment(i)">{{ i.name }}</button>
                          @if (i.tag; as t) {
                            <span class="chip ms-1" [style.color]="t.color" [style.borderColor]="t.color">{{ t.name }}</span>
                          }
                        </td>
                        <td class="text-end">{{ fmtUnits(i.unitsHolding) }} <span class="text-subtle small">{{ i.unit }}</span></td>
                        <td class="text-end">{{ fmtMoney(i.currentHoldingValue) }}</td>
                        <td class="text-end">{{ fmtMoney(i.currentPrice ?? 0) }}</td>
                        <td class="text-end">{{ fmtMoney(i.invested) }}</td>
                        <td class="text-end" [ngClass]="i.profitLoss >= 0 ? 'tone-green' : 'tone-red'">{{ fmtMoney(i.profitLoss) }}</td>
                        <td>
                          <span class="status-pill" [ngClass]="i.status === 1 ? 'is-active' : 'is-inactive'">
                            {{ i.status === 1 ? 'Active' : 'Inactive' }}
                          </span>
                        </td>
                        <td class="actions">
                          <button class="icon-btn icon-danger" type="button" title="Delete" (click)="deleteInvestment(i)">🗑</button>
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }
          </article>
        }
      }

      <!-- Investment detail modal -->
      @if (modal(); as m) {
        <div class="modal-backdrop" (click)="closeModal()"></div>
        <div class="modal-shell" role="dialog" aria-modal="true">
          <div class="modal-header">
            <div>
              <div class="d-flex align-items-center gap-2 flex-wrap">
                <h2 class="modal-title">{{ m.investment.name }}</h2>
                @if (m.investment.tag; as t) {
                  <span class="chip" [style.color]="t.color" [style.borderColor]="t.color">{{ t.name }}</span>
                }
                <span class="status-pill" [ngClass]="m.investment.status === 1 ? 'is-active' : 'is-inactive'">{{ m.investment.status === 1 ? 'Active' : 'Inactive' }}</span>
              </div>
              <div class="small text-muted-soft">
                {{ m.investment.groupName }} · {{ m.investment.unit }} · current price {{ fmtMoney(m.investment.currentPrice ?? 0) }}
              </div>
            </div>
            <div class="d-flex gap-2 align-items-center">
              <button class="btn-link-soft btn-sm" type="button" (click)="toggleEditInv()">{{ editingInv() ? 'Cancel edit' : 'Edit' }}</button>
              <button class="icon-btn" type="button" (click)="closeModal()">×</button>
            </div>
          </div>

          @if (editingInv()) {
            <div class="surface p-2 mb-3">
              <div class="row g-2 align-items-end">
                <div class="col-12 col-md-4">
                  <label class="form-label small">Name</label>
                  <input class="form-control form-control-sm" [(ngModel)]="modalForm.name" />
                </div>
                <div class="col-12 col-md-4">
                  <label class="form-label small">Description</label>
                  <input class="form-control form-control-sm" [(ngModel)]="modalForm.description" />
                </div>
                <div class="col-6 col-md-2">
                  <label class="form-label small">Tag</label>
                  <app-tag-picker [value]="modalForm.tagId" (valueChange)="modalForm.tagId = $event" />
                </div>
                <div class="col-6 col-md-2">
                  <label class="form-label small">Unit</label>
                  <input class="form-control form-control-sm" [(ngModel)]="modalForm.unit" />
                </div>
                <div class="col-12 d-flex gap-2 justify-content-end">
                  <button class="btn-neon btn-sm" type="button" [disabled]="saving()" (click)="saveEditInv()">Save Details</button>
                </div>
              </div>
            </div>
          }

          <ul class="nav nav-pills sub-tabs mb-2">
            <li class="nav-item"><button class="nav-link" [class.active]="modalTab() === 'analytics'" (click)="modalTab.set('analytics')">Analytics</button></li>
            <li class="nav-item"><button class="nav-link" [class.active]="modalTab() === 'history'" (click)="modalTab.set('history')">History</button></li>
          </ul>

          @if (modalTab() === 'analytics') {
            <div>
              <h3 class="section-title">Price history</h3>
              <div class="surface p-2 mb-3">
                <app-line-chart [series]="priceSeries()" color="var(--neon-cyan)" />
              </div>

              <h3 class="section-title">Holding value</h3>
              <div class="surface p-2 mb-3">
                <app-line-chart [series]="holdingValueSeries()" color="var(--primary)" />
              </div>

              <h3 class="section-title">Holding quantity</h3>
              <div class="surface p-2 mb-3">
                <app-line-chart [series]="holdingQuantitySeries()" color="var(--success)" />
              </div>

              <h3 class="section-title">Price entries</h3>
              <div class="d-flex justify-content-end mb-2">
                <button class="btn-link-soft btn-sm" type="button" (click)="openAddPrice()">+ Add Price Entry</button>
              </div>
              @if (priceForm(); as f) {
                <div class="surface p-2 mb-2">
                  <div class="row g-2 align-items-end">
                    <div class="col-6 col-md-3">
                      <label class="form-label small">As of</label>
                      <input type="date" class="form-control form-control-sm" [(ngModel)]="f.asOf" />
                    </div>
                    <div class="col-6 col-md-3">
                      <label class="form-label small">Price</label>
                      <input type="number" min="0" step="0.0001" class="form-control form-control-sm" [(ngModel)]="f.price" />
                    </div>
                    <div class="col-12 col-md-4">
                      <label class="form-label small">Note</label>
                      <input class="form-control form-control-sm" [(ngModel)]="f.note" />
                    </div>
                    <div class="col-12 col-md-2 d-grid gap-1">
                      <button class="btn-neon btn-sm" type="button" [disabled]="f.price <= 0 || saving()" (click)="savePrice()">Save</button>
                      <button class="btn-link-soft btn-sm" type="button" (click)="priceForm.set(null)">Cancel</button>
                    </div>
                  </div>
                </div>
              }
              <div class="table-wrap">
                <table class="hist-table">
                  <thead><tr><th>As of</th><th class="text-end">Price</th><th>Note</th><th></th></tr></thead>
                  <tbody>
                    @for (p of m.prices; track p.id) {
                      <tr>
                        <td>{{ p.asOf }}</td>
                        <td class="text-end">{{ fmtMoney(p.price, 4) }}</td>
                        <td class="text-muted-soft">{{ p.note ?? '—' }}</td>
                        <td class="actions">
                          <button class="icon-btn" type="button" title="Edit" (click)="startEditPrice(p)">✎</button>
                          <button class="icon-btn icon-danger" type="button" title="Delete" (click)="deletePrice(p)">🗑</button>
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          } @else {
            <div>
              <div class="d-flex justify-content-between mb-2 align-items-center">
                <h3 class="section-title m-0">Buy / Sell history</h3>
                <button class="btn-link-soft btn-sm" type="button" (click)="openAddTx()">+ New Buy/Sell</button>
              </div>
              @if (txForm(); as f) {
                <div class="surface p-2 mb-2">
                  <div class="row g-2 align-items-end">
                    <div class="col-6 col-md-2">
                      <label class="form-label small">Date</label>
                      <input type="date" class="form-control form-control-sm" [(ngModel)]="f.date" />
                    </div>
                    <div class="col-6 col-md-2">
                      <label class="form-label small">Type</label>
                      <select class="form-select form-select-sm" [(ngModel)]="f.type">
                        <option [ngValue]="1">Buy</option>
                        <option [ngValue]="2">Sell</option>
                      </select>
                    </div>
                    <div class="col-6 col-md-2">
                      <label class="form-label small">Units</label>
                      <input type="number" min="0" step="0.0001" class="form-control form-control-sm" [(ngModel)]="f.units" />
                    </div>
                    <div class="col-6 col-md-2">
                      <label class="form-label small">Price <span class="text-subtle">(auto if blank)</span></label>
                      <input type="number" min="0" step="0.0001" class="form-control form-control-sm" [(ngModel)]="f.price" />
                    </div>
                    <div class="col-12 col-md-3">
                      <label class="form-label small">Note</label>
                      <input class="form-control form-control-sm" [(ngModel)]="f.note" />
                    </div>
                    <div class="col-12 col-md-1 d-grid gap-1">
                      <button class="btn-neon btn-sm" type="button" [disabled]="!isTxValid(f) || saving()" (click)="saveTx()">Save</button>
                      <button class="btn-link-soft btn-sm" type="button" (click)="txForm.set(null)">Cancel</button>
                    </div>
                  </div>
                  @if (txError()) { <div class="alert alert-danger py-1 px-2 small mb-0 mt-2">{{ txError() }}</div> }
                </div>
              }

              <div class="table-wrap">
                <table class="hist-table">
                  <thead>
                    <tr><th>Date</th><th>Type</th><th class="text-end">Units</th><th class="text-end">Price</th><th class="text-end">Total</th><th>Note</th><th></th></tr>
                  </thead>
                  <tbody>
                    @for (t of m.transactions; track t.id) {
                      <tr>
                        <td>{{ t.date }}</td>
                        <td>
                          <span class="status-pill" [ngClass]="t.type === 1 ? 'tone-green' : 'tone-red'">{{ t.type === 1 ? 'Buy' : 'Sell' }}</span>
                        </td>
                        <td class="text-end">{{ fmtUnits(t.units) }}</td>
                        <td class="text-end">{{ fmtMoney(t.price, 4) }}</td>
                        <td class="text-end">{{ fmtMoney(t.total) }}</td>
                        <td class="text-muted-soft">{{ t.note ?? '—' }}</td>
                        <td class="actions">
                          <button class="icon-btn" type="button" title="Edit" (click)="startEditTx(t)">✎</button>
                          <button class="icon-btn icon-danger" type="button" title="Delete" (click)="deleteTx(t)">🗑</button>
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .kpi-label { font-size: 0.7rem; color: var(--fg-muted); text-transform: uppercase; letter-spacing: 0.05em; }
    .kpi-value { font-size: 1.15rem; font-weight: 600; }
    .tone-cyan { color: var(--neon-cyan); }
    .tone-green { color: var(--success); }
    .tone-red { color: var(--danger); }

    .group-card { padding: 1rem; }
    .group-header { display: flex; align-items: flex-start; gap: 0.5rem; padding-bottom: 0.5rem; margin-bottom: 0.5rem; border-bottom: 1px solid var(--border); flex-wrap: wrap; }
    .group-title { font-size: 1.05rem; font-weight: 600; margin: 0; }
    .group-desc { font-size: 0.85rem; color: var(--fg-muted); margin: 0.15rem 0 0; }
    .kv-label { font-size: 0.65rem; color: var(--fg-muted); text-transform: uppercase; }
    .kv-val { font-weight: 600; font-size: 0.92rem; }

    .chip { display: inline-flex; padding: 0.1rem 0.55rem; border-radius: 999px; font-size: 0.72rem; border: 1px solid; font-weight: 500; }
    .ms-1 { margin-left: 0.4rem; }

    .form-row { padding: 0.5rem; background: var(--surface-2); border-radius: var(--radius-sm); margin-bottom: 0.5rem; }

    .table-wrap { overflow: auto; border: 1px solid var(--border); border-radius: var(--radius-sm); }
    .inv-table, .hist-table { width: 100%; border-collapse: collapse; font-size: 0.86rem; }
    .inv-table thead th, .hist-table thead th { background: var(--surface-2); color: var(--fg-muted); font-weight: 600; text-align: left; padding: 0.45rem 0.7rem; border-bottom: 1px solid var(--border-strong); white-space: nowrap; }
    .inv-table tbody td, .hist-table tbody td { padding: 0.5rem 0.7rem; border-bottom: 1px solid var(--border); }
    .inv-table tbody tr:hover td, .hist-table tbody tr:hover td { background: var(--surface-2); }

    .link-name { background: transparent; border: none; padding: 0; color: var(--neon); cursor: pointer; font-weight: 500; }
    .link-name:hover { text-decoration: underline; }

    .status-pill { display: inline-flex; padding: 0.1rem 0.55rem; border-radius: 999px; font-size: 0.72rem; border: 1px solid var(--border-strong); color: var(--fg-muted); }
    .status-pill.is-active { color: var(--success); border-color: var(--success); }
    .status-pill.is-inactive { color: var(--fg-subtle); }

    .icon-btn { width: 28px; height: 28px; border-radius: var(--radius-sm); background: transparent; border: 1px solid transparent; color: var(--fg-muted); cursor: pointer; transition: all 120ms ease; font-size: 0.9rem; }
    .icon-btn:hover { color: var(--fg); border-color: var(--border-strong); }
    .icon-danger:hover { color: var(--danger); border-color: var(--danger); }
    .actions { white-space: nowrap; }

    .btn-link-soft { background: transparent; border: 1px solid var(--border-strong); color: var(--fg-muted); padding: 0.3rem 0.7rem; border-radius: var(--radius-sm); cursor: pointer; transition: all 120ms ease; font-size: 0.8rem; }
    .btn-link-soft:hover { color: var(--neon); border-color: var(--neon); }

    .modal-backdrop { position: fixed; inset: 0; background: rgba(0,0,0,0.55); z-index: 100; }
    .modal-shell {
      position: fixed; top: 5%; left: 50%; transform: translateX(-50%);
      width: 95%; max-width: 1100px;
      max-height: 90vh; overflow: auto;
      background: var(--surface-elevated);
      border: 1px solid var(--neon);
      border-radius: var(--radius-md);
      box-shadow: 0 0 40px var(--neon-soft);
      z-index: 101; padding: 1rem;
    }
    .modal-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 0.75rem; gap: 0.5rem; flex-wrap: wrap; }
    .modal-title { font-size: 1.2rem; font-weight: 600; margin: 0; }
    .section-title { font-size: 0.78rem; color: var(--fg-muted); text-transform: uppercase; letter-spacing: 0.05em; margin: 0.5rem 0; }

    .sub-tabs .nav-link { color: var(--fg-muted); background: transparent; border: 1px solid var(--border-strong); border-radius: var(--radius-sm); padding: 0.3rem 0.7rem; font-size: 0.85rem; }
    .sub-tabs .nav-link.active { color: var(--neon); border-color: var(--neon); box-shadow: 0 0 8px var(--neon-soft); }
    .sub-tabs { gap: 0.4rem; }
  `]
})
export class InvestmentsComponent {
  private readonly api = inject(AssetTrackerApi);
  fmtMoney = fmtMoney;
  fmtUnits = fmtUnits;

  readonly groups = signal<InvestmentGroup[]>([]);
  readonly investments = signal<Investment[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly statusFilter = signal<InvestmentStatus | 0>(1);

  readonly groupForm = signal<GroupForm | null>(null);
  readonly invForm = signal<InvForm | null>(null);

  readonly modal = signal<InvestmentDetail | null>(null);
  readonly modalTab = signal<ModalTab>('analytics');
  readonly editingInv = signal(false);

  readonly priceForm = signal<PriceForm | null>(null);
  readonly txForm = signal<TxForm | null>(null);
  readonly txError = signal<string | null>(null);

  modalForm: { name: string; description: string; tagId: string | null; unit: string } = { name: '', description: '', tagId: null, unit: 'unit' };

  readonly totals = computed(() => {
    const items = this.investments();
    const invested = items.reduce((acc, i) => acc + i.invested, 0);
    const current = items.reduce((acc, i) => acc + i.currentHoldingValue, 0);
    return { invested, current, pl: current - invested };
  });

  readonly priceSeries = computed(() => {
    const m = this.modal();
    if (!m) return [];
    return [...m.prices]
      .sort((a, b) => a.asOf.localeCompare(b.asOf))
      .map(p => ({ date: p.asOf, value: p.price }));
  });

  /** Holding quantity over time, sampled at every transaction date. */
  readonly holdingQuantitySeries = computed(() => {
    const m = this.modal();
    if (!m) return [];
    const txs = [...m.transactions].sort((a, b) => a.date.localeCompare(b.date));
    let units = 0;
    return txs.map(t => {
      units += t.type === 1 ? t.units : -t.units;
      return { date: t.date, value: units };
    });
  });

  /** Holding value over time, sampled at every transaction date using nearest <= price entry. */
  readonly holdingValueSeries = computed(() => {
    const m = this.modal();
    if (!m) return [];
    const prices = [...m.prices].sort((a, b) => a.asOf.localeCompare(b.asOf));
    const txs = [...m.transactions].sort((a, b) => a.date.localeCompare(b.date));
    let units = 0;
    return txs.map(t => {
      units += t.type === 1 ? t.units : -t.units;
      const priceAt = prices.filter(p => p.asOf <= t.date).at(-1)?.price
        ?? prices.at(0)?.price
        ?? 0;
      return { date: t.date, value: units * priceAt };
    });
  });

  constructor() { this.refresh(); }

  async refresh(): Promise<void> {
    this.loading.set(true);
    try {
      const status = this.statusFilter();
      const filterStatus = status === 0 ? undefined : status;
      const [g, i] = await Promise.all([
        firstValueFrom(this.api.listInvestmentGroups(filterStatus)),
        firstValueFrom(this.api.listInvestments(status === 0 ? {} : { status }))
      ]);
      this.groups.set(g);
      this.investments.set(i);
    } finally {
      this.loading.set(false);
    }
  }

  onStatusChange(value: InvestmentStatus | 0): void {
    this.statusFilter.set(value);
    this.refresh();
  }

  investmentsForGroup(groupId: string): Investment[] {
    return this.investments().filter(i => i.groupId === groupId);
  }

  // ===== Groups =====
  openCreateGroup(): void {
    this.groupForm.set({ name: '', description: '', tagId: null, status: 1 });
  }
  startEditGroup(g: InvestmentGroup): void {
    this.groupForm.set({ id: g.id, name: g.name, description: g.description ?? '', tagId: g.tag?.id ?? null, status: g.status });
  }
  async saveGroup(): Promise<void> {
    const f = this.groupForm();
    if (!f || !f.name.trim()) return;
    this.saving.set(true);
    try {
      const body = { name: f.name.trim(), description: f.description.trim() || null, tagId: f.tagId, status: f.status };
      if (f.id) await firstValueFrom(this.api.updateInvestmentGroup(f.id, body));
      else await firstValueFrom(this.api.createInvestmentGroup(body));
      this.groupForm.set(null);
      await this.refresh();
    } finally { this.saving.set(false); }
  }
  async deleteGroup(g: InvestmentGroup): Promise<void> {
    if (!confirm(`Delete group "${g.name}" and all its investments?`)) return;
    try { await firstValueFrom(this.api.deleteInvestmentGroup(g.id)); await this.refresh(); }
    catch (err: any) { alert(err?.error?.message ?? 'Delete failed.'); }
  }

  // ===== Investments =====
  openCreateInvestment(g: InvestmentGroup): void {
    this.invForm.set({
      groupId: g.id, name: '', description: '', tagId: null,
      unit: 'unit', currentPrice: null
    });
  }
  isInvValid(f: InvForm): boolean {
    if (!f.name.trim() || !f.unit.trim()) return false;
    if (!f.id && (!f.currentPrice || f.currentPrice <= 0)) return false;
    return true;
  }
  async saveInvestment(): Promise<void> {
    const f = this.invForm();
    if (!f || !this.isInvValid(f)) return;
    this.saving.set(true);
    try {
      const base = {
        groupId: f.groupId, name: f.name.trim(), description: f.description.trim() || null,
        tagId: f.tagId, unit: f.unit.trim()
      };
      if (f.id) await firstValueFrom(this.api.updateInvestment(f.id, base));
      else await firstValueFrom(this.api.createInvestment({ ...base, currentPrice: f.currentPrice }));
      this.invForm.set(null);
      await this.refresh();
    } finally { this.saving.set(false); }
  }
  async deleteInvestment(i: Investment): Promise<void> {
    if (!confirm(`Delete investment "${i.name}" and all its history?`)) return;
    try { await firstValueFrom(this.api.deleteInvestment(i.id)); await this.refresh(); }
    catch (err: any) { alert(err?.error?.message ?? 'Delete failed.'); }
  }

  // ===== Modal =====
  async openInvestment(i: Investment): Promise<void> {
    const detail = await firstValueFrom(this.api.getInvestment(i.id));
    this.modal.set(detail);
    this.modalTab.set('analytics');
    this.editingInv.set(false);
    this.modalForm = {
      name: detail.investment.name,
      description: detail.investment.description ?? '',
      tagId: detail.investment.tag?.id ?? null,
      unit: detail.investment.unit
    };
  }
  closeModal(): void {
    this.modal.set(null);
    this.priceForm.set(null);
    this.txForm.set(null);
    this.refresh();
  }
  toggleEditInv(): void { this.editingInv.update(v => !v); }
  async saveEditInv(): Promise<void> {
    const m = this.modal();
    if (!m || !this.modalForm.name.trim() || !this.modalForm.unit.trim()) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.updateInvestment(m.investment.id, {
        groupId: m.investment.groupId,
        name: this.modalForm.name.trim(),
        description: this.modalForm.description.trim() || null,
        tagId: this.modalForm.tagId,
        unit: this.modalForm.unit.trim()
      }));
      this.editingInv.set(false);
      await this.refreshModal();
    } finally { this.saving.set(false); }
  }
  private async refreshModal(): Promise<void> {
    const m = this.modal();
    if (!m) return;
    const detail = await firstValueFrom(this.api.getInvestment(m.investment.id));
    this.modal.set(detail);
  }

  // ===== Price entries =====
  openAddPrice(): void { this.priceForm.set({ asOf: isoToday(), price: 0, note: '' }); }
  startEditPrice(p: InvestmentPriceEntry): void { this.priceForm.set({ id: p.id, asOf: p.asOf, price: p.price, note: p.note ?? '' }); }
  async savePrice(): Promise<void> {
    const f = this.priceForm();
    const m = this.modal();
    if (!f || !m || f.price <= 0) return;
    this.saving.set(true);
    try {
      const body = { asOf: f.asOf, price: Number(f.price), note: f.note.trim() || null };
      if (f.id) await firstValueFrom(this.api.updateInvestmentPrice(m.investment.id, f.id, body));
      else await firstValueFrom(this.api.addInvestmentPrice(m.investment.id, body));
      this.priceForm.set(null);
      await this.refreshModal();
    } finally { this.saving.set(false); }
  }
  async deletePrice(p: InvestmentPriceEntry): Promise<void> {
    const m = this.modal();
    if (!m) return;
    if (!confirm(`Delete price entry for ${p.asOf}?`)) return;
    try { await firstValueFrom(this.api.deleteInvestmentPrice(m.investment.id, p.id)); await this.refreshModal(); }
    catch (err: any) { alert(err?.error?.message ?? 'Delete failed.'); }
  }

  // ===== Transactions =====
  openAddTx(): void {
    this.txError.set(null);
    this.txForm.set({ date: isoToday(), type: 1, units: 0, price: null, note: '' });
  }
  startEditTx(t: InvestmentTx): void {
    this.txError.set(null);
    this.txForm.set({ id: t.id, date: t.date, type: t.type, units: t.units, price: t.price, note: t.note ?? '' });
  }
  isTxValid(f: TxForm): boolean { return f.units > 0 && !!f.date; }
  async saveTx(): Promise<void> {
    const f = this.txForm();
    const m = this.modal();
    if (!f || !m || !this.isTxValid(f)) return;
    this.saving.set(true);
    this.txError.set(null);
    try {
      const body = {
        date: f.date,
        type: f.type,
        units: Number(f.units),
        price: f.price === null || f.price === undefined || isNaN(f.price as any) ? null : Number(f.price),
        note: f.note.trim() || null
      };
      if (f.id) await firstValueFrom(this.api.updateInvestmentTx(m.investment.id, f.id, body));
      else await firstValueFrom(this.api.addInvestmentTx(m.investment.id, body));
      this.txForm.set(null);
      await this.refreshModal();
    } catch (err: any) {
      this.txError.set(err?.error?.message ?? 'Save failed.');
    } finally { this.saving.set(false); }
  }
  async deleteTx(t: InvestmentTx): Promise<void> {
    const m = this.modal();
    if (!m) return;
    if (!confirm(`Delete transaction on ${t.date}?`)) return;
    try { await firstValueFrom(this.api.deleteInvestmentTx(m.investment.id, t.id)); await this.refreshModal(); }
    catch (err: any) { alert(err?.error?.message ?? 'Delete failed.'); }
  }

  // ===== Reports =====
  async downloadReport(format: ReportFormat): Promise<void> {
    try {
      const status = this.statusFilter();
      const res = await firstValueFrom(this.api.downloadInvestmentsReport(status === 0 ? null : status, format));
      const blob = res.body!;
      const fname = parseFilename(res.headers.get('content-disposition'))
        ?? `investments-${status === 0 ? 'all' : status === 1 ? 'active' : 'inactive'}.${format.toLowerCase()}`;
      triggerDownload(blob, fname);
    } catch { alert('Download failed.'); }
  }
}

function parseFilename(disposition: string | null): string | null {
  if (!disposition) return null;
  const utf8 = /filename\*=UTF-8''([^;\n]+)/i.exec(disposition);
  if (utf8) return decodeURIComponent(utf8[1]);
  const m = /filename="?([^";\n]+)"?/i.exec(disposition);
  return m ? m[1] : null;
}
function triggerDownload(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url; a.download = filename;
  document.body.appendChild(a); a.click(); document.body.removeChild(a);
  URL.revokeObjectURL(url);
}
