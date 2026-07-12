import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { FinanceApi } from '../finance.api';
import { Budget, BudgetEntry, BudgetReport, Category, ReportFormat, fmtMoney } from '../finance.models';

interface BudgetAllocationForm {
  categoryId: string;
  amount: number;
}

interface BudgetForm {
  id?: string;
  name: string;
  entries: BudgetAllocationForm[];
  from: string;
  to: string;
  note: string;
}

function firstOfMonth(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-01`;
}

function lastOfMonth(): string {
  const d = new Date();
  const last = new Date(d.getFullYear(), d.getMonth() + 1, 0);
  return `${last.getFullYear()}-${String(last.getMonth() + 1).padStart(2, '0')}-${String(last.getDate()).padStart(2, '0')}`;
}

@Component({
  selector: 'app-finance-budgets',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="budgets-shell">
      <div class="d-flex justify-content-between align-items-center mb-3">
        <p class="text-muted-soft small mb-0">Allocate budgets across categories and date ranges. Spent is computed automatically from your expenses.</p>
        <button class="btn-neon btn-sm" type="button" (click)="openCreate()">{{ form() ? 'Cancel' : '+ New Budget' }}</button>
      </div>

      @if (form(); as f) {
        <div class="surface p-3 mb-3">
          <div class="row g-2 align-items-end">
            <div class="col-12 col-md-4">
              <label class="form-label small">Name *</label>
              <input class="form-control form-control-sm" [(ngModel)]="f.name" />
            </div>
            <div class="col-6 col-md-2">
              <label class="form-label small">From *</label>
              <input type="date" class="form-control form-control-sm" [(ngModel)]="f.from" />
            </div>
            <div class="col-6 col-md-2">
              <label class="form-label small">To *</label>
              <input type="date" class="form-control form-control-sm" [(ngModel)]="f.to" />
            </div>
            <div class="col-12 col-md-4">
              <label class="form-label small">Note</label>
              <input class="form-control form-control-sm" [(ngModel)]="f.note" />
            </div>
          </div>

          <div class="allocation-box mt-3">
            <div class="d-flex justify-content-between align-items-center mb-2">
              <div class="small text-muted-soft">Category allocations *</div>
              <button class="btn-link-soft btn-sm" type="button" (click)="addAllocation(f)">+ Category</button>
            </div>
            @for (entry of f.entries; track $index) {
              <div class="allocation-row">
                <select class="form-select form-select-sm" [(ngModel)]="entry.categoryId">
                  <option value="">Select category</option>
                  @for (c of categories(); track c.id) { <option [value]="c.id">{{ c.name }}</option> }
                </select>
                <input type="number" min="0" step="0.01" class="form-control form-control-sm" placeholder="Amount" [(ngModel)]="entry.amount" />
                <button class="icon-btn icon-danger" type="button" title="Remove" (click)="removeAllocation(f, $index)">X</button>
              </div>
            }
            <div class="small text-muted-soft mt-2">Total allocation: {{ fmtMoney(totalAmount(f.entries)) }}</div>
          </div>

          @if (formError()) { <div class="alert alert-danger py-1 px-2 small mb-0 mt-2">{{ formError() }}</div> }
          <div class="d-flex gap-2 justify-content-end mt-3">
            <button class="btn-link-soft btn-sm" type="button" (click)="form.set(null)">Cancel</button>
            <button class="btn-neon btn-sm" type="button" [disabled]="saving()" (click)="save()">{{ f.id ? 'Save Changes' : 'Create Budget' }}</button>
          </div>
        </div>
      }

      @if (loading()) { <div class="text-muted-soft small">Loading...</div> }
      @else if (budgets().length === 0) {
        <div class="surface p-4 text-center text-muted-soft">No budgets yet.</div>
      } @else {
        <div class="row g-3">
          @for (b of budgets(); track b.id) {
            <div class="col-12 col-md-6 col-xl-4">
              <article class="budget-card neon">
                <header class="d-flex align-items-start gap-2 mb-2">
                  <div class="flex-grow-1">
                    <h3 class="bud-title">{{ b.name }}</h3>
                    <div class="bud-meta small text-muted-soft">{{ b.categoryName }} | {{ b.from }} to {{ b.to }}</div>
                  </div>
                  <div class="bud-actions">
                    <button class="icon-btn" type="button" title="Edit" (click)="startEdit(b)">Edit</button>
                    <button class="icon-btn icon-danger" type="button" title="Delete" (click)="remove(b)">Del</button>
                  </div>
                </header>

                <div class="bud-numbers">
                  <div><div class="muted">Allocated</div><div class="big">{{ fmtMoney(b.amount) }}</div></div>
                  <div><div class="muted">Spent</div><div class="big tone-red">{{ fmtMoney(b.spent) }}</div></div>
                  <div><div class="muted">Remaining</div><div class="big" [ngClass]="b.remaining >= 0 ? 'tone-green' : 'tone-red'">{{ fmtMoney(b.remaining) }}</div></div>
                </div>

                <div class="progress-wrap" [class.over]="b.percentUsed > 100">
                  <div class="progress-bar" [style.width.%]="b.percentUsed > 100 ? 100 : b.percentUsed"></div>
                </div>
                <div class="d-flex justify-content-between small text-muted-soft mt-1">
                  <span>{{ b.percentUsed }}% used</span>
                  @if (b.percentUsed > 100) { <span class="tone-red">Over budget</span> }
                </div>

                @if (allocations(b).length > 1) {
                  <div class="allocation-summary mt-2">
                    @for (entry of allocations(b); track entry.categoryId) {
                      <div class="allocation-mini">
                        <span>{{ entry.categoryName }}</span>
                        <span>{{ fmtMoney(entry.spent) }} / {{ fmtMoney(entry.amount) }}</span>
                      </div>
                    }
                  </div>
                }

                <div class="d-flex gap-2 mt-2 flex-wrap">
                  <button class="btn-link-soft btn-sm" type="button" (click)="toggleReport(b)">
                    {{ openReportId() === b.id ? 'Hide details' : 'View details' }}
                  </button>
                  <button class="btn-link-soft btn-sm" type="button" (click)="downloadReport(b, 'Csv')">CSV</button>
                  <button class="btn-link-soft btn-sm" type="button" (click)="downloadReport(b, 'Xlsx')">Excel</button>
                  <button class="btn-link-soft btn-sm" type="button" (click)="downloadReport(b, 'Pdf')">PDF</button>
                </div>

                @if (openReportId() === b.id) {
                  @if (loadingReport()) { <div class="small text-muted-soft mt-2">Loading...</div> }
                  @else if (report(); as r) {
                    <div class="mini-table mt-2">
                      <table>
                        <thead><tr><th>Date</th><th>Reason</th><th>Category</th><th>Account</th><th>Payment</th><th class="text-end">Amount</th></tr></thead>
                        <tbody>
                          @for (row of r.transactions; track $index) {
                            <tr>
                              <td>{{ row.date }}</td>
                              <td>{{ row.reason }}</td>
                              <td>{{ row.categoryName }}</td>
                              <td>{{ row.accountName }}</td>
                              <td>{{ row.paymentTypeName ?? '-' }}</td>
                              <td class="text-end">{{ fmtMoney(row.amount) }}</td>
                            </tr>
                          } @empty {
                            <tr><td colspan="6" class="text-muted-soft text-center py-2 small">No spend recorded for this budget yet.</td></tr>
                          }
                        </tbody>
                      </table>
                    </div>
                  }
                }
              </article>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .budget-card { padding: 1rem; }
    .bud-title { font-size: 1.05rem; font-weight: 600; margin: 0; }
    .bud-meta { margin-top: 0.1rem; }
    .bud-actions { display: flex; gap: 0.2rem; }
    .bud-numbers { display: grid; grid-template-columns: repeat(3, 1fr); gap: 0.5rem; margin-bottom: 0.65rem; }
    .bud-numbers .muted { font-size: 0.72rem; color: var(--fg-muted); text-transform: uppercase; letter-spacing: 0.05em; }
    .bud-numbers .big { font-size: 1.05rem; font-weight: 600; }
    .tone-green { color: var(--success); }
    .tone-red { color: var(--danger); }
    .progress-wrap { background: var(--surface-2); height: 8px; border-radius: 999px; overflow: hidden; }
    .progress-bar { height: 8px; background: linear-gradient(90deg, var(--success), var(--warning)); border-radius: 999px; transition: width 200ms ease; }
    .progress-wrap.over .progress-bar { background: var(--danger); }
    .allocation-box { border: 1px solid var(--border); border-radius: var(--radius-sm); padding: .75rem; }
    .allocation-row { display: grid; grid-template-columns: minmax(0, 1fr) 160px 36px; gap: .5rem; align-items: center; margin-bottom: .5rem; }
    .allocation-summary { border-top: 1px solid var(--border); padding-top: .55rem; display: grid; gap: .3rem; }
    .allocation-mini { display: flex; justify-content: space-between; gap: .75rem; font-size: .78rem; color: var(--fg-muted); }
    .mini-table { border: 1px solid var(--border); border-radius: var(--radius-sm); overflow: hidden; }
    .mini-table table { width: 100%; border-collapse: collapse; font-size: 0.82rem; }
    .mini-table thead th { background: var(--surface-2); color: var(--fg-muted); font-weight: 600; text-align: left; padding: 0.4rem 0.65rem; }
    .mini-table tbody td { padding: 0.4rem 0.65rem; border-top: 1px solid var(--border); }
    .icon-btn { min-width: 32px; height: 28px; border-radius: var(--radius-sm); background: transparent; border: 1px solid transparent; color: var(--fg-muted); cursor: pointer; transition: all 120ms ease; font-size: 0.75rem; }
    .icon-btn:hover { color: var(--fg); border-color: var(--border-strong); }
    .icon-danger:hover { color: var(--danger); border-color: var(--danger); }
    .btn-link-soft { background: transparent; border: 1px solid var(--border-strong); color: var(--fg-muted); padding: 0.3rem 0.7rem; border-radius: var(--radius-sm); cursor: pointer; transition: all 120ms ease; font-size: 0.78rem; }
    .btn-link-soft:hover { color: var(--neon); border-color: var(--neon); }
    @media (max-width: 575.98px) { .allocation-row { grid-template-columns: 1fr; } }
  `]
})
export class BudgetsComponent {
  private readonly api = inject(FinanceApi);
  readonly fmtMoney = fmtMoney;

