import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AssetTrackerApi } from '../asset-tracker.api';
import {
  Liability,
  LiabilityDetail,
  LiabilityHistoryEntry,
  LiabilityStatus,
  LiabilityTxType,
  ReportFormat,
  fmtMoney,
  isoToday
} from '../asset-tracker.models';
import { TagPickerComponent } from '../shared/tag-picker.component';
import { LineChartComponent } from '../shared/line-chart.component';

interface CreateForm {
  name: string;
  description: string;
  tagId: string | null;
  initialAmount: number;
  date: string;
  note: string;
}

interface EditForm { id: string; name: string; description: string; tagId: string | null; }
interface EntryForm { id?: string; date: string; type: LiabilityTxType; amount: number; note: string; }

type ModalTab = 'analytics' | 'history';

@Component({
  selector: 'app-asset-tracker-liabilities',
  imports: [CommonModule, FormsModule, TagPickerComponent, LineChartComponent],
  template: `
    <div class="liab-shell">
      <!-- Top -->
      <div class="surface p-3 mb-3">
        <div class="kpi-label">Total Liabilities (Active)</div>
        <div class="net-total tone-red">{{ fmtMoney(activeTotal()) }}</div>
      </div>

      <div class="d-flex flex-wrap align-items-center gap-2 mb-3">
        <div class="ms-auto d-flex gap-2 flex-wrap">
          <button class="btn-link-soft btn-sm" type="button" (click)="downloadReport('Csv')">CSV</button>
          <button class="btn-link-soft btn-sm" type="button" (click)="downloadReport('Xlsx')">Excel</button>
          <button class="btn-link-soft btn-sm" type="button" (click)="downloadReport('Pdf')">PDF</button>
          <button class="btn-neon btn-sm" type="button" (click)="openCreate()">+ New Liability</button>
        </div>
      </div>

      @if (createForm(); as f) {
        <div class="surface p-3 mb-3">
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
              <label class="form-label small">Initial Amount *</label>
              <input type="number" min="0" step="0.01" class="form-control form-control-sm" [(ngModel)]="f.initialAmount" />
            </div>
            <div class="col-6 col-md-2">
              <label class="form-label small">As of</label>
              <input type="date" class="form-control form-control-sm" [(ngModel)]="f.date" />
            </div>
            <div class="col-12">
              <label class="form-label small">Note</label>
              <input class="form-control form-control-sm" [(ngModel)]="f.note" />
            </div>
            <div class="col-12 d-flex gap-2 justify-content-end">
              <button class="btn-link-soft btn-sm" type="button" (click)="createForm.set(null)">Cancel</button>
              <button class="btn-neon btn-sm" type="button" [disabled]="!isCreateValid(f) || saving()" (click)="saveCreate()">Create</button>
            </div>
          </div>
          @if (formError()) { <div class="alert alert-danger py-1 px-2 small mb-0 mt-2">{{ formError() }}</div> }
        </div>
      }

      <h2 class="section-title">Active</h2>
      @if (active().length === 0) {
        <div class="surface p-3 text-muted-soft small">No active liabilities.</div>
      } @else {
        <div class="row g-3 mb-3">
          @for (l of active(); track l.id) {
            <div class="col-12 col-md-6 col-xl-4">
              <article class="liab-card neon">
                <div class="d-flex align-items-start gap-2 mb-2">
                  <div class="flex-grow-1">
                    <div class="d-flex align-items-center gap-2 flex-wrap">
                      <button class="link-name" type="button" (click)="openLiability(l)">{{ l.name }}</button>
                      @if (l.tag; as t) { <span class="chip" [style.color]="t.color" [style.borderColor]="t.color">{{ t.name }}</span> }
                    </div>
                    <div class="small text-muted-soft">{{ l.description ?? '—' }}</div>
                  </div>
                  <div class="text-end">
                    <div class="kv-label">Outstanding</div>
                    <div class="kv-val tone-red">{{ fmtMoney(l.currentAmount) }}</div>
                  </div>
                </div>
                <div class="small text-muted-soft">Last update: {{ l.lastUpdate ?? '—' }}</div>
              </article>
            </div>
          }
        </div>
      }

      <h2 class="section-title">Past (paid off)</h2>
      @if (past().length === 0) {
        <div class="surface p-3 text-muted-soft small">No past liabilities yet.</div>
      } @else {
        <div class="row g-3">
          @for (l of past(); track l.id) {
            <div class="col-12 col-md-6 col-xl-4">
              <article class="liab-card">
                <div class="d-flex align-items-start gap-2">
                  <div class="flex-grow-1">
                    <div class="d-flex align-items-center gap-2 flex-wrap">
                      <button class="link-name" type="button" (click)="openLiability(l)">{{ l.name }}</button>
                      @if (l.tag; as t) { <span class="chip" [style.color]="t.color" [style.borderColor]="t.color">{{ t.name }}</span> }
                      <span class="status-pill is-past">Paid off</span>
                    </div>
                    <div class="small text-muted-soft">{{ l.description ?? '—' }}</div>
                  </div>
                </div>
                <div class="small text-muted-soft">Last update: {{ l.lastUpdate ?? '—' }}</div>
              </article>
            </div>
          }
        </div>
      }

      <!-- Modal -->
      @if (modal(); as m) {
        <div class="modal-backdrop" (click)="closeModal()"></div>
        <div class="modal-shell" role="dialog" aria-modal="true">
          <div class="modal-header">
            <div>
              <div class="d-flex align-items-center gap-2 flex-wrap">
                <h2 class="modal-title">{{ m.liability.name }}</h2>
                @if (m.liability.tag; as t) { <span class="chip" [style.color]="t.color" [style.borderColor]="t.color">{{ t.name }}</span> }
                <span class="status-pill" [ngClass]="m.liability.status === 1 ? 'is-active' : 'is-past'">
                  {{ m.liability.status === 1 ? 'Active' : 'Past' }}
                </span>
              </div>
              <div class="small text-muted-soft">{{ m.liability.description ?? '—' }}</div>
              <div class="kpi-value tone-red">{{ fmtMoney(m.liability.currentAmount) }} <span class="small text-muted-soft">outstanding</span></div>
            </div>
            <div class="d-flex gap-2 align-items-center">
              <button class="btn-link-soft btn-sm" type="button" (click)="toggleEdit()">{{ editing() ? 'Cancel edit' : 'Edit' }}</button>
              <button class="icon-btn icon-danger" type="button" (click)="deleteLiability(m.liability)">🗑</button>
              <button class="icon-btn" type="button" (click)="closeModal()">×</button>
            </div>
          </div>

          @if (editing()) {
            <div class="surface p-2 mb-3">
              <div class="row g-2 align-items-end">
                <div class="col-12 col-md-4">
                  <label class="form-label small">Name</label>
                  <input class="form-control form-control-sm" [(ngModel)]="editForm.name" />
                </div>
                <div class="col-12 col-md-5">
                  <label class="form-label small">Description</label>
                  <input class="form-control form-control-sm" [(ngModel)]="editForm.description" />
                </div>
                <div class="col-6 col-md-2">
                  <label class="form-label small">Tag</label>
                  <app-tag-picker [value]="editForm.tagId" (valueChange)="editForm.tagId = $event" />
                </div>
                <div class="col-6 col-md-1 d-grid">
                  <button class="btn-neon btn-sm" type="button" [disabled]="!editForm.name.trim() || saving()" (click)="saveEdit()">Save</button>
                </div>
              </div>
            </div>
          }

          <ul class="nav nav-pills sub-tabs mb-2">
            <li class="nav-item"><button class="nav-link" [class.active]="modalTab() === 'analytics'" (click)="modalTab.set('analytics')">Analytics</button></li>
            <li class="nav-item"><button class="nav-link" [class.active]="modalTab() === 'history'" (click)="modalTab.set('history')">History</button></li>
          </ul>

          @if (modalTab() === 'analytics') {
            <div>
              <h3 class="section-title">Outstanding amount over time</h3>
              <div class="surface p-2 mb-3">
                <app-line-chart [series]="balanceSeries()" color="var(--danger)" />
              </div>
            </div>
          } @else {
            <div>
              <div class="d-flex justify-content-between mb-2 align-items-center">
                <h3 class="section-title m-0">Acquisitions / Repayments</h3>
                <button class="btn-link-soft btn-sm" type="button" (click)="openAddEntry()">+ New Entry</button>
              </div>

              @if (entryForm(); as f) {
                <div class="surface p-2 mb-2">
                  <div class="row g-2 align-items-end">
                    <div class="col-6 col-md-2">
                      <label class="form-label small">Date</label>
                      <input type="date" class="form-control form-control-sm" [(ngModel)]="f.date" />
                    </div>
                    <div class="col-6 col-md-2">
                      <label class="form-label small">Type</label>
                      <select class="form-select form-select-sm" [(ngModel)]="f.type">
                        <option [ngValue]="1">Acquisition</option>
                        <option [ngValue]="2">Repayment</option>
                      </select>
                    </div>
                    <div class="col-6 col-md-2">
                      <label class="form-label small">Amount</label>
                      <input type="number" min="0" step="0.01" class="form-control form-control-sm" [(ngModel)]="f.amount" />
                    </div>
                    <div class="col-12 col-md-4">
                      <label class="form-label small">Note</label>
                      <input class="form-control form-control-sm" [(ngModel)]="f.note" />
                    </div>
                    <div class="col-12 col-md-2 d-grid gap-1">
                      <button class="btn-neon btn-sm" type="button" [disabled]="f.amount <= 0 || saving()" (click)="saveEntry()">Save</button>
                      <button class="btn-link-soft btn-sm" type="button" (click)="entryForm.set(null)">Cancel</button>
                    </div>
                  </div>
                  @if (entryError()) { <div class="alert alert-danger py-1 px-2 small mb-0 mt-2">{{ entryError() }}</div> }
                </div>
              }

              <div class="table-wrap">
                <table class="hist-table">
                  <thead>
                    <tr><th>Date</th><th>Type</th><th class="text-end">Amount</th><th class="text-end">Standing</th><th>Note</th><th></th></tr>
                  </thead>
                  <tbody>
                    @for (h of m.history; track h.id) {
                      <tr>
                        <td>{{ h.date }}</td>
                        <td>
                          <span class="status-pill" [ngClass]="h.type === 1 ? 'tone-red' : 'tone-green'">{{ h.type === 1 ? 'Acquisition' : 'Repayment' }}</span>
                        </td>
                        <td class="text-end">{{ fmtMoney(h.amount) }}</td>
                        <td class="text-end">{{ fmtMoney(h.runningBalance) }}</td>
                        <td class="text-muted-soft">{{ h.note ?? '—' }}</td>
                        <td class="actions">
                          <button class="icon-btn" type="button" title="Edit" (click)="startEditEntry(h)">✎</button>
                          <button class="icon-btn icon-danger" type="button" title="Delete" (click)="deleteEntry(h)">🗑</button>
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .kpi-label { font-size: 0.78rem; color: var(--fg-muted); text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 0.4rem; }
    .net-total { font-size: 2rem; font-weight: 700; line-height: 1; }
    .kpi-value { font-size: 1.4rem; font-weight: 600; }
    .tone-red { color: var(--danger); }
    .tone-green { color: var(--success); }

    .section-title { font-size: 0.85rem; font-weight: 600; margin: 0.5rem 0; color: var(--fg-muted); text-transform: uppercase; letter-spacing: 0.05em; }

    .liab-card {
      padding: 1rem;
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: var(--radius-md);
      transition: all 160ms ease;
      height: 100%;
    }
    .liab-card.neon { border-color: var(--neon); box-shadow: 0 0 8px var(--neon-soft); }

    .chip { display: inline-flex; padding: 0.1rem 0.55rem; border-radius: 999px; font-size: 0.72rem; border: 1px solid; font-weight: 500; }
    .kv-label { font-size: 0.7rem; color: var(--fg-muted); text-transform: uppercase; letter-spacing: 0.05em; }
    .kv-val { font-weight: 600; font-size: 1.05rem; }

    .link-name { background: transparent; border: none; padding: 0; color: var(--neon); cursor: pointer; font-weight: 600; font-size: 1rem; }
    .link-name:hover { text-decoration: underline; }

    .status-pill { display: inline-flex; padding: 0.1rem 0.55rem; border-radius: 999px; font-size: 0.72rem; border: 1px solid var(--border-strong); color: var(--fg-muted); }
    .status-pill.is-active { color: var(--success); border-color: var(--success); }
    .status-pill.is-past { color: var(--fg-subtle); }
    .status-pill.tone-red { color: var(--danger); border-color: var(--danger); }
    .status-pill.tone-green { color: var(--success); border-color: var(--success); }

    .table-wrap { overflow: auto; border: 1px solid var(--border); border-radius: var(--radius-sm); }
    .hist-table { width: 100%; border-collapse: collapse; font-size: 0.86rem; }
    .hist-table thead th { background: var(--surface-2); color: var(--fg-muted); font-weight: 600; text-align: left; padding: 0.45rem 0.7rem; border-bottom: 1px solid var(--border-strong); }
    .hist-table tbody td { padding: 0.5rem 0.7rem; border-bottom: 1px solid var(--border); }
    .hist-table tbody tr:hover td { background: var(--surface-2); }

    .icon-btn { width: 28px; height: 28px; border-radius: var(--radius-sm); background: transparent; border: 1px solid transparent; color: var(--fg-muted); cursor: pointer; transition: all 120ms ease; font-size: 0.9rem; }
    .icon-btn:hover { color: var(--fg); border-color: var(--border-strong); }
    .icon-danger:hover { color: var(--danger); border-color: var(--danger); }
    .actions { white-space: nowrap; }

    .btn-link-soft { background: transparent; border: 1px solid var(--border-strong); color: var(--fg-muted); padding: 0.3rem 0.7rem; border-radius: var(--radius-sm); cursor: pointer; transition: all 120ms ease; font-size: 0.8rem; }
    .btn-link-soft:hover { color: var(--neon); border-color: var(--neon); }

    .modal-backdrop { position: fixed; inset: 0; background: rgba(0,0,0,0.55); z-index: 100; }
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
    .modal-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 0.75rem; gap: 0.5rem; flex-wrap: wrap; }
    .modal-title { font-size: 1.2rem; font-weight: 600; margin: 0; }

    .sub-tabs .nav-link { color: var(--fg-muted); background: transparent; border: 1px solid var(--border-strong); border-radius: var(--radius-sm); padding: 0.3rem 0.7rem; font-size: 0.85rem; }
    .sub-tabs .nav-link.active { color: var(--neon); border-color: var(--neon); box-shadow: 0 0 8px var(--neon-soft); }
    .sub-tabs { gap: 0.4rem; }
  `]
})
export class LiabilitiesComponent {
  private readonly api = inject(AssetTrackerApi);
  fmtMoney = fmtMoney;

