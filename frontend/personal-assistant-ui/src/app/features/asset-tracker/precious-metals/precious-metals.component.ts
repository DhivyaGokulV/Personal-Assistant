import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpResponse } from '@angular/common/http';
import { AssetTrackerApi } from '../asset-tracker.api';
import {
  fmtMoney,
  fmtUnits,
  isoOffset,
  isoToday,
  PagedResult,
  PreciousMetal,
  PreciousMetalEntry,
  PreciousMetalPriceEntry,
  PreciousMetalStatistics,
  PreciousMetalTxType,
  ReportFormat
} from '../asset-tracker.models';
import { LineChartComponent } from '../shared/line-chart.component';

@Component({
  selector: 'app-asset-tracker-precious-metals',
  standalone: true,
  imports: [CommonModule, FormsModule, LineChartComponent],
  template: `
    <section class="surface feature">
      <div class="toolbar">
        <div>
          <span class="eyebrow">Assets</span>
          <h2>Precious Metals</h2>
          <p class="text-muted-soft mb-0">Track solid Gold 24K, Silver, and custom metals in grams.</p>
        </div>
        <div class="actions">
          <button class="btn btn-outline-light btn-sm" (click)="openExport()" [disabled]="metals.length === 0">Export</button>
          <button class="btn btn-primary btn-sm" (click)="openMetal()">Create Precious Metal</button>
        </div>
      </div>

      <div class="filters">
        <input class="form-control" placeholder="Search metals" [(ngModel)]="search" (ngModelChange)="page=1; load()" />
        <select class="form-select" [(ngModel)]="sortBy" (ngModelChange)="load()">
          <option value="Name">Name</option>
          <option value="CreationDate">Creation date</option>
          <option value="Quantity">Quantity</option>
          <option value="CurrentValue">Current value</option>
        </select>
        <select class="form-select" [(ngModel)]="sortDirection" (ngModelChange)="load()">
          <option value="Asc">Ascending</option>
          <option value="Desc">Descending</option>
        </select>
      </div>

      @if (error) { <div class="alert alert-danger">{{ error }}</div> }
      @if (toast) { <div class="alert alert-success">{{ toast }}</div> }

      <div class="cards">
        @for (metal of metals; track metal.id) {
          <article class="asset-card">
            <div class="card-main">
              <div>
                <div class="title-row">
                  <h3>{{ metal.name }}</h3>
                  @if (metal.isDefault) { <span class="badge">Default</span> }
                </div>
                <p>{{ metal.description || 'No description' }}</p>
                <small>Created {{ metal.creationDate }} · INR</small>
              </div>
              <div class="metrics">
                <span>{{ fmtUnits(metal.quantity) }} g</span>
                <strong>{{ fmtMoney(metal.currentValue) }}</strong>
                <small>Latest price {{ metal.currentPrice === null ? '—' : fmtMoney(metal.currentPrice, 4) + '/g' }}</small>
              </div>
            </div>
            <div class="card-actions">
              <button class="btn btn-outline-light btn-sm" (click)="openMetal(metal)">Edit</button>
              <button class="btn btn-outline-danger btn-sm" (click)="confirmDelete('metal', metal)">Delete</button>
              <button class="btn btn-outline-light btn-sm" (click)="toggle(metal)">{{ expandedId === metal.id ? 'Collapse' : 'History' }}</button>
            </div>

            @if (expandedId === metal.id) {
              <div class="history">
                <div class="history-tabs">
                  <button [class.active]="historyTab === 'entries'" (click)="historyTab='entries'; loadEntries(metal)">Buying/Selling</button>
                  <button [class.active]="historyTab === 'prices'" (click)="historyTab='prices'; loadPrices(metal)">Price History</button>
                </div>
                <div class="history-tools">
                  <button class="btn btn-outline-info btn-sm" (click)="openStats(metal, historyTab === 'entries' ? 'Entries' : 'Prices')">View Statistics</button>
                  <button class="btn btn-primary btn-sm" (click)="historyTab === 'entries' ? openEntry(metal) : openPrice(metal)">
                    Add {{ historyTab === 'entries' ? 'Entry' : 'Price' }}
                  </button>
                </div>

                @if (historyTab === 'entries') {
                  <div class="table-responsive">
                    <table class="table table-dark table-sm align-middle">
                      <thead><tr><th>Type</th><th>Date</th><th>Note</th><th>Quantity</th><th>Price/g</th><th>Amount</th><th></th></tr></thead>
                      <tbody>
                        @for (entry of entries.items; track entry.id) {
                          <tr>
                            <td>{{ entry.type === 1 ? 'Buy' : 'Sell' }}</td>
                            <td>{{ entry.date }}</td>
                            <td>{{ entry.note || '—' }}</td>
                            <td>{{ fmtUnits(entry.quantity) }} g</td>
                            <td>{{ fmtMoney(entry.pricePerUnit, 4) }}</td>
                            <td>{{ fmtMoney(entry.amount) }}</td>
                            <td class="row-actions">
                              <button class="btn btn-outline-light btn-sm" (click)="openEntry(metal, entry)">Edit</button>
                              <button class="btn btn-outline-danger btn-sm" (click)="confirmDelete('entry', metal, entry)">Delete</button>
                            </td>
                          </tr>
                        } @empty {
                          <tr><td colspan="7" class="text-center text-muted-soft">No buy/sell history yet.</td></tr>
                        }
                      </tbody>
                    </table>
                  </div>
                  <div class="pager"><button (click)="entryPage=entryPage-1; loadEntries(metal)" [disabled]="entryPage<=1">Prev</button><span>{{ entryPage }} / {{ entries.totalPages || 1 }}</span><button (click)="entryPage=entryPage+1; loadEntries(metal)" [disabled]="entryPage>=entries.totalPages">Next</button></div>
                } @else {
                  <div class="table-responsive">
                    <table class="table table-dark table-sm align-middle">
                      <thead><tr><th>Date</th><th>Price/g</th><th></th></tr></thead>
                      <tbody>
                        @for (price of prices.items; track price.id) {
                          <tr>
                            <td>{{ price.date }}</td>
                            <td>{{ fmtMoney(price.pricePerUnit, 4) }}</td>
                            <td class="row-actions"><button class="btn btn-outline-danger btn-sm" (click)="confirmDelete('price', metal, price)">Delete</button></td>
                          </tr>
                        } @empty {
                          <tr><td colspan="3" class="text-center text-muted-soft">No price history yet.</td></tr>
                        }
                      </tbody>
                    </table>
                  </div>
                  <div class="pager"><button (click)="pricePage=pricePage-1; loadPrices(metal)" [disabled]="pricePage<=1">Prev</button><span>{{ pricePage }} / {{ prices.totalPages || 1 }}</span><button (click)="pricePage=pricePage+1; loadPrices(metal)" [disabled]="pricePage>=prices.totalPages">Next</button></div>
                }
              </div>
            }
          </article>
        } @empty {
          <div class="empty">Gold 24K and Silver will be seeded automatically when this section loads.</div>
        }
      </div>

      <div class="pager main-pager">
        <button (click)="page=page-1; load()" [disabled]="page<=1">Prev</button>
        <span>{{ page }} / {{ totalPages || 1 }}</span>
        <button (click)="page=page+1; load()" [disabled]="page>=totalPages">Next</button>
      </div>
    </section>

    @if (metalModal) {
      <div class="modal-backdrop-soft">
        <form class="dialog" (ngSubmit)="saveMetal()">
          <h3>{{ editingMetal ? 'Edit' : 'Create' }} Precious Metal</h3>
          <label>Name<input class="form-control" [(ngModel)]="metalForm.name" name="metalName" (ngModelChange)="validateMetal()" /></label>
          @if (metalErrors['name']) { <small class="field-error">{{ metalErrors['name'] }}</small> }
          <label>Description<textarea class="form-control" [(ngModel)]="metalForm.description" name="metalDesc" (ngModelChange)="validateMetal()"></textarea></label>
          @if (metalErrors['description']) { <small class="field-error">{{ metalErrors['description'] }}</small> }
          @if (editingMetal) {
            <label>Creation date<input class="form-control" type="date" [(ngModel)]="metalForm.creationDate" name="metalDate" (ngModelChange)="validateMetal()" /></label>
            @if (metalErrors['creationDate']) { <small class="field-error">{{ metalErrors['creationDate'] }}</small> }
          }
          <div class="dialog-actions"><button type="button" class="btn btn-outline-light" (click)="closeModals()">Discard</button><button class="btn btn-primary" [disabled]="!metalValid">Save</button></div>
        </form>
      </div>
    }

    @if (entryModal && activeMetal) {
      <div class="modal-backdrop-soft">
        <form class="dialog" (ngSubmit)="saveEntry()">
          <h3>{{ editingEntry ? 'Edit' : 'Add' }} Buy/Sell Entry</h3>
          <label>Type<select class="form-select" [(ngModel)]="entryForm.type" name="entryType" (ngModelChange)="validateEntry()"><option [ngValue]="1">Buy</option><option [ngValue]="2">Sell</option></select></label>
          <label>Date<input class="form-control" type="date" [(ngModel)]="entryForm.date" name="entryDate" (ngModelChange)="validateEntry()" /></label>
          @if (entryErrors['date']) { <small class="field-error">{{ entryErrors['date'] }}</small> }
          <label>Note<textarea class="form-control" [(ngModel)]="entryForm.note" name="entryNote" (ngModelChange)="validateEntry()"></textarea></label>
          @if (entryErrors['note']) { <small class="field-error">{{ entryErrors['note'] }}</small> }
          <label>Price per gram<input class="form-control" type="number" step="0.0001" [(ngModel)]="entryForm.pricePerUnit" name="entryPrice" (ngModelChange)="validateEntry()" /></label>
          @if (entryErrors['pricePerUnit']) { <small class="field-error">{{ entryErrors['pricePerUnit'] }}</small> }
          <label>Quantity grams<input class="form-control" type="number" step="0.0001" [(ngModel)]="entryForm.quantity" name="entryQty" (ngModelChange)="validateEntry()" /></label>
          @if (entryErrors['quantity']) { <small class="field-error">{{ entryErrors['quantity'] }}</small> }
          <div class="dialog-actions"><button type="button" class="btn btn-outline-light" (click)="closeModals()">Discard</button><button class="btn btn-primary" [disabled]="!entryValid">Confirm</button></div>
        </form>
      </div>
    }

    @if (priceModal && activeMetal) {
      <div class="modal-backdrop-soft">
        <form class="dialog" (ngSubmit)="savePrice()">
          <h3>Add Price Entry</h3>
          <label>Date<input class="form-control" type="date" [(ngModel)]="priceForm.date" name="priceDate" (ngModelChange)="validatePrice()" /></label>
          @if (priceErrors['date']) { <small class="field-error">{{ priceErrors['date'] }}</small> }
          <label>Price per gram<input class="form-control" type="number" step="0.0001" [(ngModel)]="priceForm.pricePerUnit" name="priceValue" (ngModelChange)="validatePrice()" /></label>
          @if (priceErrors['pricePerUnit']) { <small class="field-error">{{ priceErrors['pricePerUnit'] }}</small> }
          <div class="dialog-actions"><button type="button" class="btn btn-outline-light" (click)="closeModals()">Discard</button><button class="btn btn-primary" [disabled]="!priceValid">Confirm</button></div>
        </form>
      </div>
    }

    @if (statsModal && stats) {
      <div class="modal-backdrop-soft">
        <div class="dialog wide">
          <div class="toolbar mini"><h3>{{ stats.metric }}</h3><select class="form-select" [(ngModel)]="statsDuration" (ngModelChange)="reloadStats()"><option value="OneMonth">1 month</option><option value="ThreeMonths">3 months</option><option value="SixMonths">6 months</option><option value="OneYear">1 year</option><option value="ThreeYears">3 years</option><option value="FiveYears">5 years</option></select></div>
          <app-line-chart [series]="stats.points" />
          <div class="dialog-actions"><button class="btn btn-outline-light" (click)="closeModals()">Cancel</button></div>
        </div>
      </div>
    }

    @if (exportModal) {
      <div class="modal-backdrop-soft">
        <form class="dialog" (ngSubmit)="doExport()">
          <h3>Export Precious Metals</h3>
          <div class="dialog-actions start"><button type="button" class="btn btn-outline-light btn-sm" (click)="selectAllExport()">Select all</button><button type="button" class="btn btn-outline-light btn-sm" (click)="selectedExportIds.clear()">Reset</button></div>
          <div class="checklist">
            @for (metal of metals; track metal.id) {
              <label><input type="checkbox" [checked]="selectedExportIds.has(metal.id)" (change)="toggleExport(metal.id)" /> {{ metal.name }}</label>
            }
          </div>
          <label>From<input class="form-control" type="date" [(ngModel)]="exportForm.from" name="exportFrom" /></label>
          <label>To<input class="form-control" type="date" [(ngModel)]="exportForm.to" name="exportTo" /></label>
          <label>Format<select class="form-select" [(ngModel)]="exportForm.format" name="exportFormat"><option value="Xlsx">.xlsx</option><option value="Pdf">.pdf</option></select></label>
          <div class="dialog-actions"><button type="button" class="btn btn-outline-light" (click)="closeModals()">Cancel</button><button class="btn btn-primary" [disabled]="selectedExportIds.size === 0">Generate</button></div>
        </form>
      </div>
    }

    @if (deleteTarget) {
      <div class="modal-backdrop-soft">
        <div class="dialog"><h3>Confirm deletion</h3><p>{{ deleteText }}</p><div class="dialog-actions"><button class="btn btn-outline-light" (click)="deleteTarget=null">Cancel</button><button class="btn btn-danger" (click)="performDelete()">Delete</button></div></div>
      </div>
    }
  `,
  styles: [`
    .feature { padding: 1.25rem; }
    .toolbar { display:flex; justify-content:space-between; gap:1rem; align-items:flex-start; margin-bottom:1rem; }
    .toolbar h2 { margin:.15rem 0; font-size:1.35rem; }
    .toolbar.mini { align-items:center; }
    .actions, .card-actions, .history-tools, .row-actions, .dialog-actions { display:flex; gap:.5rem; flex-wrap:wrap; }
    .dialog-actions { justify-content:flex-end; margin-top:1rem; }
    .dialog-actions.start { justify-content:flex-start; margin:.25rem 0 .5rem; }
    .filters { display:grid; grid-template-columns: 1fr 180px 160px; gap:.75rem; margin-bottom:1rem; }
    .cards { display:grid; gap:.85rem; }
    .asset-card { border:1px solid var(--border); border-radius:1rem; background:rgba(255,255,255,.03); padding:1rem; }
    .card-main { display:flex; justify-content:space-between; gap:1rem; }
    h3 { margin:0; font-size:1.05rem; }
    p { color:var(--fg-muted); margin:.25rem 0; }
    small { color:var(--fg-subtle); }
    .title-row { display:flex; align-items:center; gap:.5rem; }
    .badge { border:1px solid var(--neon); color:var(--neon); border-radius:999px; padding:.1rem .4rem; font-size:.7rem; }
    .metrics { text-align:right; display:grid; gap:.15rem; }
    .metrics strong { font-size:1.12rem; }
    .card-actions { justify-content:flex-end; margin-top:.75rem; }
    .history { border-top:1px solid var(--border); margin-top:.9rem; padding-top:.9rem; }
    .history-tabs { display:flex; gap:.4rem; margin-bottom:.75rem; }
    .history-tabs button, .pager button { border:1px solid var(--border); background:var(--surface); color:var(--fg-muted); border-radius:999px; padding:.35rem .7rem; }
    .history-tabs button.active { color:var(--neon); border-color:var(--neon); }
    .history-tools { justify-content:flex-end; margin-bottom:.75rem; }
    .pager { display:flex; justify-content:center; gap:.75rem; align-items:center; color:var(--fg-muted); margin-top:.75rem; }
    .main-pager { margin-top:1.1rem; }
    .empty { padding:2rem; text-align:center; color:var(--fg-muted); border:1px dashed var(--border); border-radius:1rem; }
    .modal-backdrop-soft { position:fixed; inset:0; z-index:1040; background:rgba(0,0,0,.65); display:grid; place-items:center; padding:1rem; }
    .dialog { width:min(540px, 100%); max-height:90vh; overflow:auto; background:var(--surface); border:1px solid var(--border-strong); border-radius:1rem; padding:1rem; box-shadow:0 20px 60px rgba(0,0,0,.35); }
    .dialog.wide { width:min(820px, 100%); }
    label { display:block; margin-top:.7rem; color:var(--fg-muted); }
    .field-error { color:#ff8d8d; display:block; margin-top:.25rem; }
    .checklist { display:grid; max-height:180px; overflow:auto; border:1px solid var(--border); border-radius:.75rem; padding:.5rem; }
    @media (max-width: 760px) { .toolbar, .card-main { flex-direction:column; } .metrics { text-align:left; } .filters { grid-template-columns:1fr; } }
  `]
})
export class PreciousMetalsComponent implements OnInit {
  private readonly api = inject(AssetTrackerApi);
  readonly fmtMoney = fmtMoney;
  readonly fmtUnits = fmtUnits;
  metals: PreciousMetal[] = [];
  page = 1;
  totalPages = 0;
  search = '';
  sortBy = 'Name';
  sortDirection = 'Asc';
  error = '';
  toast = '';
  expandedId: string | null = null;
  historyTab: 'entries' | 'prices' = 'entries';
  entries: PagedResult<PreciousMetalEntry> = this.emptyPage();
  prices: PagedResult<PreciousMetalPriceEntry> = this.emptyPage();
  entryPage = 1;
  pricePage = 1;

