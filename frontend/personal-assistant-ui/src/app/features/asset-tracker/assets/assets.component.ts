import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AssetTrackerApi } from '../asset-tracker.api';
import {
  Asset,
  AssetGroup,
  AssetPriceEntry,
  AssetStatus,
  ReportFormat,
  fmtMoney,
  isoToday
} from '../asset-tracker.models';
import { TagPickerComponent } from '../shared/tag-picker.component';
import { LineChartComponent } from '../shared/line-chart.component';

interface GroupForm {
  id?: string;
  name: string;
  description: string;
  tagId: string | null;
}

interface AssetForm {
  id?: string;
  groupId: string;
  name: string;
  description: string;
  tagId: string | null;
  buyingDate: string;
  buyingPrice: number | null;
  sellingDate: string;
  sellingPrice: number | null;
  status: AssetStatus;
  currentPrice: number | null; // only used on create
}

interface PriceForm {
  id?: string;
  asOf: string;
  price: number;
  note: string;
}

@Component({
  selector: 'app-asset-tracker-assets',
  imports: [CommonModule, FormsModule, TagPickerComponent, LineChartComponent],
  template: `
    <div class="assets-shell">
      <!-- Top bar -->
      <div class="d-flex flex-wrap align-items-center gap-2 mb-3">
        <div class="d-flex align-items-center gap-2">
          <label class="form-label small text-muted-soft mb-0">Filter:</label>
          <select class="form-select form-select-sm w-auto" [ngModel]="statusFilter()" (ngModelChange)="onStatusChange($event)">
            <option [ngValue]="1">In possession</option>
            <option [ngValue]="2">Sold</option>
            <option [ngValue]="0">All</option>
          </select>
        </div>
        <div class="ms-auto d-flex gap-2 flex-wrap">
          <button class="btn-link-soft btn-sm" type="button" (click)="downloadReport('Csv')">CSV</button>
          <button class="btn-link-soft btn-sm" type="button" (click)="downloadReport('Xlsx')">Excel</button>
          <button class="btn-link-soft btn-sm" type="button" (click)="downloadReport('Pdf')">PDF</button>
          <button class="btn-neon btn-sm" type="button" (click)="openCreateGroup()">+ New Asset Group</button>
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
            <div class="col-12 col-md-5">
              <label class="form-label small">Description</label>
              <input class="form-control form-control-sm" [(ngModel)]="f.description" />
            </div>
            <div class="col-6 col-md-2">
              <label class="form-label small">Tag</label>
              <app-tag-picker [value]="f.tagId" (valueChange)="f.tagId = $event" />
            </div>
            <div class="col-6 col-md-1 d-grid">
              <button class="btn-neon btn-sm" type="button" [disabled]="!f.name.trim() || saving()" (click)="saveGroup()">Save</button>
            </div>
          </div>
          @if (formError()) { <div class="alert alert-danger py-1 px-2 small mb-0 mt-2">{{ formError() }}</div> }
        </div>
      }

      <!-- Groups -->
      @if (loading()) { <div class="text-muted-soft small">Loading…</div> }
      @else if (groups().length === 0) {
        <div class="surface p-4 text-center text-muted-soft">No asset groups yet.</div>
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
                </div>
                @if (g.description) { <p class="group-desc">{{ g.description }}</p> }
              </div>
              <div class="ms-auto d-flex align-items-center gap-3">
                <div class="text-end">
                  <div class="kv-label">Total</div>
                  <div class="kv-val">{{ fmtMoney(g.totalCurrentValue) }}</div>
                </div>
                <div class="d-flex gap-1">
                  <button class="icon-btn" type="button" title="Edit" (click)="startEditGroup(g)">✎</button>
                  <button class="icon-btn icon-danger" type="button" title="Delete" (click)="deleteGroup(g)">🗑</button>
                </div>
                <button class="btn-link-soft btn-sm" type="button" (click)="openCreateAsset(g)">+ Asset</button>
              </div>
            </header>

            <!-- Asset form -->
            @if (assetForm()?.groupId === g.id) {
              <div class="asset-form">
                @if (assetForm(); as f) {
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
                      <label class="form-label small">Status</label>
                      <select class="form-select form-select-sm" [(ngModel)]="f.status">
                        <option [ngValue]="1">In possession</option>
                        <option [ngValue]="2">Sold</option>
                      </select>
                    </div>
                    <div class="col-6 col-md-2">
                      <label class="form-label small">Buying Date</label>
                      <input type="date" class="form-control form-control-sm" [(ngModel)]="f.buyingDate" />
                    </div>
                    <div class="col-6 col-md-2">
                      <label class="form-label small">Buying Price</label>
                      <input type="number" min="0" step="0.01" class="form-control form-control-sm" [(ngModel)]="f.buyingPrice" />
                    </div>
                    <div class="col-6 col-md-2">
                      <label class="form-label small">Selling Date</label>
                      <input type="date" class="form-control form-control-sm" [(ngModel)]="f.sellingDate" />
                    </div>
                    <div class="col-6 col-md-2">
                      <label class="form-label small">Selling Price</label>
                      <input type="number" min="0" step="0.01" class="form-control form-control-sm" [(ngModel)]="f.sellingPrice" />
                    </div>
                    @if (!f.id) {
                      <div class="col-6 col-md-2">
                        <label class="form-label small">Current Price *</label>
                        <input type="number" min="0" step="0.01" class="form-control form-control-sm" [(ngModel)]="f.currentPrice" />
                      </div>
                    }
                    <div class="col-12 d-flex gap-2 justify-content-end">
                      <button class="btn-link-soft btn-sm" type="button" (click)="assetForm.set(null)">Cancel</button>
                      <button class="btn-neon btn-sm" type="button" [disabled]="!isAssetValid(f) || saving()" (click)="saveAsset()">{{ f.id ? 'Save Changes' : 'Create' }}</button>
                    </div>
                  </div>
                  @if (formError()) { <div class="alert alert-danger py-1 px-2 small mb-0 mt-2">{{ formError() }}</div> }
                }
              </div>
            }

            <!-- Assets table -->
            @if (assetsForGroup(g.id).length === 0) {
              <div class="text-muted-soft small px-2 py-2">No assets in this group for the current filter.</div>
            } @else {
              <div class="table-wrap">
                <table class="asset-table">
                  <thead>
                    <tr>
                      <th>Name</th><th>Description</th><th>Tag</th>
                      <th>Buy Date</th><th class="text-end">Buy Price</th>
                      <th class="text-end">Current Price</th>
                      <th>Sell Date</th><th class="text-end">Sell Price</th>
                      <th>Status</th><th></th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (a of assetsForGroup(g.id); track a.id) {
                      <tr>
                        <td>
                          <button class="link-name" type="button" (click)="openAsset(a)">{{ a.name }}</button>
                        </td>
                        <td class="text-muted-soft">{{ a.description ?? '—' }}</td>
                        <td>
                          @if (a.tag; as t) {
                            <span class="chip" [style.color]="t.color" [style.borderColor]="t.color">{{ t.name }}</span>
                          } @else { <span class="text-subtle">—</span> }
                        </td>
                        <td>{{ a.buyingDate ?? '—' }}</td>
                        <td class="text-end">{{ a.buyingPrice === null ? '—' : fmtMoney(a.buyingPrice) }}</td>
                        <td class="text-end">{{ fmtMoney(a.currentPrice) }}</td>
                        <td>{{ a.sellingDate ?? '—' }}</td>
                        <td class="text-end">{{ a.sellingPrice === null ? '—' : fmtMoney(a.sellingPrice) }}</td>
                        <td>
                          <span class="status-pill" [ngClass]="a.status === 1 ? 'is-active' : 'is-sold'">
                            {{ a.status === 1 ? 'In possession' : 'Sold' }}
                          </span>
                        </td>
                        <td class="actions">
                          <button class="icon-btn icon-danger" type="button" title="Delete" (click)="deleteAsset(a)">🗑</button>
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

      <!-- Asset detail modal -->
      @if (modalAsset(); as m) {
        <div class="modal-backdrop" (click)="closeModal()"></div>
        <div class="modal-shell" role="dialog" aria-modal="true">
          <div class="modal-header">
            <div>
              <h2 class="modal-title">{{ m.asset.name }}</h2>
              <div class="small text-muted-soft">{{ m.asset.groupName }}</div>
            </div>
            <button class="icon-btn" type="button" (click)="closeModal()">×</button>
          </div>

          <div class="modal-body">
            <!-- Edit form (NO current price; price changes via history below) -->
            <h3 class="section-title">Details</h3>
            <div class="row g-2 align-items-end mb-3">
              <div class="col-12 col-md-3">
                <label class="form-label small">Name</label>
                <input class="form-control form-control-sm" [(ngModel)]="modalForm.name" />
              </div>
              <div class="col-12 col-md-3">
                <label class="form-label small">Description</label>
                <input class="form-control form-control-sm" [(ngModel)]="modalForm.description" />
              </div>
              <div class="col-6 col-md-2">
                <label class="form-label small">Tag</label>
                <app-tag-picker [value]="modalForm.tagId" (valueChange)="modalForm.tagId = $event" />
              </div>
              <div class="col-6 col-md-2">
                <label class="form-label small">Status</label>
                <select class="form-select form-select-sm" [(ngModel)]="modalForm.status">
                  <option [ngValue]="1">In possession</option>
                  <option [ngValue]="2">Sold</option>
                </select>
              </div>
              <div class="col-6 col-md-2">
                <label class="form-label small">Buy Date</label>
                <input type="date" class="form-control form-control-sm" [(ngModel)]="modalForm.buyingDate" />
              </div>
              <div class="col-6 col-md-2">
                <label class="form-label small">Buy Price</label>
                <input type="number" min="0" step="0.01" class="form-control form-control-sm" [(ngModel)]="modalForm.buyingPrice" />
              </div>
              <div class="col-6 col-md-2">
                <label class="form-label small">Sell Date</label>
                <input type="date" class="form-control form-control-sm" [(ngModel)]="modalForm.sellingDate" />
              </div>
              <div class="col-6 col-md-2">
                <label class="form-label small">Sell Price</label>
                <input type="number" min="0" step="0.01" class="form-control form-control-sm" [(ngModel)]="modalForm.sellingPrice" />
              </div>
              <div class="col-12 d-flex gap-2 justify-content-end">
                <button class="btn-neon btn-sm" type="button" [disabled]="!modalForm.name.trim() || saving()" (click)="saveModalAsset()">Save Details</button>
              </div>
            </div>

            <!-- Price history chart -->
            <h3 class="section-title">Price history</h3>
            <div class="mb-3"><app-line-chart [series]="priceSeries()" color="var(--neon-cyan)" /></div>

            <!-- Add price entry -->
            <div class="d-flex justify-content-between mb-2 align-items-center">
              <h3 class="section-title m-0">Entries</h3>
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
                    <input type="number" min="0" step="0.01" class="form-control form-control-sm" [(ngModel)]="f.price" />
                  </div>
                  <div class="col-12 col-md-4">
                    <label class="form-label small">Note</label>
                    <input class="form-control form-control-sm" [(ngModel)]="f.note" />
                  </div>
                  <div class="col-12 col-md-2 d-flex gap-1 d-grid">
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
                  @for (p of m.history; track p.id) {
                    <tr>
                      <td>{{ p.asOf }}</td>
                      <td class="text-end">{{ fmtMoney(p.price) }}</td>
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
        </div>
      }
    </div>
  `,
  styles: [`
    .group-card { padding: 1rem; }
    .group-header {
      display: flex; align-items: flex-start; gap: 0.5rem;
      padding-bottom: 0.5rem; margin-bottom: 0.5rem;
      border-bottom: 1px solid var(--border);
      flex-wrap: wrap;
    }
    .group-title { font-size: 1.05rem; font-weight: 600; margin: 0; }
    .group-desc { font-size: 0.85rem; color: var(--fg-muted); margin: 0.15rem 0 0; }
    .kv-label { font-size: 0.7rem; color: var(--fg-muted); text-transform: uppercase; }
    .kv-val { font-weight: 600; font-size: 0.95rem; }

    .chip { display: inline-flex; padding: 0.1rem 0.55rem; border-radius: 999px; font-size: 0.72rem; border: 1px solid; font-weight: 500; }

    .table-wrap { overflow: auto; border: 1px solid var(--border); border-radius: var(--radius-sm); }
    .asset-table, .hist-table { width: 100%; border-collapse: collapse; font-size: 0.86rem; }
    .asset-table thead th, .hist-table thead th { background: var(--surface-2); color: var(--fg-muted); font-weight: 600; text-align: left; padding: 0.45rem 0.7rem; border-bottom: 1px solid var(--border-strong); white-space: nowrap; }
    .asset-table tbody td, .hist-table tbody td { padding: 0.45rem 0.7rem; border-bottom: 1px solid var(--border); }
    .asset-table tbody tr:hover td, .hist-table tbody tr:hover td { background: var(--surface-2); }

    .link-name { background: transparent; border: none; padding: 0; color: var(--neon); cursor: pointer; font-weight: 500; }
    .link-name:hover { text-decoration: underline; }

    .status-pill { display: inline-flex; padding: 0.1rem 0.55rem; border-radius: 999px; font-size: 0.72rem; border: 1px solid var(--border-strong); color: var(--fg-muted); }
    .status-pill.is-active { color: var(--success); border-color: var(--success); }
    .status-pill.is-sold { color: var(--fg-subtle); }

    .icon-btn { width: 28px; height: 28px; border-radius: var(--radius-sm); background: transparent; border: 1px solid transparent; color: var(--fg-muted); cursor: pointer; transition: all 120ms ease; font-size: 0.9rem; }
    .icon-btn:hover { color: var(--fg); border-color: var(--border-strong); }
    .icon-danger:hover { color: var(--danger); border-color: var(--danger); }
    .actions { white-space: nowrap; }

    .btn-link-soft { background: transparent; border: 1px solid var(--border-strong); color: var(--fg-muted); padding: 0.3rem 0.7rem; border-radius: var(--radius-sm); cursor: pointer; transition: all 120ms ease; font-size: 0.8rem; }
    .btn-link-soft:hover { color: var(--neon); border-color: var(--neon); }

    .asset-form { padding: 0.5rem; background: var(--surface-2); border-radius: var(--radius-sm); margin-bottom: 0.5rem; }

    .modal-backdrop {
      position: fixed; inset: 0; background: rgba(0,0,0,0.55);
      z-index: 100;
    }
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
    .modal-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 0.75rem; }
    .modal-title { font-size: 1.2rem; font-weight: 600; margin: 0; }
    .section-title { font-size: 0.78rem; color: var(--fg-muted); text-transform: uppercase; letter-spacing: 0.05em; margin: 0.5rem 0; }
  `]
})
export class AssetsComponent {
  private readonly api = inject(AssetTrackerApi);
  fmtMoney = fmtMoney;

