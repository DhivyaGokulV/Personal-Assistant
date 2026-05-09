import { Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AssetTrackerApi } from '../asset-tracker.api';
import { AssetTag } from '../asset-tracker.models';

/**
 * Inline tag picker. Loads tags lazily on first open.
 * Lets the user select one tag (or none), create a new tag, edit, or delete.
 */
@Component({
  selector: 'app-tag-picker',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="tag-picker" [class.compact]="compact">
      <button type="button" class="picker-btn" (click)="toggleOpen()">
        @if (selected(); as s) {
          <span class="chip" [style.color]="s.color" [style.borderColor]="s.color">{{ s.name }}</span>
        } @else {
          <span class="chip-empty">+ Tag</span>
        }
      </button>

      @if (open()) {
        <div class="dropdown surface">
          <div class="dropdown-header">
            <span>Pick a tag</span>
            <button type="button" class="icon-btn" (click)="open.set(false)" title="Close">×</button>
          </div>

          @if (loading()) {
            <div class="text-muted-soft small px-2 pb-2">Loading…</div>
          } @else {
            <div class="tag-list">
              <button type="button" class="tag-row" [class.is-active]="!value" (click)="select(null)">
                <span class="chip-empty">No tag</span>
              </button>
              @for (t of tags(); track t.id) {
                <div class="tag-row" [class.is-active]="value === t.id">
                  <button type="button" class="chip-btn" (click)="select(t.id)">
                    <span class="chip" [style.color]="t.color" [style.borderColor]="t.color">{{ t.name }}</span>
                  </button>
                  <span class="actions">
                    <button class="icon-btn" type="button" title="Edit" (click)="startEdit(t)">✎</button>
                    <button class="icon-btn icon-danger" type="button" title="Delete" (click)="remove(t)">🗑</button>
                  </span>
                </div>
              }
            </div>
          }

          <div class="dropdown-create">
            @if (creating(); as f) {
              <div class="row g-1 align-items-end">
                <div class="col-12">
                  <input class="form-control form-control-sm" placeholder="Tag name" [(ngModel)]="f.name" />
                </div>
                <div class="col-12 d-flex gap-1">
                  <input type="color" class="form-control form-control-sm form-control-color" [(ngModel)]="f.color" />
                  <button type="button" class="btn-neon btn-sm flex-grow-1" [disabled]="!f.name.trim() || saving()" (click)="saveCreate()">{{ f.id ? 'Save' : 'Create' }}</button>
                  <button type="button" class="btn-link-soft btn-sm" (click)="creating.set(null)">Cancel</button>
                </div>
              </div>
            } @else {
              <button type="button" class="btn-link-soft btn-sm w-100" (click)="startCreate()">+ New tag</button>
            }
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    :host { position: relative; display: inline-block; }
    .tag-picker { position: relative; }

    .picker-btn { background: transparent; border: none; padding: 0; cursor: pointer; }
    .chip { display: inline-flex; padding: 0.18rem 0.6rem; border-radius: 999px; font-size: 0.78rem; border: 1px solid; font-weight: 500; }
    .chip-empty { display: inline-flex; padding: 0.18rem 0.6rem; border-radius: 999px; font-size: 0.78rem; border: 1px dashed var(--border-strong); color: var(--fg-muted); }

    .dropdown {
      position: absolute; top: calc(100% + 4px); left: 0; z-index: 30;
      min-width: 240px; max-width: 340px;
      background: var(--surface-elevated);
      border: 1px solid var(--border-strong);
      border-radius: var(--radius-md);
      box-shadow: 0 8px 24px rgba(0,0,0,0.15), 0 0 12px var(--neon-soft);
      padding: 0.4rem;
    }
    .dropdown-header {
      display: flex; align-items: center; justify-content: space-between;
      padding: 0.25rem 0.5rem; font-size: 0.78rem; color: var(--fg-muted); text-transform: uppercase; letter-spacing: 0.05em;
    }
    .tag-list { max-height: 220px; overflow: auto; }
    .tag-row {
      display: flex; align-items: center; justify-content: space-between;
      padding: 0.2rem 0.4rem; border-radius: var(--radius-sm);
      background: transparent; border: none; cursor: pointer;
    }
    .tag-row.is-active { background: var(--surface-2); }
    .chip-btn { background: transparent; border: none; padding: 0; cursor: pointer; flex: 1; text-align: left; }
    .actions { display: flex; gap: 0.1rem; }

    .dropdown-create { margin-top: 0.4rem; padding-top: 0.4rem; border-top: 1px dashed var(--border); }

    .icon-btn { width: 22px; height: 22px; border-radius: var(--radius-sm); background: transparent; border: 1px solid transparent; color: var(--fg-muted); cursor: pointer; font-size: 0.75rem; }
    .icon-btn:hover { color: var(--fg); border-color: var(--border-strong); }
    .icon-danger:hover { color: var(--danger); border-color: var(--danger); }

    .btn-link-soft { background: transparent; border: 1px solid var(--border-strong); color: var(--fg-muted); padding: 0.25rem 0.6rem; border-radius: var(--radius-sm); font-size: 0.78rem; cursor: pointer; }
    .btn-link-soft:hover { color: var(--neon); border-color: var(--neon); }
  `]
})
export class TagPickerComponent {
  private readonly api = inject(AssetTrackerApi);

  @Input() value: string | null = null;
  @Input() compact = false;
  @Output() valueChange = new EventEmitter<string | null>();

  readonly open = signal(false);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly tags = signal<AssetTag[]>([]);
  readonly creating = signal<{ id?: string; name: string; color: string } | null>(null);

  selected(): AssetTag | null {
    return this.value ? this.tags().find(t => t.id === this.value) ?? null : null;
  }

  async toggleOpen(): Promise<void> {
    if (this.open()) { this.open.set(false); return; }
    this.open.set(true);
    if (this.tags().length === 0) await this.refresh();
  }

  private async refresh(): Promise<void> {
    this.loading.set(true);
    try { this.tags.set(await firstValueFrom(this.api.listTags())); }
    finally { this.loading.set(false); }
  }

  select(id: string | null): void {
    this.valueChange.emit(id);
    this.open.set(false);
  }

  startCreate(): void { this.creating.set({ name: '', color: '#6c5ce7' }); }
  startEdit(t: AssetTag): void { this.creating.set({ id: t.id, name: t.name, color: t.color }); }

  async saveCreate(): Promise<void> {
    const f = this.creating();
    if (!f || !f.name.trim()) return;
    this.saving.set(true);
    try {
      if (f.id) {
        await firstValueFrom(this.api.updateTag(f.id, { name: f.name.trim(), description: null, color: f.color }));
      } else {
        const created = await firstValueFrom(this.api.createTag({ name: f.name.trim(), description: null, color: f.color }));
        // Auto-select newly created tag
        this.value = created.id;
        this.valueChange.emit(created.id);
      }
      this.creating.set(null);
      await this.refresh();
    } finally { this.saving.set(false); }
  }

  async remove(t: AssetTag): Promise<void> {
    if (!confirm(`Delete tag "${t.name}"?`)) return;
    try {
      await firstValueFrom(this.api.deleteTag(t.id));
      if (this.value === t.id) this.select(null);
      else await this.refresh();
    } catch (err: any) {
      alert(err?.error?.message ?? 'Delete failed.');
    }
  }
}