  metalModal = false;
  editingMetal: PreciousMetal | null = null;
  metalForm = { name: '', description: '', creationDate: isoToday() };
  metalErrors: Record<string, string> = {};
  metalValid = false;

  entryModal = false;
  activeMetal: PreciousMetal | null = null;
  editingEntry: PreciousMetalEntry | null = null;
  entryForm = { type: 1 as PreciousMetalTxType, date: isoToday(), note: '', quantity: 0, pricePerUnit: 0 };
  entryErrors: Record<string, string> = {};
  entryValid = false;

  priceModal = false;
  priceForm = { date: isoToday(), pricePerUnit: 0 };
  priceErrors: Record<string, string> = {};
  priceValid = false;

  statsModal = false;
  statsMetal: PreciousMetal | null = null;
  statsSource: 'Entries' | 'Prices' = 'Entries';
  statsDuration = 'OneMonth';
  stats: PreciousMetalStatistics | null = null;

  exportModal = false;
  selectedExportIds = new Set<string>();
  exportForm = { from: isoOffset(-30), to: isoToday(), format: 'Xlsx' as ReportFormat };
  deleteTarget: { kind: 'metal' | 'entry' | 'price'; metal: PreciousMetal; item?: PreciousMetalEntry | PreciousMetalPriceEntry } | null = null;
  deleteText = '';

