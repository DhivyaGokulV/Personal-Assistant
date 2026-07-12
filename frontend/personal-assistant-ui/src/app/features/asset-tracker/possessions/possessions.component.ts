import { CommonModule } from '@angular/common';
import { HttpResponse } from '@angular/common/http';
import { Component, Input, OnChanges, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AssetTrackerApi } from '../asset-tracker.api';
import { AssetStatus, fmtMoney, fmtUnits, isoOffset, isoToday, JewelleryItem, PagedResult, PersonalAssetItem, ReportFormat } from '../asset-tracker.models';

type Mode = 'jewellery' | 'personal-assets';
type Possession = JewelleryItem | PersonalAssetItem;

@Component({
  selector: 'app-asset-tracker-possessions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="surface feature">
      <div class="toolbar">
        <div>
          <span class="eyebrow">Assets</span>
          <h2>{{ title }}</h2>
          <p class="text-muted-soft mb-0">{{ subtitle }}</p>
        </div>
        <div class="actions">
          <button class="btn btn-outline-light btn-sm" (click)="openExport()">Export</button>
          <button class="btn btn-primary btn-sm" (click)="openItem()">Create {{ itemLabel }}</button>
        </div>
      </div>

      <div class="filters">
        <input class="form-control" placeholder="Search" [(ngModel)]="search" (ngModelChange)="page=1; load()" />
        <select class="form-select" [(ngModel)]="status" (ngModelChange)="page=1; load()">
          <option [ngValue]="1">In possession</option>
          <option [ngValue]="2">Sold</option>
          <option ngValue="all">All</option>
        </select>
        <select class="form-select" [(ngModel)]="sortBy" (ngModelChange)="load()">
          <option value="Name">Name</option>
          <option value="BuyingDate">Buying date</option>
          <option value="BuyingPrice">Buying price</option>
          <option value="Status">Status</option>
          <option value="SellingDate">Selling date</option>
        </select>
        <select class="form-select" [(ngModel)]="sortDirection" (ngModelChange)="load()"><option value="Asc">Ascending</option><option value="Desc">Descending</option></select>
      </div>

      @if (error) { <div class="alert alert-danger">{{ error }}</div> }
      @if (toast) { <div class="alert alert-success">{{ toast }}</div> }

      <div class="cards">
        @for (item of items; track item.id) {
          <article class="asset-card">
            <div class="card-main">
              <div>
                <div class="title-row"><h3>{{ item.name }}</h3><span class="badge" [class.sold]="item.status === 2">{{ item.status === 1 ? 'In possession' : 'Sold' }}</span></div>
                <p>{{ item.description || 'No description' }}</p>
                <small>Bought {{ item.buyingDate }}</small>
              </div>
              <div class="metrics">
                <span>Purchase {{ fmtMoney(item.buyingPrice) }}</span>
                @if (mode === 'jewellery') { <small>{{ fmtUnits(asJewellery(item).quantityInGrams) }} g</small> }
                @if (item.status === 2) {
                  <strong>Sold {{ fmtMoney(item.sellingPrice) }}</strong>
                  <small>{{ item.sellingDate }} · {{ item.sellingNote || 'No selling note' }}</small>
                }
              </div>
            </div>
            <div class="card-actions">
              <button class="btn btn-outline-light btn-sm" (click)="openItem(item)">Edit</button>
              <button class="btn btn-outline-danger btn-sm" (click)="confirmDelete(item)">Delete</button>
              @if (item.status === 1) { <button class="btn btn-outline-info btn-sm" (click)="openSell(item)">Sell</button> }
            </div>
          </article>
        } @empty {
          <div class="empty">No {{ title.toLowerCase() }} found.</div>
        }
      </div>

      <div class="pager"><button (click)="page=page-1; load()" [disabled]="page<=1">Prev</button><span>{{ page }} / {{ totalPages || 1 }}</span><button (click)="page=page+1; load()" [disabled]="page>=totalPages">Next</button></div>
    </section>

    @if (itemModal) {
      <div class="modal-backdrop-soft">
        <form class="dialog" (ngSubmit)="saveItem()">
          <h3>{{ editingItem ? 'Edit' : 'Create' }} {{ itemLabel }}</h3>
          <label>Name<input class="form-control" [(ngModel)]="itemForm.name" name="name" (ngModelChange)="validateItem()" /></label>
          @if (itemErrors['name']) { <small class="field-error">{{ itemErrors['name'] }}</small> }
          <label>Description<textarea class="form-control" [(ngModel)]="itemForm.description" name="description" (ngModelChange)="validateItem()"></textarea></label>
          @if (itemErrors['description']) { <small class="field-error">{{ itemErrors['description'] }}</small> }
          <label>Buying date<input class="form-control" type="date" [(ngModel)]="itemForm.buyingDate" name="buyingDate" (ngModelChange)="validateItem()" /></label>
          @if (itemErrors['buyingDate']) { <small class="field-error">{{ itemErrors['buyingDate'] }}</small> }
          <label>Buying price<input class="form-control" type="number" step="0.01" [(ngModel)]="itemForm.buyingPrice" name="buyingPrice" (ngModelChange)="validateItem()" /></label>
          @if (itemErrors['buyingPrice']) { <small class="field-error">{{ itemErrors['buyingPrice'] }}</small> }
          @if (mode === 'jewellery') {
            <label>Quantity grams<input class="form-control" type="number" step="0.0001" [(ngModel)]="itemForm.quantityInGrams" name="quantity" (ngModelChange)="validateItem()" /></label>
            @if (itemErrors['quantityInGrams']) { <small class="field-error">{{ itemErrors['quantityInGrams'] }}</small> }
          }
          @if (editingItem && editingItem.status === 2) {
            <label>Status<select class="form-select" [(ngModel)]="itemForm.status" name="status" (ngModelChange)="validateItem()"><option [ngValue]="2">Sold</option><option [ngValue]="1">In possession</option></select></label>
            <small class="text-muted-soft">Changing a sold item back to in possession clears selling details.</small>
          }
          <div class="dialog-actions"><button type="button" class="btn btn-outline-light" (click)="closeModals()">Discard</button><button class="btn btn-primary" [disabled]="!itemValid">Save</button></div>
        </form>
      </div>
    }

    @if (sellModal && sellingItem) {
      <div class="modal-backdrop-soft">
        <form class="dialog" (ngSubmit)="saveSell()">
          <h3>Sell {{ sellingItem.name }}</h3>
          <label>Selling note<textarea class="form-control" [(ngModel)]="sellForm.sellingNote" name="sellingNote" (ngModelChange)="validateSell()"></textarea></label>
          @if (sellErrors['sellingNote']) { <small class="field-error">{{ sellErrors['sellingNote'] }}</small> }
          <label>Selling date<input class="form-control" type="date" [(ngModel)]="sellForm.sellingDate" name="sellingDate" (ngModelChange)="validateSell()" /></label>
          @if (sellErrors['sellingDate']) { <small class="field-error">{{ sellErrors['sellingDate'] }}</small> }
          <label>Selling price<input class="form-control" type="number" step="0.01" [(ngModel)]="sellForm.sellingPrice" name="sellingPrice" (ngModelChange)="validateSell()" /></label>
          @if (sellErrors['sellingPrice']) { <small class="field-error">{{ sellErrors['sellingPrice'] }}</small> }
          <div class="dialog-actions"><button type="button" class="btn btn-outline-light" (click)="closeModals()">Discard</button><button class="btn btn-primary" [disabled]="!sellValid">Sell</button></div>
        </form>
      </div>
    }

    @if (exportModal) {
      <div class="modal-backdrop-soft">
        <form class="dialog" (ngSubmit)="doExport()">
          <h3>Export {{ title }}</h3>
          <label>From<input class="form-control" type="date" [(ngModel)]="exportForm.from" name="exportFrom" /></label>
          <label>To<input class="form-control" type="date" [(ngModel)]="exportForm.to" name="exportTo" /></label>
          <label>Format<select class="form-select" [(ngModel)]="exportForm.format" name="exportFormat"><option value="Xlsx">.xlsx</option><option value="Pdf">.pdf</option></select></label>
          <div class="dialog-actions"><button type="button" class="btn btn-outline-light" (click)="closeModals()">Cancel</button><button class="btn btn-primary">Generate</button></div>
        </form>
      </div>
    }

    @if (deleteItem) {
      <div class="modal-backdrop-soft">
        <div class="dialog"><h3>Confirm deletion</h3><p>Delete {{ deleteItem.name }}?</p><div class="dialog-actions"><button class="btn btn-outline-light" (click)="deleteItem=null">Cancel</button><button class="btn btn-danger" (click)="performDelete()">Delete</button></div></div>
      </div>
    }
  `,
  styles: [`
    .feature { padding:1.25rem; }
    .toolbar { display:flex; justify-content:space-between; gap:1rem; align-items:flex-start; margin-bottom:1rem; }
    .toolbar h2 { margin:.15rem 0; font-size:1.35rem; }
    .actions, .card-actions, .dialog-actions { display:flex; gap:.5rem; flex-wrap:wrap; }
    .dialog-actions { justify-content:flex-end; margin-top:1rem; }
    .filters { display:grid; grid-template-columns:1fr 160px 160px 150px; gap:.75rem; margin-bottom:1rem; }
    .cards { display:grid; gap:.85rem; }
    .asset-card { border:1px solid var(--border); border-radius:1rem; background:rgba(255,255,255,.03); padding:1rem; }
    .card-main { display:flex; justify-content:space-between; gap:1rem; }
    h3 { margin:0; font-size:1.05rem; }
    p { color:var(--fg-muted); margin:.25rem 0; }
    small { color:var(--fg-subtle); }
    .title-row { display:flex; gap:.5rem; align-items:center; }
    .badge { border:1px solid var(--neon); color:var(--neon); border-radius:999px; padding:.1rem .45rem; font-size:.7rem; }
    .badge.sold { border-color:#ffbb66; color:#ffbb66; }
    .metrics { display:grid; gap:.15rem; text-align:right; }
    .card-actions { justify-content:flex-end; margin-top:.75rem; }
    .pager { display:flex; justify-content:center; gap:.75rem; align-items:center; color:var(--fg-muted); margin-top:1rem; }
    .pager button { border:1px solid var(--border); background:var(--surface); color:var(--fg-muted); border-radius:999px; padding:.35rem .7rem; }
    .empty { padding:2rem; text-align:center; color:var(--fg-muted); border:1px dashed var(--border); border-radius:1rem; }
    .modal-backdrop-soft { position:fixed; inset:0; z-index:1040; background:rgba(0,0,0,.65); display:grid; place-items:center; padding:1rem; }
    .dialog { width:min(540px, 100%); max-height:90vh; overflow:auto; background:var(--surface); border:1px solid var(--border-strong); border-radius:1rem; padding:1rem; box-shadow:0 20px 60px rgba(0,0,0,.35); }
    label { display:block; margin-top:.7rem; color:var(--fg-muted); }
    .field-error { color:#ff8d8d; display:block; margin-top:.25rem; }
    @media (max-width:760px) { .toolbar, .card-main { flex-direction:column; } .metrics { text-align:left; } .filters { grid-template-columns:1fr; } }
  `]
})
export class PossessionsComponent implements OnChanges {
  @Input({ required: true }) mode: Mode = 'jewellery';
  private readonly api = inject(AssetTrackerApi);
  readonly fmtMoney = fmtMoney;
  readonly fmtUnits = fmtUnits;

  items: Possession[] = [];
  page = 1;
  totalPages = 0;
  search = '';
  status: AssetStatus | 'all' = 1;
  sortBy = 'Name';
  sortDirection = 'Asc';
  error = '';
  toast = '';

  itemModal = false;
  editingItem: Possession | null = null;
  itemForm = { name: '', description: '', buyingDate: isoToday(), buyingPrice: 0, quantityInGrams: 0, status: 1 as AssetStatus };
  itemErrors: Record<string, string> = {};
  itemValid = false;

  sellModal = false;
  sellingItem: Possession | null = null;
  sellForm = { sellingNote: '', sellingDate: isoToday(), sellingPrice: 0 };
  sellErrors: Record<string, string> = {};
  sellValid = false;

  exportModal = false;
  exportForm = { from: isoOffset(-30), to: isoToday(), format: 'Xlsx' as ReportFormat };
  deleteItem: Possession | null = null;

  get title() { return this.mode === 'jewellery' ? 'Jewellery' : 'Personal Assets'; }
  get itemLabel() { return this.mode === 'jewellery' ? 'Jewel' : 'Asset'; }
  get subtitle() { return this.mode === 'jewellery' ? 'Track jewellery purchases and sale lifecycle.' : 'Track cars, bikes, watches, and other valuable personal assets.'; }

  ngOnChanges() { this.page = 1; this.status = 1; this.load(); }

  load() {
    const opts = { search: this.search, status: this.status, sortBy: this.sortBy, sortDirection: this.sortDirection, page: this.page };
    const req = this.mode === 'jewellery' ? this.api.listJewellery(opts) : this.api.listPersonalAssets(opts);
    req.subscribe({ next: r => { this.items = r.items; this.totalPages = r.totalPages; this.error = ''; }, error: e => this.error = this.readError(e) });
  }

  openItem(item?: Possession) {
    this.editingItem = item ?? null;
    this.itemForm = item
      ? { name: item.name, description: item.description ?? '', buyingDate: item.buyingDate, buyingPrice: item.buyingPrice, quantityInGrams: this.mode === 'jewellery' ? this.asJewellery(item).quantityInGrams : 0, status: item.status }
      : { name: '', description: '', buyingDate: isoToday(), buyingPrice: 0, quantityInGrams: 0, status: 1 };
    this.itemModal = true;
    this.validateItem();
  }

  validateItem() {
    const e: Record<string, string> = {};
    const name = this.itemForm.name.trim();
    if (name.length < 3 || name.length > 30) e['name'] = 'Name must be 3 to 30 characters.';
    if (this.itemForm.description.trim().length > 200) e['description'] = 'Description must be at most 200 characters.';
    if (!this.itemForm.buyingDate || this.itemForm.buyingDate > isoToday()) e['buyingDate'] = 'Buying date is mandatory and cannot be in the future.';
    if (Number(this.itemForm.buyingPrice) <= 0) e['buyingPrice'] = 'Buying price must be greater than zero.';
    if (this.mode === 'jewellery' && Number(this.itemForm.quantityInGrams) <= 0) e['quantityInGrams'] = 'Quantity must be greater than zero.';
    this.itemErrors = e;
    this.itemValid = Object.keys(e).length === 0;
  }

  saveItem() {
    if (!this.itemValid) return;
    const body: any = {
      name: this.itemForm.name.trim(),
      description: this.itemForm.description.trim() || null,
      buyingDate: this.itemForm.buyingDate,
      buyingPrice: Number(this.itemForm.buyingPrice),
      status: this.editingItem ? this.itemForm.status : null
    };
    if (this.mode === 'jewellery') body.quantityInGrams = Number(this.itemForm.quantityInGrams);
    const req = this.editingItem
      ? this.mode === 'jewellery' ? this.api.updateJewellery(this.editingItem.id, body) : this.api.updatePersonalAsset(this.editingItem.id, body)
      : this.mode === 'jewellery' ? this.api.createJewellery(body) : this.api.createPersonalAsset(body);
    req.subscribe({ next: () => { this.closeModals(); this.toast = `${this.itemLabel} saved.`; this.load(); }, error: e => this.error = this.readError(e) });
  }

  openSell(item: Possession) {
    this.sellingItem = item;
    this.sellForm = { sellingNote: '', sellingDate: isoToday(), sellingPrice: 0 };
    this.sellModal = true;
    this.validateSell();
  }

  validateSell() {
    const e: Record<string, string> = {};
    if (this.sellForm.sellingNote.trim().length > 200) e['sellingNote'] = 'Selling note must be at most 200 characters.';
    if (!this.sellForm.sellingDate || this.sellForm.sellingDate > isoToday()) e['sellingDate'] = 'Selling date is mandatory and cannot be in the future.';
    if (this.sellingItem && this.sellForm.sellingDate < this.sellingItem.buyingDate) e['sellingDate'] = 'Selling date cannot be before buying date.';
    if (Number(this.sellForm.sellingPrice) <= 0) e['sellingPrice'] = 'Selling price must be greater than zero.';
    this.sellErrors = e;
    this.sellValid = Object.keys(e).length === 0;
  }

  saveSell() {
    if (!this.sellingItem || !this.sellValid) return;
    const body = { sellingNote: this.sellForm.sellingNote.trim() || null, sellingDate: this.sellForm.sellingDate, sellingPrice: Number(this.sellForm.sellingPrice) };
    const req = this.mode === 'jewellery' ? this.api.sellJewellery(this.sellingItem.id, body) : this.api.sellPersonalAsset(this.sellingItem.id, body);
    req.subscribe({ next: () => { this.closeModals(); this.toast = `${this.itemLabel} sold.`; this.load(); }, error: e => this.error = this.readError(e) });
  }

  confirmDelete(item: Possession) { this.deleteItem = item; }
  performDelete() {
    if (!this.deleteItem) return;
    const req = this.mode === 'jewellery' ? this.api.deleteJewellery(this.deleteItem.id) : this.api.deletePersonalAsset(this.deleteItem.id);
    req.subscribe({ next: () => { this.deleteItem = null; this.toast = 'Deleted.'; this.load(); }, error: e => this.error = this.readError(e) });
  }

  openExport() { this.exportModal = true; this.exportForm = { from: isoOffset(-30), to: isoToday(), format: 'Xlsx' }; }
  doExport() {
    if (this.exportForm.to < this.exportForm.from) { this.error = 'Export end date must be on or after start date.'; return; }
    const req = this.mode === 'jewellery' ? this.api.exportJewellery(this.exportForm) : this.api.exportPersonalAssets(this.exportForm);
    req.subscribe({ next: r => { this.download(r); this.closeModals(); }, error: e => this.error = this.readError(e) });
  }

  closeModals() {
    this.itemModal = this.sellModal = this.exportModal = false;
    this.editingItem = null;
    this.sellingItem = null;
  }

  asJewellery(item: Possession): JewelleryItem { return item as JewelleryItem; }
  private readError(e: any) { return e?.error?.detail || e?.message || 'Something went wrong.'; }
  private download(response: HttpResponse<Blob>) {
    const blob = response.body;
    if (!blob) return;
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = this.mode === 'jewellery'
      ? (this.exportForm.format === 'Pdf' ? 'jewellery.pdf' : 'jewellery.xlsx')
      : (this.exportForm.format === 'Pdf' ? 'personal-assets.pdf' : 'personal-assets.xlsx');
    a.click();
    URL.revokeObjectURL(url);
  }
}

