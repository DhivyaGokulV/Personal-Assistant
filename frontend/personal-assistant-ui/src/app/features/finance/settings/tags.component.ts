import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { FinanceApi } from '../finance.api';
import { Tag } from '../finance.models';

interface Form { id?: string; name: string; description: string; color: string; }

@Component({
  selector: 'app-finance-tags',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="d-flex justify-content-between align-items-center mb-2">
      <p class="text-muted-soft small mb-0">Tags can be applied to multiple transactions for cross-cutting filtering and reporting.</p>
      <button class="btn-neon btn-sm" type="button" (click)="toggleAdd()">{{ form() ? 'Cancel' : '+ New Tag' }}</button>
    </div>

    @if (form(); as f) {
      <div class="surface p-3 mb-3">
        <div class="row g-2 align-items-end">
          <div class="col-12 col-md-3">
            <label class="form-label small">Name</label>
            <input class="form-control form-control-sm" [(ngModel)]="f.name" />
          </div>
          <div class="col-12 col-md-5">
            <label class="form-label small">Description</label>
            <input class="form-control form-control-sm" [(ngModel)]="f.description" />
          </div>
          <div class="col-6 col-md-2">
            <label class="form-label small">Color</label>
            <input type="color" class="form-control form-control-color form-control-sm" [(ngModel)]="f.color" />
          </div>
          <div class="col-6 col-md-2 d-grid">
            <button class="btn-neon btn-sm" type="button" [disabled]="!f.name.trim() || saving()" (click)="save()">Save</button>
          </div>
        </div>
      </div>
    }

    @if (loading()) { <div class="text-muted-soft small">Loading…</div> }
    @else if (tags().length === 0) { <div class="surface p-4 text-center text-muted-soft">No tags yet.</div> }
    @else {
      <div class="table-wrap surface">
        <table class="settings-table">
          <thead><tr><th>Tag</th><th>Description</th><th></th></tr></thead>
          <tbody>
            @for (t of tags(); track t.id) {
              <tr>
                <td>
                  <span class="tag-pill" [style.color]="t.color" [style.borderColor]="t.color">{{ t.name }}</span>
                </td>
                <td class="text-muted-soft">{{ t.description ?? '—' }}</td>
                <td class="actions">
                  <button class="icon-btn" type="button" title="Edit" (click)="startEdit(t)">✎</button>
                  <button class="icon-btn icon-danger" type="button" title="Delete" (click)="remove(t)">🗑</button>
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

    .tag-pill { display: inline-flex; padding: 0.15rem 0.65rem; border-radius: 999px; font-size: 0.78rem; border: 1px solid; font-weight: 500; }

    .icon-btn { width: 28px; height: 28px; border-radius: var(--radius-sm); background: transparent; border: 1px solid transparent; color: var(--fg-muted); cursor: pointer; transition: all 120ms ease; font-size: 0.9rem; }
    .icon-btn:hover { color: var(--fg); border-color: var(--border-strong); }
    .icon-danger:hover { color: var(--danger); border-color: var(--danger); }
    .actions { white-space: nowrap; }
  `]
})
export class TagsComponent {
  private readonly api = inject(FinanceApi);

  readonly tags = signal<Tag[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly form = signal<Form | null>(null);

  constructor() { this.refresh(); }

  async refresh(): Promise<void> {
    this.loading.set(true);
    try { this.tags.set(await firstValueFrom(this.api.getTags())); }
    finally { this.loading.set(false); }
  }

  toggleAdd(): void {
    if (this.form()) { this.form.set(null); return; }
    this.form.set({ name: '', description: '', color: '#ff2bd6' });
  }
  startEdit(t: Tag): void { this.form.set({ id: t.id, name: t.name, description: t.description ?? '', color: t.color }); }

  async save(): Promise<void> {
    const f = this.form();
    if (!f || !f.name.trim()) return;
    this.saving.set(true);
    try {
      const body = { name: f.name.trim(), description: f.description.trim() || null, color: f.color };
      if (f.id) await firstValueFrom(this.api.updateTag(f.id, body));
      else await firstValueFrom(this.api.createTag(body));
      this.form.set(null);
      await this.refresh();
    } finally { this.saving.set(false); }
  }

  async remove(t: Tag): Promise<void> {
    if (!confirm(`Delete tag "${t.name}"?`)) return;
    try { await firstValueFrom(this.api.deleteTag(t.id)); await this.refresh(); }
    catch (err: any) { alert(err?.error?.message ?? 'Delete failed.'); }
  }
}