  ngOnInit() { this.load(); }

  load() {
    this.api.listPreciousMetals({ search: this.search, sortBy: this.sortBy, sortDirection: this.sortDirection, page: this.page }).subscribe({
      next: r => { this.metals = r.items; this.totalPages = r.totalPages; this.error = ''; },
      error: e => this.error = this.readError(e)
    });
  }

  toggle(metal: PreciousMetal) {
    this.expandedId = this.expandedId === metal.id ? null : metal.id;
    this.historyTab = 'entries';
    this.entryPage = 1;
    if (this.expandedId) this.loadEntries(metal);
  }

  loadEntries(metal: PreciousMetal) {
    this.api.listPreciousMetalEntries(metal.id, this.entryPage).subscribe({ next: r => this.entries = r, error: e => this.error = this.readError(e) });
  }
  loadPrices(metal: PreciousMetal) {
    this.api.listPreciousMetalPrices(metal.id, this.pricePage).subscribe({ next: r => this.prices = r, error: e => this.error = this.readError(e) });
  }

  openMetal(metal?: PreciousMetal) {
    this.editingMetal = metal ?? null;
    this.metalForm = metal ? { name: metal.name, description: metal.description ?? '', creationDate: metal.creationDate } : { name: '', description: '', creationDate: isoToday() };
    this.metalModal = true;
    this.validateMetal();
  }
  validateMetal() {
    const e: Record<string, string> = {};
    const name = this.metalForm.name.trim();
    if (name.length < 3 || name.length > 30) e['name'] = 'Name must be 3 to 30 characters.';
    if (this.metalForm.description.trim().length > 200) e['description'] = 'Description must be at most 200 characters.';
    if (this.editingMetal && (!this.metalForm.creationDate || this.metalForm.creationDate > isoToday())) e['creationDate'] = 'Creation date cannot be in the future.';
    this.metalErrors = e;
    this.metalValid = Object.keys(e).length === 0;
  }
  saveMetal() {
    if (!this.metalValid) return;
    const body = { name: this.metalForm.name.trim(), description: this.metalForm.description.trim() || null, creationDate: this.metalForm.creationDate };
    const req = this.editingMetal ? this.api.updatePreciousMetal(this.editingMetal.id, body) : this.api.createPreciousMetal(body);
    req.subscribe({ next: () => { this.closeModals(); this.toast = 'Precious metal saved.'; this.load(); }, error: e => this.error = this.readError(e) });
  }

