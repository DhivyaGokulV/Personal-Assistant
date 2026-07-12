import { CommonModule } from '@angular/common';
import { HttpResponse } from '@angular/common/http';
import { Component, Input, OnChanges, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AssetTrackerApi } from '../asset-tracker.api';
import {
  fmtMoney,
  isoOffset,
  isoToday,
  LiabilityAccount,
  LiabilityAccountEntry,
  LiabilityAccountStatistics,
  LiabilityAccountStatus,
  LiabilityAccountStatusEntry,
  LiabilityAccountTxType,
  PagedResult,
  ReportFormat
} from '../asset-tracker.models';
import { LineChartComponent } from '../shared/line-chart.component';

type LiabilityMode = 'loans' | 'debts' | 'credit-cards';

@Component({
  selector: 'app-asset-tracker-liability-accounts',
  standalone: true,
  imports: [CommonModule, FormsModule, LineChartComponent],
  template: `
    <section class="surface feature">
      <div class="toolbar">
        <div>
          <span class="eyebrow">Liabilities</span>
          <h2>{{ title }}</h2>
          <p class="text-muted-soft mb-0">{{ subtitle }}</p>
        </div>
        <div class="actions">
          <button class="btn btn-outline-light btn-sm" (click)="openExport()" [disabled]="accounts.length === 0">Export</button>
          <button class="btn btn-primary btn-sm" (click)="openAccount()">Create {{ singular }}</button>
        </div>
      </div>

      <div class="filters">
        <input class="form-control" placeholder="Search" [(ngModel)]="search" (ngModelChange)="page=1; load()" />
        <select class="form-select" [(ngModel)]="status" (ngModelChange)="page=1; load()">
          <option [ngValue]="1">Active</option>
          <option [ngValue]="2">Inactive</option>
          <option ngValue="all">All</option>
        </select>
        <select class="form-select" [(ngModel)]="sortBy" (ngModelChange)="load()">
          <option value="Name">Name</option>
          <option value="CreationDate">Creation date</option>
          <option value="StandingAmount">Standing amount</option>
          <option value="LastEntryDate">Last entry date</option>
          <option value="Status">Status</option>
        </select>
        <select class="form-select" [(ngModel)]="sortDirection" (ngModelChange)="load()">
          <option value="Asc">Ascending</option>
          <option value="Desc">Descending</option>
        </select>
      </div>

      @if (error) { <div class="alert alert-danger">{{ error }}</div> }
      @if (toast) { <div class="alert alert-success">{{ toast }}</div> }

      <div class="cards">
        @for (account of accounts; track account.id) {
          <article class="liability-card">
            <div class="card-main">
              <div>
                <div class="title-row">
                  <h3>{{ account.name }}</h3>
                  <span class="badge" [class.inactive]="account.status === 2">{{ account.status === 1 ? 'Active' : 'Inactive' }}</span>
                </div>
                <p>{{ account.description || 'No description' }}</p>
                <small>Created {{ account.creationDate }} · Last entry {{ account.lastEntryDate || '—' }}</small>
              </div>
              <div class="metrics">
                <span>Standing amount</span>
                <strong>{{ fmtMoney(account.standingAmount) }}</strong>
              </div>
            </div>
            <div class="card-actions">
              <button class="btn btn-outline-light btn-sm" (click)="openAccount(account)">Edit</button>
              <button class="btn btn-outline-info btn-sm" (click)="openStatus(account)">Status</button>
              <button class="btn btn-outline-danger btn-sm" (click)="confirmDelete('account', account)">Delete</button>
              <button class="btn btn-outline-light btn-sm" (click)="toggle(account)">{{ expandedId === account.id ? 'Collapse' : 'Expand' }}</button>
            </div>

            @if (expandedId === account.id) {
              <div class="history">
                <div class="history-tools">
                  <button class="btn btn-outline-info btn-sm" (click)="openStats(account)">View Statistics</button>
                  <button class="btn btn-primary btn-sm" (click)="openEntry(account)">Add Entry</button>
                </div>
                <div class="table-responsive">
                  <table class="table table-dark table-sm align-middle">
                    <thead><tr><th>Type</th><th>Date</th><th>Note</th><th>Amount</th><th>Standing</th><th></th></tr></thead>
                    <tbody>
                      @for (entry of entries.items; track entry.id) {
                        <tr>
                          <td>{{ entry.type === 1 ? 'Credit' : 'Debit' }}</td>
                          <td>{{ entry.date }}</td>
                          <td>{{ entry.note || '—' }}</td>
                          <td>{{ fmtMoney(entry.amount) }}</td>
                          <td>{{ fmtMoney(entry.runningBalance) }}</td>
                          <td class="row-actions">
                            <button class="btn btn-outline-light btn-sm" (click)="openEntry(account, entry)">Edit</button>
                            <button class="btn btn-outline-danger btn-sm" (click)="confirmDelete('entry', account, entry)">Delete</button>
                          </td>
                        </tr>
                      } @empty {
                        <tr><td colspan="6" class="text-center text-muted-soft">No credit/debit history yet.</td></tr>
                      }
                    </tbody>
                  </table>
                </div>
                <div class="pager"><button (click)="entryPage=entryPage-1; loadEntries(account)" [disabled]="entryPage<=1">Prev</button><span>{{ entryPage }} / {{ entries.totalPages || 1 }}</span><button (click)="entryPage=entryPage+1; loadEntries(account)" [disabled]="entryPage>=entries.totalPages">Next</button></div>

                <div class="status-history">
                  <h4>Status history</h4>
                  @for (row of statuses; track row.id) {
                    <span>{{ row.effectiveDate }} · {{ row.status === 1 ? 'Active' : 'Inactive' }}</span>
                  } @empty {
                    <span class="text-muted-soft">No status history yet.</span>
                  }
                </div>
              </div>
            }
          </article>
        } @empty {
          <div class="empty">No {{ title.toLowerCase() }} found.</div>
        }
      </div>

      <div class="pager main-pager">
        <button (click)="page=page-1; load()" [disabled]="page<=1">Prev</button>
        <span>{{ page }} / {{ totalPages || 1 }}</span>
        <button (click)="page=page+1; load()" [disabled]="page>=totalPages">Next</button>
      </div>
    </section>

    @if (accountModal) {
      <div class="modal-backdrop-soft">
        <form class="dialog" (ngSubmit)="saveAccount()">
          <h3>{{ editingAccount ? 'Edit' : 'Create' }} {{ singular }}</h3>
          <label>Name<input class="form-control" [(ngModel)]="accountForm.name" name="accountName" (ngModelChange)="validateAccount()" /></label>
          @if (accountErrors['name']) { <small class="field-error">{{ accountErrors['name'] }}</small> }
          <label>Description<textarea class="form-control" [(ngModel)]="accountForm.description" name="accountDescription" (ngModelChange)="validateAccount()"></textarea></label>
          @if (accountErrors['description']) { <small class="field-error">{{ accountErrors['description'] }}</small> }
          <label>Creation date<input class="form-control" type="date" [(ngModel)]="accountForm.creationDate" name="accountDate" (ngModelChange)="validateAccount()" /></label>
          @if (accountErrors['creationDate']) { <small class="field-error">{{ accountErrors['creationDate'] }}</small> }
          <div class="dialog-actions"><button type="button" class="btn btn-outline-light" (click)="closeModals()">Discard</button><button class="btn btn-primary" [disabled]="!accountValid">Save</button></div>
        </form>
      </div>
    }

    @if (statusModal && activeAccount) {
      <div class="modal-backdrop-soft">
        <form class="dialog" (ngSubmit)="saveStatus()">
          <h3>Change Status</h3>
          <label>Status<select class="form-select" [(ngModel)]="statusForm.status" name="statusValue" (ngModelChange)="validateStatus()"><option [ngValue]="1">Active</option><option [ngValue]="2">Inactive</option></select></label>
          <label>Effective from<input class="form-control" type="date" [(ngModel)]="statusForm.effectiveDate" name="statusDate" (ngModelChange)="validateStatus()" /></label>
          @if (statusErrors['effectiveDate']) { <small class="field-error">{{ statusErrors['effectiveDate'] }}</small> }
          @if (statusErrors['status']) { <small class="field-error">{{ statusErrors['status'] }}</small> }
          <div class="dialog-actions"><button type="button" class="btn btn-outline-light" (click)="closeModals()">Discard</button><button class="btn btn-primary" [disabled]="!statusValid">Confirm</button></div>
        </form>
      </div>
    }

    @if (entryModal && activeAccount) {
      <div class="modal-backdrop-soft">
        <form class="dialog" (ngSubmit)="saveEntry()">
          <h3>{{ editingEntry ? 'Edit' : 'Add' }} Entry</h3>
          <label>Type<select class="form-select" [(ngModel)]="entryForm.type" name="entryType" (ngModelChange)="validateEntry()"><option [ngValue]="1">Credit</option><option [ngValue]="2">Debit</option></select></label>
          <label>Date<input class="form-control" type="date" [(ngModel)]="entryForm.date" name="entryDate" (ngModelChange)="validateEntry()" /></label>
          @if (entryErrors['date']) { <small class="field-error">{{ entryErrors['date'] }}</small> }
          <label>Note<textarea class="form-control" [(ngModel)]="entryForm.note" name="entryNote" (ngModelChange)="validateEntry()"></textarea></label>
          @if (entryErrors['note']) { <small class="field-error">{{ entryErrors['note'] }}</small> }
          <label>Amount<input class="form-control" type="number" step="0.01" [(ngModel)]="entryForm.amount" name="entryAmount" (ngModelChange)="validateEntry()" /></label>
          @if (entryErrors['amount']) { <small class="field-error">{{ entryErrors['amount'] }}</small> }
          <div class="dialog-actions"><button type="button" class="btn btn-outline-light" (click)="closeModals()">Discard</button><button class="btn btn-primary" [disabled]="!entryValid">Confirm</button></div>
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
          <h3>Export {{ title }}</h3>
          <div class="dialog-actions start"><button type="button" class="btn btn-outline-light btn-sm" (click)="selectAllExport()">Select all</button><button type="button" class="btn btn-outline-light btn-sm" (click)="selectedExportIds.clear()">Reset</button></div>
          <div class="checklist">@for (account of accounts; track account.id) { <label><input type="checkbox" [checked]="selectedExportIds.has(account.id)" (change)="toggleExport(account.id)" /> {{ account.name }}</label> }</div>
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
    .feature { padding:1.25rem; }
    .toolbar { display:flex; justify-content:space-between; gap:1rem; align-items:flex-start; margin-bottom:1rem; }
    .toolbar h2 { margin:.15rem 0; font-size:1.35rem; }
    .toolbar.mini { align-items:center; }
    .actions, .card-actions, .history-tools, .row-actions, .dialog-actions { display:flex; gap:.5rem; flex-wrap:wrap; }
    .dialog-actions { justify-content:flex-end; margin-top:1rem; }
    .dialog-actions.start { justify-content:flex-start; margin:.25rem 0 .5rem; }
    .filters { display:grid; grid-template-columns:1fr 150px 180px 150px; gap:.75rem; margin-bottom:1rem; }
    .cards { display:grid; gap:.85rem; }
    .liability-card { border:1px solid var(--border); border-radius:1rem; background:rgba(255,255,255,.03); padding:1rem; }
    .card-main { display:flex; justify-content:space-between; gap:1rem; }
    h3 { margin:0; font-size:1.05rem; }
    h4 { margin:.85rem 0 .35rem; font-size:.9rem; color:var(--fg-muted); }
    p { color:var(--fg-muted); margin:.25rem 0; }
    small { color:var(--fg-subtle); }
    .title-row { display:flex; gap:.5rem; align-items:center; }
    .badge { border:1px solid var(--neon); color:var(--neon); border-radius:999px; padding:.1rem .45rem; font-size:.7rem; }
    .badge.inactive { border-color:#ffbb66; color:#ffbb66; }
    .metrics { text-align:right; display:grid; gap:.15rem; }
    .metrics strong { font-size:1.15rem; }
    .card-actions { justify-content:flex-end; margin-top:.75rem; }
    .history { border-top:1px solid var(--border); margin-top:.9rem; padding-top:.9rem; }
    .history-tools { justify-content:flex-end; margin-bottom:.75rem; }
    .status-history { display:flex; flex-wrap:wrap; gap:.5rem; border-top:1px solid var(--border); margin-top:.8rem; padding-top:.5rem; }
    .status-history h4 { width:100%; }
    .status-history span { border:1px solid var(--border); border-radius:999px; padding:.2rem .55rem; color:var(--fg-muted); }
    .pager { display:flex; justify-content:center; gap:.75rem; align-items:center; color:var(--fg-muted); margin-top:.75rem; }
    .pager button { border:1px solid var(--border); background:var(--surface); color:var(--fg-muted); border-radius:999px; padding:.35rem .7rem; }
    .main-pager { margin-top:1.1rem; }
    .empty { padding:2rem; text-align:center; color:var(--fg-muted); border:1px dashed var(--border); border-radius:1rem; }
    .modal-backdrop-soft { position:fixed; inset:0; z-index:1040; background:rgba(0,0,0,.65); display:grid; place-items:center; padding:1rem; }
    .dialog { width:min(540px, 100%); max-height:90vh; overflow:auto; background:var(--surface); border:1px solid var(--border-strong); border-radius:1rem; padding:1rem; box-shadow:0 20px 60px rgba(0,0,0,.35); }
    .dialog.wide { width:min(820px, 100%); }
    label { display:block; margin-top:.7rem; color:var(--fg-muted); }
    .field-error { color:#ff8d8d; display:block; margin-top:.25rem; }
    .checklist { display:grid; max-height:180px; overflow:auto; border:1px solid var(--border); border-radius:.75rem; padding:.5rem; }
    @media (max-width:760px) { .toolbar, .card-main { flex-direction:column; } .metrics { text-align:left; } .filters { grid-template-columns:1fr; } }
  `]
})
export class LiabilityAccountsComponent implements OnChanges {
  @Input({ required: true }) mode: LiabilityMode = 'loans';
  private readonly api = inject(AssetTrackerApi);
  readonly fmtMoney = fmtMoney;

