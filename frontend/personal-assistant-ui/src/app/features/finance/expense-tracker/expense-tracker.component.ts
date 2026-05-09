import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { FinanceApi } from '../finance.api';
import {
  Account,
  Category,
  PaymentType,
  Tag,
  Transaction,
  TransactionType,
  TRANSACTION_TYPES,
  fmtMoney
} from '../finance.models';
import { ReportFormat } from '../finance.models';

interface TxForm {
  id?: string;
  date: string;
  type: TransactionType;
  accountId: string;
  amount: number;
  reason: string;
  note: string;
  categoryId: string;
  paymentTypeId: string;
  tagIds: Set<string>;
}

interface TransferForm {
  date: string;
  sourceAccountId: string;
  destinationAccountId: string;
  amount: number;
  reason: string;
  note: string;
  paymentTypeId: string;
  tagIds: Set<string>;
}

interface Filters {
  from: string;
  to: string;
  accountId: string;
  categoryId: string;
  paymentTypeId: string;
  tagId: string;
  type: '' | '1' | '2';
  search: string;
}

function todayIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}
function isoOffset(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

@Component({
  selector: 'app-expense-tracker',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="exp-shell">
      <!-- Top action bar -->
      <div class="d-flex flex-wrap gap-2 align-items-center mb-3">
        <h2 class="m-0" style="font-size: 1.05rem; font-weight: 600;">Transactions</h2>
        <div class="ms-auto d-flex gap-2">
          <button class="btn-link-soft btn-sm" type="button" (click)="showingReports.update(v => !v)">
            {{ showingReports() ? 'Hide Reports' : 'Reports' }}
          </button>
          <button class="btn-link-soft btn-sm" type="button" (click)="openTransfer()">+ Self-Transfer</button>
          <button class="btn-neon btn-sm" type="button" (click)="openCreate()">+ Add Transaction</button>
        </div>
      </div>

      <!-- Filters -->
      <div class="surface p-3 mb-3 filters">
        <div class="row g-2 align-items-end">
          <div class="col-6 col-md-2">
            <label class="form-label small">From</label>
            <input type="date" class="form-control form-control-sm" [(ngModel)]="filters.from" (change)="applyFilters()" />
          </div>
          <div class="col-6 col-md-2">
            <label class="form-label small">To</label>
            <input type="date" class="form-control form-control-sm" [(ngModel)]="filters.to" (change)="applyFilters()" />
          </div>
          <div class="col-6 col-md-2">
            <label class="form-label small">Account</label>
            <select class="form-select form-select-sm" [(ngModel)]="filters.accountId" (change)="applyFilters()">
              <option value="">All</option>
              @for (a of accounts(); track a.id) { <option [value]="a.id">{{ a.name }}</option> }
            </select>
          </div>
          <div class="col-6 col-md-2">
            <label class="form-label small">Category</label>
            <select class="form-select form-select-sm" [(ngModel)]="filters.categoryId" (change)="applyFilters()">
              <option value="">All</option>
              @for (c of categories(); track c.id) { <option [value]="c.id">{{ c.name }}</option> }
            </select>
          </div>
          <div class="col-6 col-md-2">
            <label class="form-label small">Payment</label>
            <select class="form-select form-select-sm" [(ngModel)]="filters.paymentTypeId" (change)="applyFilters()">
              <option value="">All</option>
              @for (p of paymentTypes(); track p.id) { <option [value]="p.id">{{ p.name }}</option> }
            </select>
          </div>
          <div class="col-6 col-md-2">
            <label class="form-label small">Tag</label>
            <select class="form-select form-select-sm" [(ngModel)]="filters.tagId" (change)="applyFilters()">
              <option value="">All</option>
              @for (t of tags(); track t.id) { <option [value]="t.id">{{ t.name }}</option> }
            </select>
          </div>
          <div class="col-6 col-md-2">
            <label class="form-label small">Type</label>
            <select class="form-select form-select-sm" [(ngModel)]="filters.type" (change)="applyFilters()">
              <option value="">All</option>
              <option value="1">Credit</option>
              <option value="2">Debit</option>
            </select>
          </div>
          <div class="col-12 col-md-4">
            <label class="form-label small">Search reason / note</label>
            <input class="form-control form-control-sm" [(ngModel)]="filters.search" (keyup.enter)="applyFilters()" placeholder="press Enter" />
          </div>
          <div class="col-12 col-md-2 d-grid">
            <button class="btn-link-soft btn-sm" type="button" (click)="resetFilters()">Reset</button>
          </div>
        </div>
      </div>

      <!-- Reports -->
      @if (showingReports()) {
        <div class="surface p-3 mb-3">
          <div class="d-flex flex-wrap align-items-end gap-2">
            <div>
              <label class="form-label small">From</label>
              <input type="date" class="form-control form-control-sm" [(ngModel)]="reportFrom" />
            </div>
            <div>
              <label class="form-label small">To</label>
              <input type="date" class="form-control form-control-sm" [(ngModel)]="reportTo" />
            </div>
            <div>
              <label class="form-label small">Account</label>
              <select class="form-select form-select-sm" [(ngModel)]="reportAccountId">
                <option value="">All accounts</option>
                @for (a of accounts(); track a.id) { <option [value]="a.id">{{ a.name }}</option> }
              </select>
            </div>
            <div class="ms-auto d-flex gap-2">
              <button class="btn-link-soft btn-sm" type="button" (click)="downloadReport('Csv')">CSV</button>
              <button class="btn-link-soft btn-sm" type="button" (click)="downloadReport('Xlsx')">Excel</button>
              <button class="btn-link-soft btn-sm" type="button" (click)="downloadReport('Pdf')">PDF</button>
            </div>
          </div>
          @if (reportError()) { <div class="alert alert-danger py-1 px-2 small mb-0 mt-2">{{ reportError() }}</div> }
        </div>
      }

      <!-- Transaction form -->
      @if (txForm(); as f) {
        <div class="surface p-3 mb-3">
          <div class="row g-2 align-items-end">
            <div class="col-6 col-md-2">
              <label class="form-label small">Date</label>
              <input type="date" class="form-control form-control-sm" [(ngModel)]="f.date" />
            </div>
            <div class="col-6 col-md-2">
              <label class="form-label small">Type</label>
              <select class="form-select form-select-sm" [(ngModel)]="f.type">
                <option [ngValue]="1">Credit</option>
                <option [ngValue]="2">Debit</option>
              </select>
            </div>
            <div class="col-6 col-md-2">
              <label class="form-label small">Account</label>
              <select class="form-select form-select-sm" [(ngModel)]="f.accountId">
                <option value="">—</option>
                @for (a of accounts(); track a.id) { <option [value]="a.id">{{ a.name }}</option> }
              </select>
            </div>
            <div class="col-6 col-md-2">
              <label class="form-label small">Amount</label>
              <input type="number" min="0" step="0.01" class="form-control form-control-sm" [(ngModel)]="f.amount" />
            </div>
            <div class="col-12 col-md-4">
              <label class="form-label small">Reason</label>
              <input class="form-control form-control-sm" [(ngModel)]="f.reason" />
            </div>
            <div class="col-12 col-md-4">
              <label class="form-label small">Note</label>
              <input class="form-control form-control-sm" [(ngModel)]="f.note" />
            </div>
            <div class="col-6 col-md-2">
              <label class="form-label small">Category</label>
              <select class="form-select form-select-sm" [(ngModel)]="f.categoryId">
                <option value="">—</option>
                @for (c of categories(); track c.id) { <option [value]="c.id">{{ c.name }}</option> }
              </select>
            </div>
            <div class="col-6 col-md-2">
              <label class="form-label small">Payment Type</label>
              <select class="form-select form-select-sm" [(ngModel)]="f.paymentTypeId">
                <option value="">—</option>
                @for (p of paymentTypes(); track p.id) { <option [value]="p.id">{{ p.name }}</option> }
              </select>
            </div>
            <div class="col-12 col-md-4 d-grid gap-1">
              <button class="btn-neon btn-sm" type="button" [disabled]="!isFormValid(f) || saving()" (click)="saveTransaction()">{{ f.id ? 'Save Changes' : 'Create' }}</button>
              <button class="btn-link-soft btn-sm" type="button" (click)="txForm.set(null)">Cancel</button>
            </div>
            <div class="col-12">
              <label class="form-label small">Tags</label>
              <div class="tag-picker">
                @for (t of tags(); track t.id) {
                  <button type="button" class="tag-chip"
                          [class.is-selected]="f.tagIds.has(t.id)"
                          [style.color]="f.tagIds.has(t.id) ? t.color : null"
                          [style.borderColor]="f.tagIds.has(t.id) ? t.color : null"
                          (click)="toggleTag(f, t.id)">{{ t.name }}</button>
                }
              </div>
            </div>
          </div>
          @if (formError()) { <div class="alert alert-danger py-1 px-2 small mb-0 mt-2">{{ formError() }}</div> }
        </div>
      }

      <!-- Self-Transfer form -->
      @if (transferForm(); as f) {
        <div class="surface p-3 mb-3 neon-cyan-border">
          <div class="row g-2 align-items-end">
            <div class="col-6 col-md-2">
              <label class="form-label small">Date</label>
              <input type="date" class="form-control form-control-sm" [(ngModel)]="f.date" />
            </div>
            <div class="col-6 col-md-2">
              <label class="form-label small">Amount</label>
              <input type="number" min="0" step="0.01" class="form-control form-control-sm" [(ngModel)]="f.amount" />
            </div>
            <div class="col-6 col-md-2">
              <label class="form-label small">From Account</label>
              <select class="form-select form-select-sm" [(ngModel)]="f.sourceAccountId">
                <option value="">—</option>
                @for (a of accounts(); track a.id) { <option [value]="a.id">{{ a.name }}</option> }
              </select>
            </div>
            <div class="col-6 col-md-2">
              <label class="form-label small">To Account</label>
              <select class="form-select form-select-sm" [(ngModel)]="f.destinationAccountId">
                <option value="">—</option>
                @for (a of accounts(); track a.id) { <option [value]="a.id">{{ a.name }}</option> }
              </select>
            </div>
            <div class="col-12 col-md-4">
              <label class="form-label small">Reason</label>
              <input class="form-control form-control-sm" [(ngModel)]="f.reason" placeholder="e.g. Top up wallet" />
            </div>
            <div class="col-12 col-md-6">
              <label class="form-label small">Note</label>
              <input class="form-control form-control-sm" [(ngModel)]="f.note" />
            </div>
            <div class="col-6 col-md-3">
              <label class="form-label small">Payment Type</label>
              <select class="form-select form-select-sm" [(ngModel)]="f.paymentTypeId">
                <option value="">—</option>
                @for (p of paymentTypes(); track p.id) { <option [value]="p.id">{{ p.name }}</option> }
              </select>
            </div>
            <div class="col-6 col-md-3 d-grid gap-1">
              <button class="btn-neon btn-sm" type="button" [disabled]="!isTransferValid(f) || saving()" (click)="saveTransfer()">Create</button>
              <button class="btn-link-soft btn-sm" type="button" (click)="transferForm.set(null)">Cancel</button>
            </div>
          </div>
          @if (formError()) { <div class="alert alert-danger py-1 px-2 small mb-0 mt-2">{{ formError() }}</div> }
        </div>
      }

      <!-- Transactions table -->
      @if (loading()) { <div class="text-muted-soft small">Loading…</div> }
      @else if (page().items.length === 0) {
        <div class="surface p-4 text-center text-muted-soft">No transactions for these filters.</div>
      } @else {
        <div class="table-wrap surface">
          <table class="tx-table">
            <thead>
              <tr>
                <th>Date</th><th>Type</th><th>Account</th>
                <th class="text-end">Amount</th>
                <th>Reason / Note</th>
                <th>Category</th><th>Payment</th><th>Tags</th>
                @if (filters.accountId) { <th class="text-end">Standing</th> }
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (t of page().items; track t.id) {
                <tr [class.is-transfer]="t.transferGroupId">
                  <td>{{ t.date }}</td>
                  <td>
                    <span class="type-pill" [ngClass]="t.type === 1 ? 'tone-green' : 'tone-red'">
                      {{ t.type === 1 ? 'Credit' : 'Debit' }}
                    </span>
                    @if (t.transferGroupId) { <span class="transfer-mark" title="Self transfer leg">⇄</span> }
                  </td>
                  <td>{{ t.accountName }}</td>
                  <td class="text-end" [class.amount-credit]="t.type === 1" [class.amount-debit]="t.type === 2">
                    {{ t.type === 1 ? '+' : '−' }} {{ fmtMoney(t.amount) }}
                  </td>
                  <td>
                    <div>{{ t.reason }}</div>
                    @if (t.note) { <div class="small text-muted-soft">{{ t.note }}</div> }
                  </td>
                  <td>{{ t.categoryName ?? '—' }}</td>
                  <td>{{ t.paymentTypeName ?? '—' }}</td>
                  <td>
                    @for (tag of t.tags; track tag.id) {
                      <span class="tag-pill" [style.color]="tag.color" [style.borderColor]="tag.color">{{ tag.name }}</span>
                    }
                  </td>
                  @if (filters.accountId) {
                    <td class="text-end">{{ fmtMoney(t.accountStanding) }}</td>
                  }
                  <td class="actions">
                    <button class="icon-btn" type="button" title="Edit" (click)="startEdit(t)">✎</button>
                    <button class="icon-btn icon-danger" type="button" title="Delete" (click)="remove(t)">🗑</button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <div class="d-flex justify-content-between align-items-center mt-2 small text-muted-soft">
          <div>
            Page {{ page().page }} of {{ totalPages() }} · {{ page().totalCount }} total
          </div>
          <div class="d-flex gap-2">
            <button class="btn-link-soft btn-sm" type="button" [disabled]="page().page <= 1" (click)="goto(page().page - 1)">‹ Prev</button>
            <button class="btn-link-soft btn-sm" type="button" [disabled]="page().page >= totalPages()" (click)="goto(page().page + 1)">Next ›</button>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .filters .form-label { margin-bottom: 0.15rem; }

    .table-wrap { overflow: auto; border: 1px solid var(--border); border-radius: var(--radius-md); }
    .tx-table { width: 100%; border-collapse: collapse; font-size: 0.88rem; }
    .tx-table thead th {
      position: sticky; top: 0;
      background: var(--surface-2); color: var(--fg-muted);
      font-weight: 600; text-align: left;
      padding: 0.5rem 0.75rem;
      border-bottom: 1px solid var(--border-strong);
      white-space: nowrap;
    }
    .tx-table tbody td { padding: 0.5rem 0.75rem; border-bottom: 1px solid var(--border); vertical-align: top; }
    .tx-table tbody tr:hover td { background: var(--surface-2); }
    .tx-table tr.is-transfer td { background: rgba(8, 253, 216, 0.04); }

    .type-pill { display: inline-flex; padding: 0.1rem 0.5rem; border-radius: 999px; font-size: 0.72rem; border: 1px solid var(--border-strong); }
    .tone-green { color: var(--success); border-color: var(--success); }
    .tone-red { color: var(--danger); border-color: var(--danger); }

    .transfer-mark { color: var(--neon-cyan); margin-left: 0.3rem; }

    .amount-credit { color: var(--success); font-weight: 600; }
    .amount-debit { color: var(--danger); font-weight: 600; }

    .tag-pill {
      display: inline-flex;
      padding: 0.1rem 0.5rem;
      margin-right: 0.25rem;
      border-radius: 999px;
      font-size: 0.7rem;
      border: 1px solid;
      font-weight: 500;
    }

    .tag-picker {
      display: flex; gap: 0.4rem; flex-wrap: wrap;
      padding: 0.4rem;
      border: 1px solid var(--border);
      border-radius: var(--radius-sm);
      background: var(--surface-2);
    }
    .tag-chip {
      padding: 0.25rem 0.7rem;
      border-radius: 999px;
      font-size: 0.78rem;
      background: transparent;
      color: var(--fg-muted);
      border: 1px solid var(--border-strong);
      cursor: pointer;
    }
    .tag-chip.is-selected { font-weight: 500; }

    .neon-cyan-border { border: 1px solid var(--neon-cyan); box-shadow: 0 0 12px var(--neon-cyan-soft); }

    .icon-btn { width: 28px; height: 28px; border-radius: var(--radius-sm); background: transparent; border: 1px solid transparent; color: var(--fg-muted); cursor: pointer; transition: all 120ms ease; font-size: 0.9rem; }
    .icon-btn:hover { color: var(--fg); border-color: var(--border-strong); }
    .icon-danger:hover { color: var(--danger); border-color: var(--danger); }
    .actions { white-space: nowrap; }

    .btn-link-soft { background: transparent; border: 1px solid var(--border-strong); color: var(--fg-muted); padding: 0.3rem 0.7rem; border-radius: var(--radius-sm); cursor: pointer; transition: all 120ms ease; }
    .btn-link-soft:hover { color: var(--neon); border-color: var(--neon); }
  `]
})
export class ExpenseTrackerComponent {
  private readonly api = inject(FinanceApi);
  readonly fmtMoney = fmtMoney;
  readonly txTypes = TRANSACTION_TYPES;

  // Reference data
  readonly accounts = signal<Account[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly paymentTypes = signal<PaymentType[]>([]);
  readonly tags = signal<Tag[]>([]);

  // Page
  readonly page = signal<{ items: Transaction[]; page: number; pageSize: number; totalCount: number }>({
    items: [], page: 1, pageSize: 25, totalCount: 0
  });
  readonly loading = signal(false);
  readonly saving = signal(false);

  // Forms
  readonly txForm = signal<TxForm | null>(null);
  readonly transferForm = signal<TransferForm | null>(null);
  readonly formError = signal<string | null>(null);

  // Reports
  readonly showingReports = signal(false);
  reportFrom = isoOffset(-30);
  reportTo = isoOffset(0);
  reportAccountId = '';
  readonly reportError = signal<string | null>(null);

  filters: Filters = {
    from: '',
    to: '',
    accountId: '',
    categoryId: '',
    paymentTypeId: '',
    tagId: '',
    type: '',
    search: ''
  };

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.page().totalCount / this.page().pageSize)));

  constructor() {
    this.bootstrap();
  }

  private async bootstrap(): Promise<void> {
    const [a, c, p, t] = await Promise.all([
      firstValueFrom(this.api.getAccounts(true)),
      firstValueFrom(this.api.getCategories()),
      firstValueFrom(this.api.getPaymentTypes()),
      firstValueFrom(this.api.getTags())
    ]);
    this.accounts.set(a);
    this.categories.set(c);
    this.paymentTypes.set(p);
    this.tags.set(t);
    await this.refresh(1);
  }

  async refresh(toPage = this.page().page): Promise<void> {
    this.loading.set(true);
    try {
      const f = this.filters;
      const result = await firstValueFrom(this.api.listTransactions({
        from: f.from || undefined,
        to: f.to || undefined,
        accountId: f.accountId || undefined,
        categoryId: f.categoryId || undefined,
        paymentTypeId: f.paymentTypeId || undefined,
        tagId: f.tagId || undefined,
        type: f.type ? (Number(f.type) as TransactionType) : undefined,
        search: f.search || undefined,
        page: toPage,
        pageSize: 25
      }));
      this.page.set(result);
    } finally {
      this.loading.set(false);
    }
  }

  applyFilters(): void { this.refresh(1); }
  resetFilters(): void {
    this.filters = { from: '', to: '', accountId: '', categoryId: '', paymentTypeId: '', tagId: '', type: '', search: '' };
    this.refresh(1);
  }
  goto(page: number): void { this.refresh(page); }

  // ===== Transaction form =====
  openCreate(): void {
    this.formError.set(null);
    this.transferForm.set(null);
    this.txForm.set({
      date: todayIso(),
      type: 2,
      accountId: this.accounts()[0]?.id ?? '',
      amount: 0,
      reason: '',
      note: '',
      categoryId: '',
      paymentTypeId: '',
      tagIds: new Set()
    });
  }

  startEdit(t: Transaction): void {
    if (t.transferGroupId) {
      alert('Self-transfer transactions can only be deleted as a pair, not edited individually.');
      return;
    }
    this.formError.set(null);
    this.transferForm.set(null);
    this.txForm.set({
      id: t.id,
      date: t.date,
      type: t.type,
      accountId: t.accountId,
      amount: t.amount,
      reason: t.reason,
      note: t.note ?? '',
      categoryId: t.categoryId ?? '',
      paymentTypeId: t.paymentTypeId ?? '',
      tagIds: new Set(t.tags.map(x => x.id))
    });
  }

  toggleTag(f: TxForm | TransferForm, tagId: string): void {
    if (f.tagIds.has(tagId)) f.tagIds.delete(tagId);
    else f.tagIds.add(tagId);
  }

  isFormValid(f: TxForm): boolean {
    return !!(f.accountId && f.reason.trim() && f.amount > 0);
  }

  async saveTransaction(): Promise<void> {
    const f = this.txForm();
    if (!f || !this.isFormValid(f)) return;
    this.saving.set(true);
    this.formError.set(null);
    try {
      const body = {
        date: f.date,
        type: f.type,
        accountId: f.accountId,
        amount: Number(f.amount),
        reason: f.reason.trim(),
        note: f.note.trim() || null,
        categoryId: f.categoryId || null,
        paymentTypeId: f.paymentTypeId || null,
        tagIds: Array.from(f.tagIds)
      };
      if (f.id) await firstValueFrom(this.api.updateTransaction(f.id, body));
      else await firstValueFrom(this.api.createTransaction(body));
      this.txForm.set(null);
      await this.refresh();
    } catch (err: any) {
      this.formError.set(err?.error?.message ?? 'Save failed.');
    } finally {
      this.saving.set(false);
    }
  }

  async remove(t: Transaction): Promise<void> {
    const msg = t.transferGroupId
      ? 'This is one leg of a self-transfer. Both legs will be deleted. Continue?'
      : `Delete transaction "${t.reason}"?`;
    if (!confirm(msg)) return;
    try { await firstValueFrom(this.api.deleteTransaction(t.id)); await this.refresh(); }
    catch (err: any) { alert(err?.error?.message ?? 'Delete failed.'); }
  }

  // ===== Self-transfer =====
  openTransfer(): void {
    this.formError.set(null);
    this.txForm.set(null);
    const accts = this.accounts();
    this.transferForm.set({
      date: todayIso(),
      sourceAccountId: accts[0]?.id ?? '',
      destinationAccountId: accts[1]?.id ?? '',
      amount: 0,
      reason: 'Self transfer',
      note: '',
      paymentTypeId: '',
      tagIds: new Set()
    });
  }

  isTransferValid(f: TransferForm): boolean {
    return !!(f.sourceAccountId && f.destinationAccountId
      && f.sourceAccountId !== f.destinationAccountId
      && f.amount > 0
      && f.reason.trim());
  }

  async saveTransfer(): Promise<void> {
    const f = this.transferForm();
    if (!f || !this.isTransferValid(f)) return;
    this.saving.set(true);
    this.formError.set(null);
    try {
      await firstValueFrom(this.api.createTransfer({
        date: f.date,
        sourceAccountId: f.sourceAccountId,
        destinationAccountId: f.destinationAccountId,
        amount: Number(f.amount),
        reason: f.reason.trim(),
        note: f.note.trim() || null,
        paymentTypeId: f.paymentTypeId || null,
        tagIds: Array.from(f.tagIds)
      }));
      this.transferForm.set(null);
      await this.refresh();
    } catch (err: any) {
      this.formError.set(err?.error?.message ?? 'Transfer failed.');
    } finally {
      this.saving.set(false);
    }
  }

  // ===== Reports =====
  async downloadReport(format: ReportFormat): Promise<void> {
    if (!this.reportFrom || !this.reportTo) {
      this.reportError.set('Pick a date range.');
      return;
    }
    this.reportError.set(null);
    try {
      const res = await firstValueFrom(this.api.downloadTransactionsReport(
        this.reportFrom, this.reportTo,
        this.reportAccountId || null, format));
      const blob = res.body!;
      const filename = parseFilename(res.headers.get('content-disposition'))
        ?? `transactions-${this.reportFrom}-to-${this.reportTo}.${format.toLowerCase()}`;
      triggerDownload(blob, filename);
    } catch {
      this.reportError.set('Download failed.');
    }
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
