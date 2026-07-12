import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { TodoApi } from './todo.api';
import {
  SortOrder,
  TODO_STATUSES,
  TODO_STATUS_BY_ID,
  Todo,
  TodoSort,
  TodoStatus,
  TodoSummary
} from './todo.models';
import { TodoReportsComponent } from './todo-reports.component';

interface NewForm {
  title: string;
  description: string;
  deadline: string;
  status: TodoStatus;
}

interface EditingForm {
  id: string;
  title: string;
  description: string;
  deadline: string;
  status: TodoStatus;
  completedOn: string;
  statusNote: string;
}

function todayIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

@Component({
  selector: 'app-todo-list',
  imports: [CommonModule, FormsModule, TodoReportsComponent],
  template: `
    <div class="todo-shell">
      <div class="d-flex flex-wrap align-items-end gap-3 mb-3">
        @if (summary(); as s) {
          <div class="counts">
            <span class="count-pill"><strong>{{ s.total }}</strong> total</span>
            @for (sc of statusPills(s); track sc.status) {
              <span class="count-pill" [ngClass]="sc.tone">{{ sc.label }} <strong>{{ sc.count }}</strong></span>
            }
          </div>
        }
        <div class="ms-auto d-flex gap-2">
          <button type="button" class="btn-link-soft btn-sm" (click)="showingReports.update(v => !v)">
            {{ showingReports() ? 'Hide Reports' : 'Reports' }}
          </button>
          <button type="button" class="btn-neon btn-sm" (click)="toggleAdd()">
            {{ adding() ? 'Cancel' : '+ Add Task' }}
          </button>
        </div>
      </div>

      @if (showingReports()) {
        <div class="mb-3"><app-todo-reports /></div>
      }

      @if (adding()) {
        <div class="surface p-3 mb-3">
          <div class="row g-2 align-items-end">
            <div class="col-12 col-md-3">
              <label class="form-label small">Title</label>
              <input class="form-control form-control-sm" [(ngModel)]="newItem.title" />
            </div>
            <div class="col-12 col-md-4">
              <label class="form-label small">Description</label>
              <input class="form-control form-control-sm" [(ngModel)]="newItem.description" />
            </div>
            <div class="col-6 col-md-2">
              <label class="form-label small">Deadline</label>
              <input type="date" class="form-control form-control-sm" [(ngModel)]="newItem.deadline" />
            </div>
            <div class="col-6 col-md-2">
              <label class="form-label small">Status</label>
              <select class="form-select form-select-sm" [(ngModel)]="newItem.status">
                @for (s of createStatuses; track s.value) {
                  <option [ngValue]="s.value">{{ s.label }}</option>
                }
              </select>
            </div>
            <div class="col-12 col-md-1 d-grid">
              <button class="btn-neon btn-sm" type="button" [disabled]="!newItem.title.trim() || saving()" (click)="saveNew()">Save</button>
            </div>
          </div>
        </div>
      }

      <div class="d-flex flex-wrap gap-2 mb-2 align-items-center">
        <label class="form-label small text-muted-soft mb-0">Filter:</label>
        <select class="form-select form-select-sm w-auto" [ngModel]="filterStatus()" (ngModelChange)="onFilterChange($event)">
          <option [ngValue]="null">All</option>
          @for (s of statuses; track s.value) {
            <option [ngValue]="s.value">{{ s.label }}</option>
          }
        </select>

        <label class="form-label small text-muted-soft mb-0 ms-3">Sort by:</label>
        <select class="form-select form-select-sm w-auto" [ngModel]="sortBy()" (ngModelChange)="onSortByChange($event)">
          <option value="AddedDate">Added date</option>
          <option value="Deadline">Deadline</option>
          <option value="DaysLeft">Days left</option>
          <option value="Status">Status</option>
        </select>
        <button class="btn-link-soft btn-sm" type="button" (click)="toggleOrder()">
          {{ order() === 'Asc' ? '↑ Asc' : '↓ Desc' }}
        </button>
      </div>

      @if (loading()) {
        <div class="text-muted-soft small">Loading…</div>
      } @else if (items().length === 0) {
        <div class="surface p-4 text-center text-muted-soft">
          No tasks yet. Click <em>Add Task</em> to create one.
        </div>
      } @else {
        <div class="table-wrap surface">
          <table class="todo-table">
            <thead>
              <tr>
                <th>Task</th>
                <th>Added</th>
                <th>Deadline</th>
                <th class="text-end">Days Left</th>
                <th>Status</th>
                <th>Completion</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (t of items(); track t.id) {
                @if (editingItem()?.id === t.id) {
                  <tr class="editing-row">
                    <td colspan="7">
                      <div class="row g-2 align-items-end">
                        <div class="col-12 col-md-3">
                          <label class="form-label small">Title</label>
                          <input class="form-control form-control-sm" [(ngModel)]="editingItem()!.title" />
                        </div>
                        <div class="col-12 col-md-3">
                          <label class="form-label small">Description</label>
                          <input class="form-control form-control-sm" [(ngModel)]="editingItem()!.description" />
                        </div>
                        <div class="col-6 col-md-2">
                          <label class="form-label small">Deadline</label>
                          <input type="date" class="form-control form-control-sm" [(ngModel)]="editingItem()!.deadline" />
                        </div>
                        <div class="col-6 col-md-2">
                          <label class="form-label small">Status</label>
                          <select class="form-select form-select-sm" [(ngModel)]="editingItem()!.status">
                            @for (s of statuses; track s.value) {
                              <option [ngValue]="s.value">{{ s.label }}</option>
                            }
                          </select>
                        </div>
                        <div class="col-12 col-md-2 d-grid gap-1">
                          <button class="btn-neon btn-sm" [disabled]="!editingItem()!.title.trim() || saving()" (click)="saveEdit()">Save</button>
                          <button class="btn-link-soft btn-sm" type="button" (click)="editingItem.set(null)">Cancel</button>
                        </div>
                        @if (editingItem()!.status === 5) {
                          <div class="col-6 col-md-3">
                            <label class="form-label small">Completed on</label>
                            <input type="date" class="form-control form-control-sm" [(ngModel)]="editingItem()!.completedOn" />
                          </div>
                          <div class="col-12 col-md-9">
                            <label class="form-label small">Status note</label>
                            <input class="form-control form-control-sm" [(ngModel)]="editingItem()!.statusNote" />
                          </div>
                        }
                      </div>
                    </td>
                  </tr>
                } @else {
                  <tr>
                    <td>
                      <div class="task-title">{{ t.title }}</div>
                      @if (t.description) {
                        <div class="task-desc small text-muted-soft">{{ t.description }}</div>
                      }
                    </td>
                    <td>{{ t.addedDate }}</td>
                    <td>{{ t.deadline ?? '—' }}</td>
                    <td class="text-end" [ngClass]="daysLeftClass(t)">
                      {{ t.daysLeft === null ? '—' : t.daysLeft }}
                    </td>
                    <td>
                      <span class="status-pill" [ngClass]="statusTone(t.status)">{{ statusLabel(t.status) }}</span>
                    </td>
                    <td>
                      @if (t.status === 5) {
                        <div class="small">{{ t.completedOn ?? '—' }}</div>
                        @if (t.statusNote) {
                          <div class="small text-muted-soft">{{ t.statusNote }}</div>
                        }
                      } @else {
                        <span class="text-subtle">—</span>
                      }
                    </td>
                    <td class="actions">
                      <button class="icon-btn" type="button" title="Edit" (click)="startEdit(t)">✎</button>
                      <button class="icon-btn icon-danger" type="button" title="Delete" (click)="deleteItem(t)">🗑</button>
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
  styles: [`
    .counts { display: inline-flex; gap: 0.5rem; flex-wrap: wrap; }
    .count-pill {
      display: inline-flex; gap: 0.4rem; align-items: center;
      padding: 0.25rem 0.7rem; border-radius: 999px;
      font-size: 0.8rem; color: var(--fg-muted);
      background: var(--surface);
      border: 1px solid var(--border-strong);
    }
    .count-pill strong { color: var(--fg); font-weight: 600; }

    .tone-amber  { color: var(--warning); border-color: var(--warning); }
    .tone-grey   { color: var(--fg-subtle); }
    .tone-cyan   { color: var(--neon-cyan); border-color: var(--neon-cyan); }
    .tone-violet { color: var(--primary); border-color: var(--primary); }
    .tone-green  { color: var(--success); border-color: var(--success); }
    .tone-red    { color: var(--danger); border-color: var(--danger); }

    .table-wrap {
      overflow: auto;
      border: 1px solid var(--border);
      border-radius: var(--radius-md);
    }
    .todo-table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
    .todo-table thead th {
      position: sticky; top: 0;
      background: var(--surface-2); color: var(--fg-muted);
      font-weight: 600; text-align: left;
      padding: 0.6rem 0.85rem;
      border-bottom: 1px solid var(--border-strong);
      white-space: nowrap;
    }
    .todo-table tbody td {
      padding: 0.55rem 0.85rem;
      border-bottom: 1px solid var(--border);
      vertical-align: top;
    }
    .todo-table tbody tr:hover td { background: var(--surface-2); }
    .editing-row td { background: var(--surface); }
    .actions { white-space: nowrap; }

    .task-title { font-weight: 500; }

    .status-pill {
      display: inline-flex;
      padding: 0.1rem 0.6rem;
      border-radius: 999px;
      font-size: 0.75rem;
      border: 1px solid var(--border-strong);
    }

    .days-overdue { color: var(--danger); font-weight: 600; }
    .days-soon { color: var(--warning); font-weight: 600; }

    .icon-btn {
      width: 28px; height: 28px;
      border-radius: var(--radius-sm);
      background: transparent; border: 1px solid transparent;
      color: var(--fg-muted); cursor: pointer;
      transition: all 120ms ease; font-size: 0.9rem;
    }
    .icon-btn:hover { color: var(--fg); border-color: var(--border-strong); }
    .icon-danger:hover { color: var(--danger); border-color: var(--danger); }

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
export class TodoListComponent {
  private readonly api = inject(TodoApi);

  readonly statuses = TODO_STATUSES;
  readonly createStatuses = TODO_STATUSES.filter(s => s.value !== 5 && s.value !== 6);

  readonly items = signal<Todo[]>([]);
  readonly summary = signal<TodoSummary | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);

  readonly filterStatus = signal<TodoStatus | null>(null);
  readonly sortBy = signal<TodoSort>('AddedDate');
  readonly order = signal<SortOrder>('Desc');

  readonly adding = signal(false);
  readonly editingItem = signal<EditingForm | null>(null);
  readonly showingReports = signal(false);

  newItem: NewForm = { title: '', description: '', deadline: '', status: 2 };

  constructor() {
    this.refresh();
  }

  async refresh(): Promise<void> {
    this.loading.set(true);
    try {
      const [items, summary] = await Promise.all([
        firstValueFrom(this.api.list({
          status: this.filterStatus() ?? undefined,
          sortBy: this.sortBy(),
          order: this.order()
        })),
        firstValueFrom(this.api.summary())
      ]);
      this.items.set(items);
      this.summary.set(summary);
    } finally {
      this.loading.set(false);
    }
  }

  statusPills(s: TodoSummary): { status: TodoStatus; label: string; tone: string; count: number }[] {
    return TODO_STATUSES.map(meta => {
      const found = s.byStatus.find(x => x.status === meta.value);
      return {
        status: meta.value,
        label: meta.label,
        tone: meta.tone,
        count: found?.count ?? 0
      };
    });
  }

  statusLabel(status: TodoStatus): string { return TODO_STATUS_BY_ID[status]?.label ?? '?'; }
  statusTone(status: TodoStatus): string { return TODO_STATUS_BY_ID[status]?.tone ?? ''; }

  daysLeftClass(t: Todo): string {
    if (t.daysLeft === null || t.status === 5 || t.status === 6) return '';
    if (t.daysLeft < 0) return 'days-overdue';
    if (t.daysLeft <= 3) return 'days-soon';
    return '';
  }

  toggleAdd(): void {
    this.adding.update(v => !v);
    this.newItem = { title: '', description: '', deadline: '', status: 2 };
  }

  async saveNew(): Promise<void> {
    if (!this.newItem.title.trim()) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.create({
        title: this.newItem.title.trim(),
        description: this.newItem.description.trim() || null,
        deadline: this.newItem.deadline || null,
        status: this.newItem.status
      }));
      this.adding.set(false);
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  startEdit(t: Todo): void {
    this.editingItem.set({
      id: t.id,
      title: t.title,
      description: t.description ?? '',
      deadline: t.deadline ?? '',
      status: t.status,
      completedOn: t.completedOn ?? '',
      statusNote: t.statusNote ?? ''
    });
  }

  async saveEdit(): Promise<void> {
    const e = this.editingItem();
    if (!e || !e.title.trim()) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.update(e.id, {
        title: e.title.trim(),
        description: e.description.trim() || null,
        deadline: e.deadline || null,
        status: e.status,
        completedOn: e.completedOn || null,
        statusNote: e.statusNote.trim() || null
      }));
      this.editingItem.set(null);
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  async deleteItem(t: Todo): Promise<void> {
    if (!confirm(`Delete task "${t.title}"?`)) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.delete(t.id));
      if (this.editingItem()?.id === t.id) this.editingItem.set(null);
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  onFilterChange(value: TodoStatus | null): void {
    this.filterStatus.set(value);
    this.refresh();
  }

  onSortByChange(value: TodoSort): void {
    this.sortBy.set(value);
    this.refresh();
  }

  toggleOrder(): void {
    this.order.update(o => o === 'Asc' ? 'Desc' : 'Asc');
    this.refresh();
  }
}