  accounts: LiabilityAccount[] = [];
  page = 1;
  totalPages = 0;
  search = '';
  status: LiabilityAccountStatus | 'all' = 1;
  sortBy = 'Name';
  sortDirection = 'Asc';
  error = '';
  toast = '';
  expandedId: string | null = null;
  entries: PagedResult<LiabilityAccountEntry> = this.emptyPage();
  statuses: LiabilityAccountStatusEntry[] = [];
  entryPage = 1;

  accountModal = false;
  editingAccount: LiabilityAccount | null = null;
  accountForm = { name: '', description: '', creationDate: isoToday() };
  accountErrors: Record<string, string> = {};
  accountValid = false;

  statusModal = false;
  activeAccount: LiabilityAccount | null = null;
  statusForm = { status: 1 as LiabilityAccountStatus, effectiveDate: isoToday() };
  statusErrors: Record<string, string> = {};
  statusValid = false;

  entryModal = false;
  editingEntry: LiabilityAccountEntry | null = null;
  entryForm = { type: 1 as LiabilityAccountTxType, date: isoToday(), note: '', amount: 0 };
  entryErrors: Record<string, string> = {};
  entryValid = false;

  statsModal = false;
  statsAccount: LiabilityAccount | null = null;
  statsDuration = 'OneMonth';
  stats: LiabilityAccountStatistics | null = null;

