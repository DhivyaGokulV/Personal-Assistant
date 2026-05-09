import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { PeriodicTasksApi } from './periodic-tasks.api';
import { PeriodicReport, ReportFormat } from './periodic-tasks.models';

function isoOffset(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

@Component({
  selector: 'app-periodic-reports',
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

      @if (report(); as r) {
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
                  <th>Next Due On</th>
                </tr>
              </thead>
              <tbody>
                @for (row of r.rows; track $index) {
                  <tr>
                    <td>{{ row.groupName }}</td>
                    <td>{{ row.taskTitle }}</td>
                    <td class="text-end">{{ row.timesDoneInRange }}</td>
                    <td>{{ row.lastDoneOn ?? '—' }}</td>
                    <td>{{ row.nextDueOn ?? '—' }}</td>
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
    .report-table { width: 100%; border-collapse: collapse; font-size: 0.88rem; }
    .report-table thead th {
      position: sticky; top: 0;
      background: var(--surface-2); color: var(--fg-muted);
      font-weight: 600; text-align: left;
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
export class PeriodicReportsComponent {
  private readonly api = inject(PeriodicTasksApi);

  from = isoOffset(-30);
  to = isoOffset(0);

  readonly report = signal<PeriodicReport | null>(null);
  readonly loading = signal(false);
  readonly downloading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  async loadView(): Promise<void> {
    this.errorMessage.set(null);
    this.loading.set(true);
    try {
      const r = await firstValueFrom(this.api.getReport(this.from, this.to));
      this.report.set(r);
    } catch {
      this.errorMessage.set('Failed to load report.');
    } finally {
      this.loading.set(false);
    }
  }

  async download(format: ReportFormat): Promise<void> {
    this.errorMessage.set(null);
    this.downloading.set(true);
    try {
      const res = await firstValueFrom(this.api.downloadReport(this.from, this.to, format));
      const blob = res.body!;
      const filename = parseFilename(res.headers.get('content-disposition'))
        ?? `periodic-tasks-${this.from}-to-${this.to}.${format.toLowerCase()}`;
      triggerDownload(blob, filename);
    } catch {
      this.errorMessage.set('Download failed.');
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