  readonly budgets = signal<Budget[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly form = signal<BudgetForm | null>(null);
  readonly formError = signal<string | null>(null);
  readonly openReportId = signal<string | null>(null);
  readonly report = signal<BudgetReport | null>(null);
  readonly loadingReport = signal(false);

  constructor() { this.bootstrap(); }

  private async bootstrap(): Promise<void> {
    const cats = await firstValueFrom(this.api.getCategories());
    this.categories.set(cats);
    await this.refresh();
  }

  async refresh(): Promise<void> {
    this.loading.set(true);
    try { this.budgets.set(await firstValueFrom(this.api.listBudgets())); }
    finally { this.loading.set(false); }
  }

  openCreate(): void {
    this.formError.set(null);
    if (this.form()) { this.form.set(null); return; }
    this.form.set({ name: '', entries: [{ categoryId: '', amount: 0 }], from: firstOfMonth(), to: lastOfMonth(), note: '' });
  }

  startEdit(b: Budget): void {
    this.formError.set(null);
    const entries = this.allocations(b).map(e => ({ categoryId: e.categoryId, amount: e.amount }));
    this.form.set({ id: b.id, name: b.name, entries, from: b.from, to: b.to, note: b.note ?? '' });
  }

  addAllocation(f: BudgetForm): void { f.entries.push({ categoryId: '', amount: 0 }); }
  removeAllocation(f: BudgetForm, index: number): void {
    f.entries.splice(index, 1);
    if (f.entries.length === 0) f.entries.push({ categoryId: '', amount: 0 });
  }

  allocations(b: Budget): BudgetEntry[] {
    return b.entries?.length ? b.entries : [{ id: '', categoryId: b.categoryId, categoryName: b.categoryName, amount: b.amount, spent: b.spent, remaining: b.remaining, percentUsed: b.percentUsed }];
  }

  totalAmount(entries: BudgetAllocationForm[]): number {
    return entries.reduce((sum, e) => sum + Number(e.amount || 0), 0);
  }

  private validationError(f: BudgetForm): string | null {
    if (!f.name.trim()) return 'Name is required.';
    if (!f.from || !f.to) return 'From and To dates are required.';
    if (f.from > f.to) return 'To date must be on or after From date.';
    if (f.entries.some(e => !e.categoryId)) return 'Every allocation needs a category.';
    if (f.entries.some(e => Number(e.amount) <= 0)) return 'Every allocation amount must be positive.';
    const ids = f.entries.map(e => e.categoryId);
    if (new Set(ids).size !== ids.length) return 'Each category can appear only once in a budget.';
    return null;
  }

  async save(): Promise<void> {
    const f = this.form();
    if (!f) return;
    const error = this.validationError(f);
    if (error) { this.formError.set(error); return; }

    const entries = f.entries.map(e => ({ categoryId: e.categoryId, amount: Number(e.amount) }));
    const body = {
      name: f.name.trim(),
      categoryId: entries[0].categoryId,
      amount: this.totalAmount(entries),
      from: f.from,
      to: f.to,
      note: f.note.trim() || null,
      entries
    };

    this.saving.set(true);
    this.formError.set(null);
    try {
      if (f.id) await firstValueFrom(this.api.updateBudget(f.id, body));
      else await firstValueFrom(this.api.createBudget(body));
      this.form.set(null);
      await this.refresh();
    } catch (err: any) {
      this.formError.set(err?.error?.message ?? 'Save failed.');
    } finally {
      this.saving.set(false);
    }
  }

  async remove(b: Budget): Promise<void> {
    if (!confirm(`Delete budget "${b.name}"?`)) return;
    try { await firstValueFrom(this.api.deleteBudget(b.id)); await this.refresh(); }
    catch (err: any) { alert(err?.error?.message ?? 'Delete failed.'); }
  }

  async toggleReport(b: Budget): Promise<void> {
    if (this.openReportId() === b.id) {
      this.openReportId.set(null);
      this.report.set(null);
      return;
    }
    this.openReportId.set(b.id);
    this.loadingReport.set(true);
    try { this.report.set(await firstValueFrom(this.api.getBudgetReport(b.id))); }
    finally { this.loadingReport.set(false); }
  }

  async downloadReport(b: Budget, format: ReportFormat): Promise<void> {
    try {
      const res = await firstValueFrom(this.api.downloadBudgetReport(b.id, format));
      const filename = parseFilename(res.headers.get('content-disposition')) ?? `budget-${b.name}-${b.from}-to-${b.to}.${format.toLowerCase()}`;
      triggerDownload(res.body!, filename);
    } catch {
      alert('Download failed.');
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
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}