  exportModal = false;
  selectedExportIds = new Set<string>();
  exportForm = { from: isoOffset(-30), to: isoToday(), format: 'Xlsx' as ReportFormat };
  deleteTarget: { kind: 'account' | 'entry'; account: LiabilityAccount; entry?: LiabilityAccountEntry } | null = null;
  deleteText = '';

  get title() { return this.mode === 'loans' ? 'Loans' : this.mode === 'debts' ? 'Debts' : 'Credit Cards'; }
  get singular() { return this.mode === 'loans' ? 'Loan' : this.mode === 'debts' ? 'Debt' : 'Credit Card'; }
  get subtitle() { return this.mode === 'loans' ? 'Manage education, personal, home, and other loans.' : this.mode === 'debts' ? 'Track outstanding debts and payments.' : 'Track credit card standing balances and bill payments.'; }
  get minNameLength() { return this.mode === 'loans' ? 3 : 2; }

  ngOnChanges() { this.page = 1; this.status = 1; this.expandedId = null; this.load(); }

  load() {
    this.api.listLiabilityAccounts(this.mode, { search: this.search, status: this.status, sortBy: this.sortBy, sortDirection: this.sortDirection, page: this.page }).subscribe({
      next: r => { this.accounts = r.items; this.totalPages = r.totalPages; this.error = ''; },
      error: e => this.error = this.readError(e)
    });
  }