  openEntry(metal: PreciousMetal, entry?: PreciousMetalEntry) {
    this.activeMetal = metal;
    this.editingEntry = entry ?? null;
    this.entryForm = entry ? { type: entry.type, date: entry.date, note: entry.note ?? '', quantity: entry.quantity, pricePerUnit: entry.pricePerUnit } : { type: 1, date: isoToday(), note: '', quantity: 0, pricePerUnit: 0 };
    this.entryModal = true;
    this.validateEntry();
  }
  validateEntry() {
    const e: Record<string, string> = {};
    if (!this.entryForm.date || this.entryForm.date > isoToday()) e['date'] = 'Date is mandatory and cannot be in the future.';
    if (this.activeMetal && this.entryForm.date < this.activeMetal.creationDate) e['date'] = 'Date cannot be before creation date.';
    if (this.entryForm.note.trim().length > 200) e['note'] = 'Note must be at most 200 characters.';
    if (Number(this.entryForm.quantity) <= 0) e['quantity'] = 'Quantity must be greater than zero.';
    if (Number(this.entryForm.pricePerUnit) <= 0) e['pricePerUnit'] = 'Price per gram must be greater than zero.';
    this.entryErrors = e;
    this.entryValid = Object.keys(e).length === 0;
  }
  saveEntry() {
    if (!this.activeMetal || !this.entryValid) return;
    const body = { ...this.entryForm, note: this.entryForm.note.trim() || null, quantity: Number(this.entryForm.quantity), pricePerUnit: Number(this.entryForm.pricePerUnit) };
    const req = this.editingEntry ? this.api.updatePreciousMetalEntry(this.activeMetal.id, this.editingEntry.id, body) : this.api.addPreciousMetalEntry(this.activeMetal.id, body);
    req.subscribe({ next: () => { const m = this.activeMetal!; this.closeModals(); this.toast = 'Entry saved.'; this.load(); this.expandedId = m.id; this.loadEntries(m); }, error: e => this.error = this.readError(e) });
  }