  readonly liabilities = signal<Liability[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);

  readonly createForm = signal<CreateForm | null>(null);
  readonly formError = signal<string | null>(null);

  readonly modal = signal<LiabilityDetail | null>(null);
  readonly modalTab = signal<ModalTab>('analytics');
  readonly editing = signal(false);

  readonly entryForm = signal<EntryForm | null>(null);
  readonly entryError = signal<string | null>(null);

  editForm: EditForm = { id: '', name: '', description: '', tagId: null };

  readonly active = computed(() => this.liabilities().filter(l => l.status === 1));
  readonly past = computed(() => this.liabilities().filter(l => l.status === 2));
  readonly activeTotal = computed(() => this.active().reduce((acc, l) => acc + l.currentAmount, 0));

  readonly balanceSeries = computed(() => {
    const m = this.modal();
    if (!m) return [];
    const ordered = [...m.history].reverse();  // history comes newest-first
    return ordered.map(h => ({ date: h.date, value: h.runningBalance }));
  });

  constructor() { this.refresh(); }

  async refresh(): Promise<void> {
    this.loading.set(true);
    try { this.liabilities.set(await firstValueFrom(this.api.listLiabilities())); }
    finally { this.loading.set(false); }
  }

  // ===== Create =====
  openCreate(): void {
    this.formError.set(null);
    this.createForm.set({ name: '', description: '', tagId: null, initialAmount: 0, date: isoToday(), note: '' });
  }
  isCreateValid(f: CreateForm): boolean { return !!f.name.trim() && f.initialAmount > 0; }
  async saveCreate(): Promise<void> {
    const f = this.createForm();
    if (!f || !this.isCreateValid(f)) return;
    this.saving.set(true);
    this.formError.set(null);
    try {
      await firstValueFrom(this.api.createLiability({
        name: f.name.trim(),
        description: f.description.trim() || null,
        tagId: f.tagId,
        initialAmount: Number(f.initialAmount),
        date: f.date || null,
        note: f.note.trim() || null
      }));
      this.createForm.set(null);
      await this.refresh();
    } catch (err: any) {
      this.formError.set(err?.error?.message ?? 'Save failed.');
    } finally { this.saving.set(false); }
  }