  toggle(account: LiabilityAccount) {
    this.expandedId = this.expandedId === account.id ? null : account.id;
    this.entryPage = 1;
    if (this.expandedId) { this.loadEntries(account); this.loadStatuses(account); }
  }

  loadEntries(account: LiabilityAccount) {
    this.api.listLiabilityAccountEntries(this.mode, account.id, this.entryPage).subscribe({ next: r => this.entries = r, error: e => this.error = this.readError(e) });
  }
  loadStatuses(account: LiabilityAccount) {
    this.api.listLiabilityAccountStatuses(this.mode, account.id).subscribe({ next: r => this.statuses = r, error: e => this.error = this.readError(e) });
  }

  openAccount(account?: LiabilityAccount) {
    this.editingAccount = account ?? null;
    this.accountForm = account ? { name: account.name, description: account.description ?? '', creationDate: account.creationDate } : { name: '', description: '', creationDate: isoToday() };
    this.accountModal = true;
    this.validateAccount();
  }
  validateAccount() {
    const e: Record<string, string> = {};
    const name = this.accountForm.name.trim();
    if (name.length < this.minNameLength || name.length > 100) e['name'] = `Name must be ${this.minNameLength} to 100 characters.`;
    if (this.accountForm.description.trim().length > 500) e['description'] = 'Description must be at most 500 characters.';
    if (!this.accountForm.creationDate || this.accountForm.creationDate > isoToday()) e['creationDate'] = 'Creation date is mandatory and cannot be in the future.';
    this.accountErrors = e;
    this.accountValid = Object.keys(e).length === 0;
  }
  saveAccount() {
    if (!this.accountValid) return;
    const body = { name: this.accountForm.name.trim(), description: this.accountForm.description.trim() || null, creationDate: this.accountForm.creationDate };
    const req = this.editingAccount ? this.api.updateLiabilityAccount(this.mode, this.editingAccount.id, body) : this.api.createLiabilityAccount(this.mode, body);
    req.subscribe({ next: () => { this.closeModals(); this.toast = `${this.singular} saved.`; this.load(); }, error: e => this.error = this.readError(e) });
  }

