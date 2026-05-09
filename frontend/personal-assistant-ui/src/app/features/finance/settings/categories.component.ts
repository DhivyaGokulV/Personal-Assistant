import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { FinanceApi } from '../finance.api';
import { CATEGORY_TYPES, Category, CategoryType } from '../finance.models';

interface Form { id?: string; name: string; type: CategoryType; }

@Component({
  selector: 'app-finance-categories',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="d-flex justify-content-between align-items-center mb-2">
      <p class="text-muted-soft small mb-0">Categorize transactions as Need, Want, or Saving.</p>
      <button class="btn-neon btn-sm" type="button" (click)="toggleAdd()">{{ form() ? 'Cancel' : '+ New Category' }}</button>
    </div>

    @if (form(); as f) {
      <div class="surface p-3 mb-3">
        <div class="row g-2 align-items-end">
          <div class="col-12 col-md-6">
            <label class="form-label small">Name</label>
            <input class="form-control form-control-sm" [(ngModel)]="f.name" />
          </div>
          <div class="col-6 col-md-4">
            <label class="form-label small">Type</label>
            <select class="form-select form-select-sm" [(ngModel)]="f.type">
              @for (t of types; track t.value) { <option [ngValue]="t.value">{{ t.label }}</option> }
            </select>
          </div>
          <div class="col-6 col-md-2 d-grid">
            <button class="btn-neon btn-sm" type="button" [disabled]="!f.name.trim() || saving()" (click)="save()">Save</button>
          </div>
        </div>
      </div>
    }

    @if (loading()) { <div class="text-muted-soft small">Loading…</div> }
    @else if (categories().length === 0) { <div class="surface p-4 text-center text-muted-soft">No categories yet.</div> }
    @else {
      <div class="table-wrap surface">
        <table class="settings-table">
          <thead><tr><th>Name</th><th>Type</th><th></th></tr></thead>
          <tbody>
            @for (c of categories(); track c.id) {
              <tr>
                <td><strong>{{ c.name }}</strong></td>
                <td><span class="type-pill" [ngClass]="toneFor(c.type)">{{ labelFor(c.type) }}</span></td>
                <td class="actions">
                  <button class="icon-btn" type="button" title="Edit" (click)="startEdit(c)">✎</button>
                  <button class="icon-btn icon-danger" type="button" title="Delete" (click)="remove(c)">🗑</button>
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
    .settings-table thead th { background: var(--surface-2); color: var(--fg-muted); font-weight: 600; text-align: left; padding: 0.5rem 0.85rem; border-bottom: 1px solid var(--border-strong); }
    .settings-table tbody td { padding: 0.55rem 0.85rem; border-bottom: 1px solid var(--border); }
    .settings-table tbody tr:hover td { background: var(--surface-2); }

    .type-pill { display: inline-flex; padding: 0.1rem 0.6rem; border-radius: 999px; font-size: 0.75rem; border: 1px solid var(--border-strong); }
    .tone-amber { color: var(--warning); border-color: var(--warning); }
    .tone-violet { color: var(--primary); border-color: var(--primary); }
    .tone-green { color: var(--success); border-color: var(--success); }

    .icon-btn { width: 28px; height: 28px; border-radius: var(--radius-sm); background: transparent; border: 1px solid transparent; color: var(--fg-muted); cursor: pointer; transition: all 120ms ease; font-size: 0.9rem; }
    .icon-btn:hover { color: var(--fg); border-color: var(--border-strong); }
    .icon-danger:hover { color: var(--danger); border-color: var(--danger); }
    .actions { white-space: nowrap; }
  `]
})
export class CategoriesComponent {
  private readonly api = inject(FinanceApi);
  readonly types = CATEGORY_TYPES;

  readonly categories = signal<Category[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly form = signal<Form | null>(null);

  constructor() { this.refresh(); }

  async refresh(): Promise<void> {
    this.loading.set(true);
    try { this.categories.set(await firstValueFrom(this.api.getCategories())); }
    finally { this.loading.set(false); }
  }

  labelFor(t: CategoryType): string { return CATEGORY_TYPES.find(x => x.value === t)?.label ?? '?'; }
  toneFor(t: CategoryType): string { return CATEGORY_TYPES.find(x => x.value === t)?.tone ?? ''; }

  toggleAdd(): void {
    if (this.form()) { this.form.set(null); return; }
    this.form.set({ name: '', type: 1 });
  }
  startEdit(c: Category): void { this.form.set({ id: c.id, name: c.name, type: c.type }); }

  async save(): Promise<void> {
    const f = this.form();
    if (!f || !f.name.trim()) return;
    this.saving.set(true);
    try {
      const body = { name: f.name.trim(), type: f.type };
      if (f.id) await firstValueFrom(this.api.updateCategory(f.id, body));
      else await firstValueFrom(this.api.createCategory(body));
      this.form.set(null);
      await this.refresh();
    } finally { this.saving.set(false); }
  }

  async remove(c: Category): Promise<void> {
    if (!confirm(`Delete category "${c.name}"?`)) return;
    try { await firstValueFrom(this.api.deleteCategory(c.id)); await this.refresh(); }
    catch (err: any) { alert(err?.error?.message ?? 'Delete failed. Category may be used by a budget.'); }
  }
}
