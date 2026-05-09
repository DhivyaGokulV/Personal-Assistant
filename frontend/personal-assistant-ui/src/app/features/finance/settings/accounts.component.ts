import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { FinanceApi } from '../finance.api';
import { Account, AccountStatus, fmtMoney } from '../finance.models';

interface Form {
  id?: string;
  name: string;
  description: string;
  openingBalance: number;
  openingDate: string;
  status: AccountStatus;
}

function todayIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

@Component({
  selector: 'app-finance-accounts',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="d-flex justify-content-between align-items-center mb-2">
      <p class="text-muted-soft small mb-0">Accounts hold money. Inactive accounts won't appear when creating transactions.</p>
      <button class="btn-neon btn-sm" type="button" (click)="toggleAdd()">
        {{ form() ? 'Cancel' : '+ New Account' }}
      </button>
    </div>

    @if (form(); as f) {
      <div class="surface p-3 mb-3">
        <div class="row g-2 align-items-end">
          <div class="col-12 col-md-3">
            <label class="form-label small">Name</label>
            <input class="form-control form-control-sm" [(ngModel)]="f.name" />
          </div>
          <div class="col-12 col-md-3">
            <label class="form-label small">Description</label>
            <input class="form-control form-control-sm" [(ngModel)]="f.description" />
          </div>
          <div class="col-6 col-md-2">
            <label class="form-label small">Opening Balance</label>
            <input type="number" class="form-control form-control-sm" [(ngModel)]="f.openingBalance" />
          </div>
          <div class="col-6 col-md-2">
            <label class="form-label small">Opening Date</label>
            <input type="date" class="form-control form-control-sm" [(ngModel)]="f.openingDate" />
          </div>
          <div class="col-6 col-md-1">
            <label class="form-label small">Status</label>
            <select class="form-select form-select-sm" [(ngModel)]="f.status">
              <option [ngValue]="1">Active</option>
              <option [ngValue]="2">Inactive</option>
            </select>
          </div>
          <div class="col-6 col-md-1 d-grid">
            <button class="btn-neon btn-sm" type="button" [disabled]="!f.name.trim() || saving()" (click)="save()">Save</button>
          </div>
        </div>
        @if (errorMessage()) { <div class="alert alert-danger py-1 px-2 small mb-0 mt-2">{{ errorMessage() }}</div> }
      </div>
    }

    @if (loading()) { <div class="text-muted-soft small">Loading…</div> }
    @else if (accounts().length === 0) {
      <div class="surface p-4 text-center text-muted-soft">No accounts yet.</div>
    } @else {
      <div class="table-wrap surface">
        <table class="settings-table">
          <thead>
            <tr><th>Name</th><th>Description</th><th class="text-end">Opening</th><th>Opening Date</th><th>Status</th><th></th></tr>
          </thead>
          <tbody>
            @for (a of accounts(); track a.id) {
              <tr>
                <td><strong>{{ a.name }}</strong></td>
                <td class="text-muted-soft">{{ a.description ?? '—' }}</td>
                <td class="text-end">{{ fmtMoney(a.openingBalance) }}</td>
                <td>{{ a.openingDate }}</td>
                <td><span class="status-pill" [class.is-active]="a.status === 1">{{ a.status === 1 ? 'Active' : 'Inactive' }}</span></td>
                <td class="actions">
                  <button class="icon-btn" type="button" title="Edit" (click)="startEdit(a)">✎</button>
                  <button class="icon-btn icon-danger" type="button" title="Delete" (click)="remove(a)">🗑</button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
  styles: [`
    .table-wrap { overflow: auto; border: 1px solid var(--border); border-radius: var(--radius-md); }
    .settings-table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
    .settings-table thead th {
      background: var(--surface-2); color: var(--fg-muted);
      font-weight: 600; text-align: left;
      padding: 0.5rem 0.85rem;
      border-bottom: 1px solid var(--border-strong);
      white-space: nowrap;
    }
    .settings-table tbody td { padding: 0.55rem 0.85rem; border-bottom: 1px solid var(--border); }
    .settings-table tbody tr:hover td { background: var(--surface-2); }

    .status-pill { display: inline-flex; padding: 0.1rem 0.55rem; border-radius: 999px; font-size: 0.75rem; border: 1px solid var(--border-strong); color: var(--fg-muted); }
    .status-pill.is-active { color: var(--success); border-color: var(--success); }

    .icon-btn { width: 28px; height: 28px; border-radius: var(--radius-sm); background: transparent; border: 1px solid transparent; color: var(--fg-muted); cursor: pointer; transition: all 120ms ease; font-size: 0.9rem; }
    .icon-btn:hover { color: var(--fg); border-color: var(--border-strong); }
    .icon-danger:hover { color: var(--danger); border-color: var(--danger); }
    .actions { white-space: nowrap; }
  `]
})
export class AccountsComponent {
  private readonly api = inject(FinanceApi);
  readonly fmtMoney = fmtMoney;

  readonly accounts = signal<Account[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly form = signal<Form | null>(null);
  readonly errorMessage = signal<string | null>(null);

  constructor() { this.refresh(); }

  async refresh(): Promise<void> {
    this.loading.set(true);
    try { this.accounts.set(await firstValueFrom(this.api.getAccounts(true))); }
    finally { this.loading.set(false); }
  }

  toggleAdd(): void {
    this.errorMessage.set(null);
    if (this.form()) { this.form.set(null); return; }
    this.form.set({ name: '', description: '', openingBalance: 0, openingDate: todayIso(), status: 1 });
  }

  startEdit(a: Account): void {
    this.errorMessage.set(null);
    this.form.set({
      id: a.id, name: a.name, description: a.description ?? '',
      openingBalance: a.openingBalance, openingDate: a.openingDate, status: a.status
    });
  }

  async save(): Promise<void> {
    const f = this.form();
    if (!f || !f.name.trim()) return;
    this.saving.set(true);
    this.errorMessage.set(null);
    try {
      const body = {
        name: f.name.trim(),
        description: f.description.trim() || null,
        openingBalance: Number(f.openingBalance) || 0,
        openingDate: f.openingDate,
        status: f.status
      };
      if (f.id) await firstValueFrom(this.api.updateAccount(f.id, body));
      else await firstValueFrom(this.api.createAccount(body));
      this.form.set(null);
      await this.refresh();
    } catch (err: any) {
      this.errorMessage.set(err?.error?.message ?? 'Save failed.');
    } finally {
      this.saving.set(false);
    }
  }

  async remove(a: Account): Promise<void> {
    if (!confirm(`Delete account "${a.name}"?`)) return;
    try { await firstValueFrom(this.api.deleteAccount(a.id)); await this.refresh(); }
    catch (err: any) { alert(err?.error?.message ?? 'Delete failed. Account may have transactions — try marking it inactive instead.'); }
  }
}