  openStatus(account: LiabilityAccount) {
    this.activeAccount = account;
    this.statusForm = { status: account.status, effectiveDate: isoToday() };
    this.statusModal = true;
    this.validateStatus();
  }
  validateStatus() {
    const e: Record<string, string> = {};
    if (!this.statusForm.effectiveDate || this.statusForm.effectiveDate > isoToday()) e['effectiveDate'] = 'Effective date is mandatory and cannot be in the future.';
    if (this.activeAccount && this.statusForm.effectiveDate < this.activeAccount.creationDate) e['effectiveDate'] = 'Effective date cannot be before creation date.';
    if (this.activeAccount && this.statusForm.status === 2 && this.activeAccount.standingAmount !== 0) e['status'] = 'Inactive is allowed only when standing amount is zero.';
    this.statusErrors = e;
    this.statusValid = Object.keys(e).length === 0;
  }
  saveStatus() {
    if (!this.activeAccount || !this.statusValid) return;
    this.api.changeLiabilityAccountStatus(this.mode, this.activeAccount.id, this.statusForm).subscribe({
      next: () => { const a = this.activeAccount!; this.closeModals(); this.toast = 'Status saved.'; this.load(); if (this.expandedId === a.id) this.loadStatuses(a); },
      error: e => this.error = this.readError(e)
    });
  }

