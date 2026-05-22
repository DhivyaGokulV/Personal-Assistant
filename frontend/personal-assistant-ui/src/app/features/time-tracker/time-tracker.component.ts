import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { TimeTrackerApi } from './time-tracker.api';
import { ReportFormat, TimeEntry, TimeReport } from './time-tracker.models';

function pad(n: number): string { return String(n).padStart(2, '0'); }
function dateIso(d = new Date()): string { return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`; }
function localInput(d = new Date()): string { return `${dateIso(d)}T${pad(d.getHours())}:${pad(d.getMinutes())}`; }
function addHours(hours: number): string { const d = new Date(); d.setHours(d.getHours() + hours); return localInput(d); }

@Component({
  selector: 'app-time-tracker',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  template: `
    <section class="container py-4">
      <a routerLink="/home" class="text-muted-soft small">Back home</a>
      <div class="d-flex flex-wrap align-items-center gap-2 mt-2 mb-3">
        <h1 class="page-title m-0">Time Tracker</h1>
        <button class="btn-neon btn-sm ms-auto" type="button" (click)="startCreate()">+ Add Entry</button>
        <button class="btn-link-soft btn-sm" type="button" (click)="showReport.set(!showReport())">Reports</button>
      </div>

      <div class="surface p-3 mb-3">
        <div class="row g-2 align-items-end">
          <div class="col-6 col-md-2"><label class="form-label small">Day</label><input type="date" class="form-control form-control-sm" [(ngModel)]="day" (change)="refresh()" /></div>
          <div class="col-6 col-md-2"><label class="form-label small">Activity</label><input class="form-control form-control-sm" [(ngModel)]="filters.activity" (keyup.enter)="refresh()" /></div>
          <div class="col-6 col-md-2"><label class="form-label small">Tag</label><input class="form-control form-control-sm" [(ngModel)]="filters.tag" (keyup.enter)="refresh()" /></div>
          <div class="col-6 col-md-2 d-grid"><button class="btn-link-soft btn-sm" type="button" (click)="clearFilters()">Reset</button></div>
        </div>
      </div>

      @if (formOpen()) {
        <form class="surface p-3 mb-3" [formGroup]="form" (ngSubmit)="save()">
          <div class="row g-2 align-items-end">
            <div class="col-6 col-md-2"><label class="form-label small">Start *</label><input type="datetime-local" class="form-control form-control-sm" formControlName="startTime" /></div>
            <div class="col-6 col-md-2"><label class="form-label small">End *</label><input type="datetime-local" class="form-control form-control-sm" formControlName="endTime" /></div>
            <div class="col-12 col-md-3"><label class="form-label small">Activity *</label><input class="form-control form-control-sm" formControlName="activity" /></div>
            <div class="col-6 col-md-2"><label class="form-label small">Tag</label><input class="form-control form-control-sm" formControlName="tag" /></div>
            <div class="col-12 col-md-3"><label class="form-label small">Note</label><input class="form-control form-control-sm" formControlName="note" /></div>
            <div class="col-12 d-flex gap-2">
              <button class="btn-neon btn-sm" type="submit" [disabled]="saving()">Save</button>
              <button class="btn-link-soft btn-sm" type="button" (click)="cancel()">Cancel</button>
            </div>
          </div>
          @if (validationMessage()) { <div class="alert alert-danger py-1 px-2 small mt-2 mb-0">{{ validationMessage() }}</div> }
        </form>
      }

      @if (showReport()) {
        <div class="surface p-3 mb-3">
          <div class="row g-2 align-items-end">
            <div class="col-6 col-md-3"><label class="form-label small">From</label><input type="datetime-local" class="form-control form-control-sm" [(ngModel)]="reportFrom" /></div>
            <div class="col-6 col-md-3"><label class="form-label small">To</label><input type="datetime-local" class="form-control form-control-sm" [(ngModel)]="reportTo" /></div>
            <div class="col-6 col-md-2"><label class="form-label small">Activity</label><input class="form-control form-control-sm" [(ngModel)]="reportActivity" /></div>
            <div class="col-6 col-md-2"><label class="form-label small">Tag</label><input class="form-control form-control-sm" [(ngModel)]="reportTag" /></div>
            <div class="col-12 col-md-2 d-flex gap-2">
              <button class="btn-link-soft btn-sm" type="button" (click)="loadReport()">View</button>
              <button class="btn-link-soft btn-sm" type="button" (click)="download('Csv')">CSV</button>
              <button class="btn-link-soft btn-sm" type="button" (click)="download('Xlsx')">Excel</button>
              <button class="btn-link-soft btn-sm" type="button" (click)="download('Pdf')">PDF</button>
            </div>
          </div>
          @if (report(); as r) {
            <div class="row g-3 mt-1">
              <div class="col-md-6"><h3 class="section-title">Activity summary</h3>@for (s of r.activitySummary; track s.key) { <div class="summary-line"><span>{{ s.key }}</span><span>{{ s.numberOfDays }} days / {{ s.numberOfMinutes }} min</span></div> }</div>
              <div class="col-md-6"><h3 class="section-title">Tag summary</h3>@for (s of r.tagSummary; track s.key) { <div class="summary-line"><span>{{ s.key }}</span><span>{{ s.numberOfDays }} days / {{ s.numberOfMinutes }} min</span></div> }</div>
            </div>
          }
          @if (reportError()) { <div class="alert alert-danger py-1 px-2 small mt-2 mb-0">{{ reportError() }}</div> }
        </div>
      }

      <div class="calendar surface">
        <div class="calendar-hours">
          @for (h of hours; track h) { <div>{{ pad(h) }}:00</div> }
        </div>
        <div class="calendar-grid">
          @for (h of hours; track h) { <div class="hour-line"></div> }
          @for (entry of entries(); track entry.id) {
            <button class="time-block" type="button" [style.top.%]="top(entry)" [style.height.%]="height(entry)" (click)="edit(entry)">
              <strong>{{ entry.activity }}</strong>
              <span>{{ time(entry.startTime) }}-{{ time(entry.endTime) }} · {{ entry.minutes }}m</span>
              @if (entry.tag) { <em>{{ entry.tag }}</em> }
            </button>
          }
        </div>
      </div>
      <div class="table-wrap surface mt-3">
        <table class="table-mini"><thead><tr><th>Start</th><th>End</th><th>Activity</th><th>Tag</th><th>Minutes</th><th></th></tr></thead>
        <tbody>@for (entry of entries(); track entry.id) { <tr><td>{{ entry.startTime | date:'short' }}</td><td>{{ entry.endTime | date:'short' }}</td><td>{{ entry.activity }}</td><td>{{ entry.tag || '-' }}</td><td>{{ entry.minutes }}</td><td><button class="icon-btn" (click)="edit(entry)">Edit</button><button class="icon-btn danger" (click)="remove(entry)">Delete</button></td></tr> }</tbody></table>
      </div>
    </section>
  `,
  styles: [`
    .page-title { font-size: 1.5rem; font-weight: 600; }
    .btn-link-soft { background: transparent; border: 1px solid var(--border-strong); color: var(--fg-muted); padding: .3rem .7rem; border-radius: var(--radius-sm); }
    .calendar { display: grid; grid-template-columns: 64px 1fr; min-height: 760px; overflow: hidden; }
    .calendar-hours { border-right: 1px solid var(--border); color: var(--fg-muted); font-size: .78rem; }
    .calendar-hours div { height: 31.66px; padding: .15rem .5rem; border-bottom: 1px solid var(--border); }
    .calendar-grid { position: relative; min-height: 760px; }
    .hour-line { height: 31.66px; border-bottom: 1px solid var(--border); }
    .time-block { position: absolute; left: .5rem; right: .5rem; min-height: 26px; overflow: hidden; text-align: left; border: 1px solid var(--neon-cyan); box-shadow: 0 0 12px var(--neon-cyan-soft); background: var(--surface-2); color: var(--fg); border-radius: var(--radius-sm); padding: .35rem .55rem; display: flex; flex-direction: column; font-size: .8rem; }
    .time-block em { color: var(--neon); font-style: normal; }
    .section-title { font-size: .9rem; font-weight: 600; }
    .summary-line { display: flex; justify-content: space-between; border-bottom: 1px solid var(--border); padding: .25rem 0; font-size: .86rem; }
    .table-wrap { overflow: auto; }
    .table-mini { width: 100%; border-collapse: collapse; font-size: .86rem; }
    .table-mini th, .table-mini td { padding: .5rem .7rem; border-bottom: 1px solid var(--border); }
    .icon-btn { border: 0; background: transparent; color: var(--neon-cyan); margin-right: .4rem; }
    .icon-btn.danger { color: var(--danger); }
  `]
})
export class TimeTrackerComponent {
  private readonly api = inject(TimeTrackerApi);
  private readonly fb = inject(FormBuilder);
  readonly entries = signal<TimeEntry[]>([]);
  readonly formOpen = signal(false);
  readonly saving = signal(false);
  readonly touchedSubmit = signal(false);
  readonly showReport = signal(false);
  readonly report = signal<TimeReport | null>(null);
  readonly reportError = signal<string | null>(null);
  readonly hours = Array.from({ length: 24 }, (_, i) => i);
  day = dateIso();
  filters = { activity: '', tag: '' };
  reportFrom = `${dateIso()}T00:00`;
  reportTo = `${dateIso()}T23:59`;
  reportActivity = '';
  reportTag = '';
  editingId: string | null = null;

  readonly form = this.fb.nonNullable.group({
    startTime: [localInput(), Validators.required],
    endTime: [addHours(1), Validators.required],
    activity: ['', Validators.required],
    tag: [''],
    note: ['']
  });

  readonly validationMessage = computed(() => {
    if (!this.touchedSubmit()) return null;
    const f = this.form.getRawValue();
    if (!f.startTime || !f.endTime || !f.activity.trim()) return 'Start time, end time, and activity are required.';
    if (new Date(f.endTime) <= new Date(f.startTime)) return 'End time must be after start time.';
    return null;
  });

  constructor() { this.refresh(); }
  pad = pad;
  time(v: string): string { return new Date(v).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }); }
  top(e: TimeEntry): number { const d = new Date(e.startTime); return ((d.getHours() * 60 + d.getMinutes()) / 1440) * 100; }
  height(e: TimeEntry): number { return Math.max(3.4, (e.minutes / 1440) * 100); }

  async refresh(): Promise<void> {
    const from = `${this.day}T00:00:00`;
    const to = `${this.day}T23:59:59`;
    const page = await firstValueFrom(this.api.list({ from, to, activity: this.filters.activity || undefined, tag: this.filters.tag || undefined, pageSize: 100 }));
    this.entries.set([...page.items].sort((a, b) => a.startTime.localeCompare(b.startTime)));
  }
  clearFilters(): void { this.filters = { activity: '', tag: '' }; this.refresh(); }
  startCreate(): void { this.editingId = null; this.touchedSubmit.set(false); this.form.reset({ startTime: `${this.day}T09:00`, endTime: `${this.day}T10:00`, activity: '', tag: '', note: '' }); this.formOpen.set(true); }
  edit(e: TimeEntry): void { this.editingId = e.id; this.touchedSubmit.set(false); this.form.reset({ startTime: e.startTime.slice(0, 16), endTime: e.endTime.slice(0, 16), activity: e.activity, tag: e.tag ?? '', note: e.note ?? '' }); this.formOpen.set(true); }
  cancel(): void { this.formOpen.set(false); this.editingId = null; }
  async save(): Promise<void> {
    this.touchedSubmit.set(true);
    if (this.validationMessage()) return;
    this.saving.set(true);
    try {
      const f = this.form.getRawValue();
      const body = { startTime: f.startTime, endTime: f.endTime, activity: f.activity.trim(), tag: f.tag.trim() || null, note: f.note.trim() || null };
      if (this.editingId) await firstValueFrom(this.api.update(this.editingId, body));
      else await firstValueFrom(this.api.create(body));
      this.cancel();
      await this.refresh();
    } finally { this.saving.set(false); }
  }
  async remove(e: TimeEntry): Promise<void> { if (!confirm(`Delete "${e.activity}"?`)) return; await firstValueFrom(this.api.delete(e.id)); await this.refresh(); }
  async loadReport(): Promise<void> {
    this.reportError.set(null);
    try { this.report.set(await firstValueFrom(this.api.report({ from: this.reportFrom, to: this.reportTo, activity: this.reportActivity || undefined, tag: this.reportTag || undefined }))); }
    catch (err: any) { this.reportError.set(err?.error?.message ?? 'Report failed.'); }
  }
  async download(format: ReportFormat): Promise<void> {
    const res = await firstValueFrom(this.api.download({ from: this.reportFrom, to: this.reportTo, activity: this.reportActivity || undefined, tag: this.reportTag || undefined, format }));
    triggerDownload(res.body!, parseFilename(res.headers.get('content-disposition')) ?? `time-report.${format.toLowerCase()}`);
  }
}

function parseFilename(disposition: string | null): string | null {
  const m = disposition ? /filename="?([^";\n]+)"?/i.exec(disposition) : null;
  return m ? m[1] : null;
}
function triggerDownload(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url; a.download = filename; document.body.appendChild(a); a.click(); document.body.removeChild(a);
  URL.revokeObjectURL(url);
}
