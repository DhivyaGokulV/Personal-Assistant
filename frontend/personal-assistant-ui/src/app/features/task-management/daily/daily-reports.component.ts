import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { DailyTasksApi } from './daily-tasks.api';
import {
  DailyDayWiseReport,
  DailyTaskWiseReport,
  ReportFormat,
  ReportKind
} from './daily-reports.models';

function isoOffset(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

@Component({
  selector: 'app-daily-reports',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="reports-panel surface p-3">
      <div class="d-flex flex-wrap align-items-end gap-2 mb-3">
        <div>
          <label class="form-label small text-muted-soft mb-1">From</label>
          <input type="date" class="form-control form-control-sm" [(ngModel)]="from" />
        </div>
        <div>
          <label class="form-label small text-muted-soft mb-1">To</label>
          <input type="date" class="form-control form-control-sm" [(ngModel)]="to" />
        </div>
        <div>
          <label class="form-label small text-muted-soft mb-1">Report</label>
          <select class="form-select form-select-sm" [(ngModel)]="kind">
            <option value="day-wise">Day-wise</option>
            <option value="task-wise">Task-wise</option>
          </select>
        </div>
        <div class="d-flex gap-2 ms-auto">
          <button class="btn-neon btn-sm" type="button" [disabled]="loading()" (click)="loadView()">
            {{ loading() ? 'Loading…' : 'View' }}
          </button>
          <button class="btn-link-soft btn-sm" type="button" [disabled]="downloading()" (click)="download('Csv')">CSV</button>
          <button class="btn-link-soft btn-sm" type="button" [disabled]="downloading()" (click)="download('Xlsx')">Excel</button>
          <button class="btn-link-soft btn-sm" type="button" [disabled]="downloading()" (click)="download('Pdf')">PDF</button>
        </div>
      </div>

      @if (errorMessage()) {
        <div class="alert alert-danger py-2 small mb-2">{{ errorMessage() }}</div>
      }

      @if (kind === 'day-wise' && dayWise(); as r) {
        <div class="report-summary small text-muted-soft mb-2">
          {{ r.rows.length }} rows · {{ r.from }} → {{ r.to }}
        </div>
        @if (r.rows.length === 0) {
          <div class="text-muted-soft small">No data in this range.</div>
        } @else {
          <div class="table-wrap">
            <table class="report-table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Group</th>
                  <th>Task</th>
                  <th>Status</th>
                  <th>Note</th>
                </tr>
              </thead>
              <tbody>
                @for (row of r.rows; track $index) {
                  <tr [class.row-completed]="row.isCompleted">
                    <td>{{ row.date }}</td>
                    <td>{{ row.groupName }}</td>
                    <td>{{ row.taskTitle }}</td>
                    <td>
                      <span class="status-pill" [class.is-done]="row.isCompleted">
                        {{ row.isCompleted ? 'Completed' : 'Not completed' }}
                      </span>
                    </td>
                    <td class="text-muted-soft">{{ row.note }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      }

      @if (kind === 'task-wise' && taskWise(); as r) {
        <div class="report-summary small text-muted-soft mb-2">
          {{ r.rows.length }} tasks · {{ r.from }} → {{ r.to }}
        </div>
        @if (r.rows.length === 0) {
          <div class="text-muted-soft small">No active tasks.</div>
        } @else {
          <div class="table-wrap">
            <table class="report-table">
              <thead>
                <tr>
                  <th>Group</th>
                  <th>Task</th>
                  <th class="text-end">Times Done</th>
                  <th>Last Done On</th>
                </tr>
              </thead>
              <tbody>
                @for (row of r.rows; track $index) {
                  <tr>
                    <td>{{ row.groupName }}</td>
                    <td>{{ row.taskTitle }}</td>
                    <td class="text-end">{{ row.timesDone }}</td>
                    <td>{{ row.lastDoneOn ?? '—' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .reports-panel { border: 1px solid var(--border); }
    .table-wrap { overflow: auto; max-height: 480px; border: 1px solid var(--border); border-radius: var(--radius-sm); }
    .report-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.88rem;
    }
    .report-table thead th {
      position: sticky;
      top: 0;
      background: var(--surface-2);
      color: var(--fg-muted);
      font-weight: 600;
      text-align: left;
      padding: 0.5rem 0.75rem;
      border-bottom: 1px solid var(--border-strong);
      white-space: nowrap;
    }
    .report-table tbody td {
      padding: 0.45rem 0.75rem;
      border-bottom: 1px solid var(--border);
      vertical-align: top;
    }
    .report-table tbody tr:hover td { background: var(--surface-2); }

    .row-completed td:nth-child(3) { text-decoration: line-through; color: var(--fg-muted); }

    .status-pill {
      display: inline-flex;
      padding: 0.1rem 0.55rem;
      border-radius: 999px;
      font-size: 0.75rem;
      border: 1px solid var(--border-strong);
      color: var(--fg-muted);
    }
    .status-pill.is-done { color: var(--success); border-color: var(--success); }

    .btn-link-soft {
      background: transparent;
      border: 1px solid var(--border-strong);
      color: var(--fg-muted);
      padding: 0.3rem 0.7rem;
      border-radius: var(--radius-sm);
      cursor: pointer;
      transition: all 120ms ease;
    }
    .btn-link-soft:hover { color: var(--neon); border-color: var(--neon); }
  `]
})
export class DailyReportsComponent {
  private readonly api = inject(DailyTasksApi);

  from = isoOffset(-30);
  to = isoOffset(0);
  kind: ReportKind = 'day-wise';

  readonly dayWise = signal<DailyDayWiseReport | null>(null);
  readonly taskWise = signal<DailyTaskWiseReport | null>(null);
  readonly loading = signal(false);
  readonly downloading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  async loadView(): Promise<void> {
    this.errorMessage.set(null);
    this.loading.set(true);
    try {
      if (this.kind === 'day-wise') {
        const r = await firstValueFrom(this.api.getDayWiseReport(this.from, this.to));
        this.dayWise.set(r);
      } else {
        const r = await firstValueFrom(this.api.getTaskWiseReport(this.from, this.to));
        this.taskWise.set(r);
      }
    } catch (err: unknown) {
      this.errorMessage.set('Failed to load report. Check the date range.');
    } finally {
      this.loading.set(false);
    }
  }

  async download(format: ReportFormat): Promise<void> {
    this.errorMessage.set(null);
    this.downloading.set(true);
    try {
      const res = await firstValueFrom(this.api.downloadReport(this.kind, this.from, this.to, format));
      const blob = res.body!;
      const filename = parseFilename(res.headers.get('content-disposition'))
        ?? defaultFilename(this.kind, this.from, this.to, format);
      triggerDownload(blob, filename);
    } catch (err: unknown) {
      this.errorMessage.set('Download failed. Try again or check the date range.');
    } finally {
      this.downloading.set(false);
    }
  }
}

function parseFilename(disposition: string | null): string | null {
  if (!disposition) return null;
  const utf8Match = /filename\*=UTF-8''([^;\n]+)/i.exec(disposition);
  if (utf8Match) return decodeURIComponent(utf8Match[1]);
  const match = /filename="?([^";\n]+)"?/i.exec(disposition);
  return match ? match[1] : null;
}

function defaultFilename(kind: ReportKind, from: string, to: string, format: ReportFormat): string {
  const ext = format.toLowerCase() === 'xlsx' ? 'xlsx'
    : format.toLowerCase() === 'pdf' ? 'pdf'
    : 'csv';
  return `daily-${kind}-${from}-to-${to}.${ext}`;
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