  openEntry(account: LiabilityAccount, entry?: LiabilityAccountEntry) {
    this.activeAccount = account;
    this.editingEntry = entry ?? null;
    this.entryForm = entry ? { type: entry.type, date: entry.date, note: entry.note ?? '', amount: entry.amount } : { type: 1, date: isoToday(), note: '', amount: 0 };
    this.entryModal = true;
    this.validateEntry();
  }
  validateEntry() {
    const e: Record<string, string> = {};
    if (!this.entryForm.date || this.entryForm.date > isoToday()) e['date'] = 'Date is mandatory and cannot be in the future.';
    if (this.activeAccount && this.entryForm.date < this.activeAccount.creationDate) e['date'] = 'Date cannot be before creation date.';
    if (this.entryForm.note.trim().length > 200) e['note'] = 'Note must be at most 200 characters.';
    if (Number(this.entryForm.amount) <= 0) e['amount'] = 'Amount must be greater than zero.';
    this.entryErrors = e;
    this.entryValid = Object.keys(e).length === 0;
  }
  saveEntry() {
    if (!this.activeAccount || !this.entryValid) return;
    const body = { type: this.entryForm.type, date: this.entryForm.date, note: this.entryForm.note.trim() || null, amount: Number(this.entryForm.amount) };
    const req = this.editingEntry ? this.api.updateLiabilityAccountEntry(this.mode, this.activeAccount.id, this.editingEntry.id, body) : this.api.addLiabilityAccountEntry(this.mode, this.activeAccount.id, body);
    req.subscribe({ next: () => { const a = this.activeAccount!; this.closeModals(); this.toast = 'Entry saved.'; this.load(); this.expandedId = a.id; this.loadEntries(a); }, error: e => this.error = this.readError(e) });
  }

  openStats(account: LiabilityAccount) { this.statsModal = true; this.statsAccount = account; this.statsDuration = 'OneMonth'; this.reloadStats(); }
  reloadStats() {
    if (!this.statsAccount) return;
    this.api.getLiabilityAccountStatistics(this.mode, this.statsAccount.id, this.statsDuration).subscribe({ next: r => this.stats = r, error: e => this.error = this.readError(e) });
  }

  openExport() { this.exportModal = true; this.selectedExportIds = new Set(this.accounts.map(x => x.id)); this.exportForm = { from: isoOffset(-30), to: isoToday(), format: 'Xlsx' }; }
  selectAllExport() { this.selectedExportIds = new Set(this.accounts.map(x => x.id)); }
  toggleExport(id: string) { this.selectedExportIds.has(id) ? this.selectedExportIds.delete(id) : this.selectedExportIds.add(id); }
  doExport() {
    if (this.exportForm.to < this.exportForm.from) { this.error = 'Export end date must be on or after start date.'; return; }
    this.api.exportLiabilityAccounts(this.mode, { liabilityAccountIds: [...this.selectedExportIds], ...this.exportForm }).subscribe({ next: r => { this.download(r); this.closeModals(); }, error: e => this.error = this.readError(e) });
  }

  confirmDelete(kind: 'account' | 'entry', account: LiabilityAccount, entry?: LiabilityAccountEntry) {
    this.deleteTarget = { kind, account, entry };
    this.deleteText = kind === 'account' ? `Delete ${account.name}?` : 'Delete this history entry?';
  }
  performDelete() {
    if (!this.deleteTarget) return;
    const t = this.deleteTarget;
    const req = t.kind === 'account' ? this.api.deleteLiabilityAccount(this.mode, t.account.id) : this.api.deleteLiabilityAccountEntry(this.mode, t.account.id, t.entry!.id);
    req.subscribe({ next: () => { this.deleteTarget = null; this.toast = 'Deleted.'; this.load(); if (t.kind === 'entry') this.loadEntries(t.account); }, error: e => this.error = this.readError(e) });
  }

  closeModals() {
    this.accountModal = this.statusModal = this.entryModal = this.statsModal = this.exportModal = false;
    this.editingAccount = null; this.activeAccount = null; this.editingEntry = null; this.stats = null;
  }

  private emptyPage<T>(): PagedResult<T> { return { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 }; }
  private readError(e: any) { return e?.error?.detail || e?.error?.message || e?.message || 'Something went wrong.'; }
  private download(response: HttpResponse<Blob>) {
    const blob = response.body;
    if (!blob) return;
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${this.mode}.${this.exportForm.format === 'Pdf' ? 'pdf' : 'xlsx'}`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