  openPrice(metal: PreciousMetal) {
    this.activeMetal = metal;
    this.priceForm = { date: isoToday(), pricePerUnit: 0 };
    this.priceModal = true;
    this.validatePrice();
  }
  validatePrice() {
    const e: Record<string, string> = {};
    if (!this.priceForm.date || this.priceForm.date > isoToday()) e['date'] = 'Date is mandatory and cannot be in the future.';
    if (this.activeMetal && this.priceForm.date < this.activeMetal.creationDate) e['date'] = 'Date cannot be before creation date.';
    if (Number(this.priceForm.pricePerUnit) < 0) e['pricePerUnit'] = 'Price cannot be negative.';
    this.priceErrors = e;
    this.priceValid = Object.keys(e).length === 0;
  }
  savePrice() {
    if (!this.activeMetal || !this.priceValid) return;
    const body = { date: this.priceForm.date, pricePerUnit: Number(this.priceForm.pricePerUnit) };
    this.api.addPreciousMetalPrice(this.activeMetal.id, body).subscribe({ next: () => { const m = this.activeMetal!; this.closeModals(); this.toast = 'Price saved.'; this.load(); this.expandedId = m.id; this.historyTab = 'prices'; this.loadPrices(m); }, error: e => this.error = this.readError(e) });
  }