  readonly groups = signal<AssetGroup[]>([]);
  readonly assets = signal<Asset[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly statusFilter = signal<AssetStatus | 0>(1);

  readonly groupForm = signal<GroupForm | null>(null);
  readonly assetForm = signal<AssetForm | null>(null);
  readonly formError = signal<string | null>(null);

  readonly modalAsset = signal<{ asset: Asset; groupName: string; history: AssetPriceEntry[] } | null>(null);
  readonly priceForm = signal<PriceForm | null>(null);

  modalForm: AssetForm = blankAsset();

  readonly priceSeries = computed(() => {
    const m = this.modalAsset();
    if (!m) return [];
    return [...m.history]
      .sort((a, b) => a.asOf.localeCompare(b.asOf))
      .map(p => ({ date: p.asOf, value: p.price }));
  });

  constructor() { this.refresh(); }

  async refresh(): Promise<void> {
    this.loading.set(true);
    try {
      const status = this.statusFilter();
      const [g, a] = await Promise.all([
        firstValueFrom(this.api.listAssetGroups()),
        firstValueFrom(this.api.listAssets(status === 0 ? {} : { status }))
      ]);
      this.groups.set(g);
      this.assets.set(a);
    } finally {
      this.loading.set(false);
    }
  }

  onStatusChange(value: AssetStatus | 0): void {
    this.statusFilter.set(value);
    this.refresh();
  }

  assetsForGroup(groupId: string): Asset[] {
    return this.assets().filter(a => a.groupId === groupId);
  }

  // ===== Groups =====
  openCreateGroup(): void {
    this.formError.set(null);
    this.groupForm.set({ name: '', description: '', tagId: null });
  }
  startEditGroup(g: AssetGroup): void {
    this.groupForm.set({ id: g.id, name: g.name, description: g.description ?? '', tagId: g.tag?.id ?? null });
  }
  async saveGroup(): Promise<void> {
    const f = this.groupForm();
    if (!f || !f.name.trim()) return;
    this.saving.set(true);
    this.formError.set(null);
    try {
      const body = { name: f.name.trim(), description: f.description.trim() || null, tagId: f.tagId };
      if (f.id) await firstValueFrom(this.api.updateAssetGroup(f.id, body));
      else await firstValueFrom(this.api.createAssetGroup(body));
      this.groupForm.set(null);
      await this.refresh();
    } catch (err: any) {
      this.formError.set(err?.error?.message ?? 'Save failed.');
    } finally { this.saving.set(false); }
  }
  async deleteGroup(g: AssetGroup): Promise<void> {
    if (!confirm(`Delete group "${g.name}" and all its assets?`)) return;
    try { await firstValueFrom(this.api.deleteAssetGroup(g.id)); await this.refresh(); }
    catch (err: any) { alert(err?.error?.message ?? 'Delete failed.'); }
  }

  // ===== Assets =====
  openCreateAsset(g: AssetGroup): void {
    this.formError.set(null);
    const f = blankAsset();
    f.groupId = g.id;
    this.assetForm.set(f);
  }
  isAssetValid(f: AssetForm): boolean {
    if (!f.name.trim()) return false;
    if (!f.id && (!f.currentPrice || f.currentPrice <= 0)) return false;
    return true;
  }
  async saveAsset(): Promise<void> {
    const f = this.assetForm();
    if (!f || !this.isAssetValid(f)) return;
    this.saving.set(true);
    this.formError.set(null);
    try {
      const base = {
        groupId: f.groupId,
        name: f.name.trim(),
        description: f.description.trim() || null,
        tagId: f.tagId,
        buyingDate: f.buyingDate || null,
        buyingPrice: f.buyingPrice ?? null,
        sellingDate: f.sellingDate || null,
        sellingPrice: f.sellingPrice ?? null,
        status: f.status
      };
      if (f.id) await firstValueFrom(this.api.updateAsset(f.id, base));
      else await firstValueFrom(this.api.createAsset({ ...base, currentPrice: f.currentPrice }));
      this.assetForm.set(null);
      await this.refresh();
    } catch (err: any) {
      this.formError.set(err?.error?.message ?? 'Save failed.');
    } finally { this.saving.set(false); }
  }
  async deleteAsset(a: Asset): Promise<void> {
    if (!confirm(`Delete asset "${a.name}"?`)) return;
    try { await firstValueFrom(this.api.deleteAsset(a.id)); await this.refresh(); }
    catch (err: any) { alert(err?.error?.message ?? 'Delete failed.'); }
  }

  // ===== Modal =====
  async openAsset(a: Asset): Promise<void> {
    const detail = await firstValueFrom(this.api.getAsset(a.id));
    this.modalAsset.set({
      asset: detail.asset,
      groupName: detail.asset.groupName,
      history: detail.history
    });
    this.modalForm = {
      id: detail.asset.id,
      groupId: detail.asset.groupId,
      name: detail.asset.name,
      description: detail.asset.description ?? '',
      tagId: detail.asset.tag?.id ?? null,
      buyingDate: detail.asset.buyingDate ?? '',
      buyingPrice: detail.asset.buyingPrice,
      sellingDate: detail.asset.sellingDate ?? '',
      sellingPrice: detail.asset.sellingPrice,
      status: detail.asset.status,
      currentPrice: null
    };
  }
  closeModal(): void {
    this.modalAsset.set(null);
    this.priceForm.set(null);
    this.refresh();
  }
  async saveModalAsset(): Promise<void> {
    if (!this.modalForm.name.trim() || !this.modalForm.id) return;
    this.saving.set(true);
    try {
      const body = {
        groupId: this.modalForm.groupId,
        name: this.modalForm.name.trim(),
        description: this.modalForm.description.trim() || null,
        tagId: this.modalForm.tagId,
        buyingDate: this.modalForm.buyingDate || null,
        buyingPrice: this.modalForm.buyingPrice ?? null,
        sellingDate: this.modalForm.sellingDate || null,
        sellingPrice: this.modalForm.sellingPrice ?? null,
        status: this.modalForm.status
      };
      await firstValueFrom(this.api.updateAsset(this.modalForm.id, body));
      await this.refreshModal();
    } finally { this.saving.set(false); }
  }

  // ===== Price entries =====
  openAddPrice(): void {
    this.priceForm.set({ asOf: isoToday(), price: 0, note: '' });
  }
  startEditPrice(p: AssetPriceEntry): void {
    this.priceForm.set({ id: p.id, asOf: p.asOf, price: p.price, note: p.note ?? '' });
  }
  async savePrice(): Promise<void> {
    const f = this.priceForm();
    const m = this.modalAsset();
    if (!f || !m || f.price <= 0) return;
    this.saving.set(true);
    try {
      const body = { asOf: f.asOf, price: Number(f.price), note: f.note.trim() || null };
      if (f.id) await firstValueFrom(this.api.updateAssetPrice(m.asset.id, f.id, body));
      else await firstValueFrom(this.api.addAssetPrice(m.asset.id, body));
      this.priceForm.set(null);
      await this.refreshModal();
    } finally { this.saving.set(false); }
  }
  async deletePrice(p: AssetPriceEntry): Promise<void> {
    const m = this.modalAsset();
    if (!m) return;
    if (!confirm(`Delete price entry for ${p.asOf}?`)) return;
    try {
      await firstValueFrom(this.api.deleteAssetPrice(m.asset.id, p.id));
      await this.refreshModal();
    } catch (err: any) {
      alert(err?.error?.message ?? 'Delete failed.');
    }
  }
  private async refreshModal(): Promise<void> {
    const m = this.modalAsset();
    if (!m) return;
    const detail = await firstValueFrom(this.api.getAsset(m.asset.id));
    this.modalAsset.set({ asset: detail.asset, groupName: detail.asset.groupName, history: detail.history });
  }

  // ===== Reports =====
  async downloadReport(format: ReportFormat): Promise<void> {
    try {
      const status = this.statusFilter();
      const res = await firstValueFrom(this.api.downloadAssetsReport(status === 0 ? null : status, format));
      const blob = res.body!;
      const fname = parseFilename(res.headers.get('content-disposition'))
        ?? `assets-${status === 0 ? 'all' : status === 1 ? 'in-possession' : 'sold'}.${format.toLowerCase()}`;
      triggerDownload(blob, fname);
    } catch { alert('Download failed.'); }
  }
}

function blankAsset(): AssetForm {
  return {
    groupId: '', name: '', description: '', tagId: null,
    buyingDate: '', buyingPrice: null, sellingDate: '', sellingPrice: null,
    status: 1, currentPrice: null
  };
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