  // ===== Modal =====
  async openLiability(l: Liability): Promise<void> {
    const detail = await firstValueFrom(this.api.getLiability(l.id));
    this.modal.set(detail);
    this.modalTab.set('analytics');
    this.editing.set(false);
    this.editForm = {
      id: detail.liability.id,
      name: detail.liability.name,
      description: detail.liability.description ?? '',
      tagId: detail.liability.tag?.id ?? null
    };
  }
  closeModal(): void {
    this.modal.set(null);
    this.entryForm.set(null);
    this.refresh();
  }
  toggleEdit(): void { this.editing.update(v => !v); }
  async saveEdit(): Promise<void> {
    if (!this.editForm.name.trim()) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.updateLiability(this.editForm.id, {
        name: this.editForm.name.trim(),
        description: this.editForm.description.trim() || null,
        tagId: this.editForm.tagId
      }));
      this.editing.set(false);
      await this.refreshModal();
    } finally { this.saving.set(false); }
  }
  async deleteLiability(l: Liability): Promise<void> {
    if (!confirm(`Delete liability "${l.name}" and all its history?`)) return;
    try {
      await firstValueFrom(this.api.deleteLiability(l.id));
      this.closeModal();
    } catch (err: any) { alert(err?.error?.message ?? 'Delete failed.'); }
  }
  private async refreshModal(): Promise<void> {
    const m = this.modal();
    if (!m) return;
    const detail = await firstValueFrom(this.api.getLiability(m.liability.id));
    this.modal.set(detail);
  }

  // ===== History entries =====
  openAddEntry(): void {
    this.entryError.set(null);
    this.entryForm.set({ date: isoToday(), type: 2, amount: 0, note: '' });
  }
  startEditEntry(h: LiabilityHistoryEntry): void {
    this.entryError.set(null);
    this.entryForm.set({ id: h.id, date: h.date, type: h.type, amount: h.amount, note: h.note ?? '' });
  }
  async saveEntry(): Promise<void> {
    const f = this.entryForm();
    const m = this.modal();
    if (!f || !m || f.amount <= 0) return;
    this.saving.set(true);
    this.entryError.set(null);
    try {
      const body = { date: f.date, type: f.type, amount: Number(f.amount), note: f.note.trim() || null };
      if (f.id) await firstValueFrom(this.api.updateLiabilityHistory(m.liability.id, f.id, body));
      else await firstValueFrom(this.api.addLiabilityHistory(m.liability.id, body));
      this.entryForm.set(null);
      await this.refreshModal();
    } catch (err: any) {
      this.entryError.set(err?.error?.message ?? 'Save failed.');
    } finally { this.saving.set(false); }
  }
  async deleteEntry(h: LiabilityHistoryEntry): Promise<void> {
    const m = this.modal();
    if (!m) return;
    if (!confirm(`Delete entry on ${h.date}?`)) return;
    try { await firstValueFrom(this.api.deleteLiabilityHistory(m.liability.id, h.id)); await this.refreshModal(); }
    catch (err: any) { alert(err?.error?.message ?? 'Delete failed.'); }
  }

  // ===== Reports =====
  async downloadReport(format: ReportFormat): Promise<void> {
    try {
      const res = await firstValueFrom(this.api.downloadLiabilitiesReport(null, format));
      const blob = res.body!;
      const fname = parseFilename(res.headers.get('content-disposition'))
        ?? `liabilities.${format.toLowerCase()}`;
      triggerDownload(blob, fname);
    } catch { alert('Download failed.'); }
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