  openStats(metal: PreciousMetal, source: 'Entries' | 'Prices') {
    this.statsModal = true; this.statsMetal = metal; this.statsSource = source; this.statsDuration = 'OneMonth'; this.reloadStats();
  }
  reloadStats() {
    if (!this.statsMetal) return;
    this.api.getPreciousMetalStatistics(this.statsMetal.id, this.statsSource, this.statsDuration).subscribe({ next: r => this.stats = r, error: e => this.error = this.readError(e) });
  }

  openExport() { this.exportModal = true; this.selectedExportIds = new Set(this.metals.map(x => x.id)); this.exportForm = { from: isoOffset(-30), to: isoToday(), format: 'Xlsx' }; }
  selectAllExport() { this.selectedExportIds = new Set(this.metals.map(x => x.id)); }
  toggleExport(id: string) { this.selectedExportIds.has(id) ? this.selectedExportIds.delete(id) : this.selectedExportIds.add(id); }
  doExport() {
    if (this.exportForm.to < this.exportForm.from) { this.error = 'Export end date must be on or after start date.'; return; }
    this.api.exportPreciousMetals({ preciousMetalIds: [...this.selectedExportIds], ...this.exportForm }).subscribe({ next: r => { this.download(r); this.closeModals(); }, error: e => this.error = this.readError(e) });
  }

  confirmDelete(kind: 'metal' | 'entry' | 'price', metal: PreciousMetal, item?: PreciousMetalEntry | PreciousMetalPriceEntry) {
    this.deleteTarget = { kind, metal, item };
    this.deleteText = kind === 'metal' ? `Delete ${metal.name}?` : `Delete this ${kind} entry?`;
  }
  performDelete() {
    if (!this.deleteTarget) return;
    const t = this.deleteTarget;
    const req = t.kind === 'metal' ? this.api.deletePreciousMetal(t.metal.id)
      : t.kind === 'entry' ? this.api.deletePreciousMetalEntry(t.metal.id, t.item!.id)
      : this.api.deletePreciousMetalPrice(t.metal.id, t.item!.id);
    req.subscribe({ next: () => { this.deleteTarget = null; this.toast = 'Deleted.'; this.load(); if (t.kind === 'entry') this.loadEntries(t.metal); if (t.kind === 'price') this.loadPrices(t.metal); }, error: e => this.error = this.readError(e) });
  }

  closeModals() {
    this.metalModal = this.entryModal = this.priceModal = this.statsModal = this.exportModal = false;
    this.editingMetal = null; this.editingEntry = null; this.activeMetal = null; this.stats = null;
  }

  private emptyPage<T>(): PagedResult<T> { return { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 }; }
  private readError(e: any) { return e?.error?.detail || e?.message || 'Something went wrong.'; }
  private download(response: HttpResponse<Blob>) {
    const blob = response.body;
    if (!blob) return;
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = this.exportForm.format === 'Pdf' ? 'precious-metals.pdf' : 'precious-metals.xlsx';
    a.click();
    URL.revokeObjectURL(url);
  }
}

