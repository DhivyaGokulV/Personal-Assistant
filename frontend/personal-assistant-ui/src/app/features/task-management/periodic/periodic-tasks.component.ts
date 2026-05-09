import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { PeriodicTasksApi } from './periodic-tasks.api';
import {
  FrequencyUnit,
  FREQUENCY_UNITS,
  PeriodicGroup,
  PeriodicHistory,
  PeriodicTask
} from './periodic-tasks.models';
import { PeriodicReportsComponent } from './periodic-reports.component';

interface NewGroupForm { name: string; description: string; }
interface EditingGroup { id: string; name: string; description: string; }

interface NewTaskForm {
  groupId: string;
  title: string;
  description: string;
  frequencyValue: number;
  frequencyUnit: FrequencyUnit;
}

interface EditingTask {
  id: string;
  groupId: string;
  title: string;
  description: string;
  status: 1 | 2;
  frequencyValue: number;
  frequencyUnit: FrequencyUnit;
}

interface MarkDoneForm { date: string; note: string; }
interface EditingHistory { id: string; completedOn: string; note: string; }

function todayIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function daysBetween(isoDate: string): number {
  const target = new Date(isoDate + 'T00:00:00');
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const diff = target.getTime() - today.getTime();
  return Math.round(diff / (1000 * 60 * 60 * 24));
}

@Component({
  selector: 'app-periodic-tasks',
  imports: [CommonModule, FormsModule, PeriodicReportsComponent],
  template: `
    <div class="periodic-shell">
      <div class="d-flex flex-wrap align-items-end gap-3 mb-3">
        @if (taskStats(); as s) {
          <div class="counts">
            <span class="count-pill">Active <strong>{{ s.active }}</strong></span>
            <span class="count-pill count-overdue">Overdue <strong>{{ s.overdue }}</strong></span>
            <span class="count-pill count-soon">Due soon <strong>{{ s.dueSoon }}</strong></span>
            <span class="count-pill count-never">Never done <strong>{{ s.never }}</strong></span>
          </div>
        }

        <div class="ms-auto d-flex gap-2">
          <button type="button" class="btn-link-soft btn-sm" (click)="showingReports.update(v => !v)">
            {{ showingReports() ? 'Hide Reports' : 'Reports' }}
          </button>
          <button type="button" class="btn-neon btn-sm" (click)="toggleAddGroup()">
            {{ addingGroup() ? 'Cancel' : '+ Add Group' }}
          </button>
        </div>
      </div>

      @if (showingReports()) {
        <div class="mb-3"><app-periodic-reports /></div>
      }

      @if (addingGroup()) {
        <div class="surface p-3 mb-3">
          <div class="row g-2 align-items-end">
            <div class="col-12 col-md-4">
              <label class="form-label small">Name</label>
              <input class="form-control form-control-sm" [(ngModel)]="newGroup.name" placeholder="e.g. Cleaning" />
            </div>
            <div class="col-12 col-md-6">
              <label class="form-label small">Description</label>
              <input class="form-control form-control-sm" [(ngModel)]="newGroup.description" />
            </div>
            <div class="col-12 col-md-2 d-grid">
              <button class="btn-neon btn-sm" type="button" [disabled]="!newGroup.name.trim() || saving()" (click)="saveNewGroup()">Save</button>
            </div>
          </div>
        </div>
      }

      @if (loading()) {
        <div class="text-muted-soft small">Loading…</div>
      } @else if (groups().length === 0) {
        <div class="surface p-4 text-center text-muted-soft">
          No groups yet. Click <em>Add Group</em> to create one.
        </div>
      } @else {
        @for (g of groups(); track g.id) {
          <article class="group-card neon mb-3">
            @if (editingGroup()?.id === g.id) {
              <div class="row g-2 align-items-end">
                <div class="col-12 col-md-4">
                  <label class="form-label small">Name</label>
                  <input class="form-control form-control-sm" [(ngModel)]="editingGroup()!.name" />
                </div>
                <div class="col-12 col-md-6">
                  <label class="form-label small">Description</label>
                  <input class="form-control form-control-sm" [(ngModel)]="editingGroup()!.description" />
                </div>
                <div class="col-12 col-md-2 d-grid gap-2">
                  <button class="btn-neon btn-sm" [disabled]="!editingGroup()!.name.trim() || saving()" (click)="saveEditGroup()">Save</button>
                  <button class="btn-link-soft btn-sm" type="button" (click)="editingGroup.set(null)">Cancel</button>
                </div>
              </div>
            } @else {
              <header class="group-header">
                <div>
                  <h3 class="group-title">{{ g.name }}</h3>
                  @if (g.description) {
                    <p class="group-desc">{{ g.description }}</p>
                  }
                </div>
                <span class="count-pill ms-auto">{{ tasksInGroup(g.id).length }} task(s)</span>
                <div class="group-actions">
                  <button class="icon-btn" type="button" title="Edit group" (click)="startEditGroup(g)">✎</button>
                  <button class="icon-btn icon-danger" type="button" title="Delete group" (click)="deleteGroup(g)">🗑</button>
                </div>
              </header>
            }

            <ul class="task-list">
              @for (t of tasksInGroup(g.id); track t.id) {
                <li class="task-row">
                  @if (editingTask()?.id === t.id) {
                    <div class="task-edit-form">
                      <div class="row g-2 align-items-end">
                        <div class="col-12 col-md-4">
                          <label class="form-label small">Title</label>
                          <input class="form-control form-control-sm" [(ngModel)]="editingTask()!.title" />
                        </div>
                        <div class="col-12 col-md-3">
                          <label class="form-label small">Description</label>
                          <input class="form-control form-control-sm" [(ngModel)]="editingTask()!.description" />
                        </div>
                        <div class="col-6 col-md-1">
                          <label class="form-label small">Every</label>
                          <input type="number" min="1" class="form-control form-control-sm" [(ngModel)]="editingTask()!.frequencyValue" />
                        </div>
                        <div class="col-6 col-md-2">
                          <label class="form-label small">Unit</label>
                          <select class="form-select form-select-sm" [(ngModel)]="editingTask()!.frequencyUnit">
                            @for (u of frequencyUnits; track u.value) {
                              <option [ngValue]="u.value">{{ u.label }}</option>
                            }
                          </select>
                        </div>
                        <div class="col-6 col-md-1">
                          <label class="form-label small">Status</label>
                          <select class="form-select form-select-sm" [(ngModel)]="editingTask()!.status">
                            <option [ngValue]="1">Active</option>
                            <option [ngValue]="2">Inactive</option>
                          </select>
                        </div>
                        <div class="col-6 col-md-1 d-grid gap-1">
                          <button class="btn-neon btn-sm" [disabled]="!editingTask()!.title.trim() || saving()" (click)="saveEditTask()">Save</button>
                          <button class="btn-link-soft btn-sm" type="button" (click)="editingTask.set(null)">Cancel</button>
                        </div>
                      </div>
                    </div>
                  } @else {
                    <div class="task-main">
                      <div class="task-header">
                        <span class="task-title">{{ t.title }}</span>
                        <span class="frequency-pill">Every {{ t.frequencyValue }} {{ unitLabel(t.frequencyUnit) }}</span>
                        <span class="due-pill" [ngClass]="dueClass(t)">{{ dueLabel(t) }}</span>
                      </div>
                      @if (t.description) {
                        <div class="task-desc">{{ t.description }}</div>
                      }
                      <div class="task-meta small text-muted-soft">
                        Last done: <strong>{{ t.lastDoneOn ?? '—' }}</strong>
                        · Next due: <strong>{{ t.nextDueOn ?? '—' }}</strong>
                        · {{ t.historyCount }} entries
                      </div>

                      @if (markingDoneTaskId() === t.id) {
                        <div class="mark-done-form">
                          <div class="row g-2 align-items-end">
                            <div class="col-6 col-md-3">
                              <label class="form-label small">Date</label>
                              <input type="date" class="form-control form-control-sm" [(ngModel)]="markForm.date" />
                            </div>
                            <div class="col-12 col-md-7">
                              <label class="form-label small">Note <span class="text-subtle">(optional)</span></label>
                              <input class="form-control form-control-sm" [(ngModel)]="markForm.note" />
                            </div>
                            <div class="col-12 col-md-2 d-grid gap-1">
                              <button class="btn-neon btn-sm" [disabled]="!markForm.date || saving()" (click)="saveMarkDone(t)">Save</button>
                              <button class="btn-link-soft btn-sm" type="button" (click)="markingDoneTaskId.set(null)">Cancel</button>
                            </div>
                          </div>
                        </div>
                      }

                      @if (expandedTaskId() === t.id) {
                        <div class="history-block">
                          <div class="history-header">History</div>
                          @if (loadingHistoryFor() === t.id) {
                            <div class="text-muted-soft small">Loading history…</div>
                          } @else if ((historyByTask()[t.id] ?? []).length === 0) {
                            <div class="text-muted-soft small">No history yet.</div>
                          } @else {
                            <ul class="history-list">
                              @for (h of historyByTask()[t.id] ?? []; track h.id) {
                                <li class="history-row">
                                  @if (editingHistory()?.id === h.id) {
                                    <div class="row g-2 align-items-end w-100">
                                      <div class="col-6 col-md-3">
                                        <input type="date" class="form-control form-control-sm" [(ngModel)]="editingHistory()!.completedOn" />
                                      </div>
                                      <div class="col-12 col-md-6">
                                        <input class="form-control form-control-sm" [(ngModel)]="editingHistory()!.note" />
                                      </div>
                                      <div class="col-12 col-md-3 d-grid gap-1">
                                        <button class="btn-neon btn-sm" [disabled]="!editingHistory()!.completedOn || saving()" (click)="saveEditHistory(t)">Save</button>
                                        <button class="btn-link-soft btn-sm" type="button" (click)="editingHistory.set(null)">Cancel</button>
                                      </div>
                                    </div>
                                  } @else {
                                    <div class="history-meta">
                                      <span class="history-date">{{ h.completedOn }}</span>
                                      @if (h.note) {
                                        <span class="history-note text-muted-soft">— {{ h.note }}</span>
                                      }
                                    </div>
                                    <div class="history-actions">
                                      <button class="icon-btn" type="button" title="Edit" (click)="startEditHistory(h)">✎</button>
                                      <button class="icon-btn icon-danger" type="button" title="Delete" (click)="deleteHistory(t, h)">🗑</button>
                                    </div>
                                  }
                                </li>
                              }
                            </ul>
                          }
                        </div>
                      }
                    </div>
                    <div class="task-actions">
                      <button class="btn-neon btn-sm" type="button" (click)="startMarkDone(t)">Mark Done</button>
                      <button class="btn-link-soft btn-sm" type="button" (click)="toggleHistory(t)">
                        {{ expandedTaskId() === t.id ? 'Hide history' : 'History (' + t.historyCount + ')' }}
                      </button>
                      <button class="icon-btn" type="button" title="Edit task" (click)="startEditTask(t)">✎</button>
                      <button class="icon-btn icon-danger" type="button" title="Delete task" (click)="deleteTask(t)">🗑</button>
                    </div>
                  }
                </li>
              }
            </ul>

            @if (addingTaskInGroup() === g.id) {
              <div class="add-task-form">
                <div class="row g-2 align-items-end">
                  <div class="col-12 col-md-3">
                    <label class="form-label small">Title</label>
                    <input class="form-control form-control-sm" [(ngModel)]="newTask.title" />
                  </div>
                  <div class="col-12 col-md-3">
                    <label class="form-label small">Description</label>
                    <input class="form-control form-control-sm" [(ngModel)]="newTask.description" />
                  </div>
                  <div class="col-6 col-md-2">
                    <label class="form-label small">Every</label>
                    <input type="number" min="1" class="form-control form-control-sm" [(ngModel)]="newTask.frequencyValue" />
                  </div>
                  <div class="col-6 col-md-2">
                    <label class="form-label small">Unit</label>
                    <select class="form-select form-select-sm" [(ngModel)]="newTask.frequencyUnit">
                      @for (u of frequencyUnits; track u.value) {
                        <option [ngValue]="u.value">{{ u.label }}</option>
                      }
                    </select>
                  </div>
                  <div class="col-12 col-md-2 d-grid gap-1">
                    <button class="btn-neon btn-sm" [disabled]="!newTask.title.trim() || saving()" (click)="saveNewTask(g)">Save</button>
                    <button class="btn-link-soft btn-sm" type="button" (click)="addingTaskInGroup.set(null)">Cancel</button>
                  </div>
                </div>
              </div>
            } @else {
              <button class="btn-link-soft btn-sm add-task-btn" type="button" (click)="startAddTask(g)">+ Add Task</button>
            }
          </article>
        }
      }
    </div>
  `,
  styles: [`
    .counts { display: inline-flex; gap: 0.5rem; flex-wrap: wrap; }
    .count-pill {
      display: inline-flex;
      gap: 0.4rem;
      align-items: center;
      padding: 0.25rem 0.7rem;
      border-radius: 999px;
      font-size: 0.8rem;
      color: var(--fg-muted);
      background: var(--surface);
      border: 1px solid var(--border-strong);
    }
    .count-pill strong { color: var(--fg); font-weight: 600; }
    .count-overdue { color: var(--danger); border-color: var(--danger); }
    .count-soon { color: var(--warning); border-color: var(--warning); }
    .count-never { color: var(--fg-subtle); }

    .group-card { padding: 1rem; }
    .group-header {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      padding-bottom: 0.5rem;
      margin-bottom: 0.5rem;
      border-bottom: 1px solid var(--border);
    }
    .group-title { font-size: 1.05rem; font-weight: 600; margin: 0; }
    .group-desc { font-size: 0.85rem; color: var(--fg-muted); margin: 0.15rem 0 0; }
    .group-actions { display: flex; gap: 0.25rem; }

    .task-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.5rem; }
    .task-row {
      display: flex;
      gap: 0.75rem;
      padding: 0.75rem;
      border: 1px solid var(--border);
      border-radius: var(--radius-sm);
      background: var(--surface-2);
    }
    .task-main { flex: 1; min-width: 0; }
    .task-edit-form { flex: 1; }
    .task-header { display: flex; gap: 0.5rem; align-items: center; flex-wrap: wrap; }
    .task-title { font-weight: 600; }
    .task-desc { font-size: 0.85rem; color: var(--fg-muted); margin-top: 0.2rem; }
    .task-meta { margin-top: 0.25rem; }
    .task-actions { display: flex; gap: 0.25rem; align-items: flex-start; flex-wrap: wrap; }

    .frequency-pill {
      display: inline-flex;
      padding: 0.1rem 0.55rem;
      border-radius: 999px;
      font-size: 0.75rem;
      border: 1px solid var(--border-strong);
      color: var(--fg-muted);
    }
    .due-pill {
      display: inline-flex;
      padding: 0.1rem 0.55rem;
      border-radius: 999px;
      font-size: 0.75rem;
      font-weight: 500;
      border: 1px solid var(--border-strong);
    }
    .due-overdue { color: var(--danger); border-color: var(--danger); }
    .due-soon { color: var(--warning); border-color: var(--warning); }
    .due-ok { color: var(--success); border-color: var(--success); }
    .due-never { color: var(--fg-subtle); }

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
      background: transparent; border: 1px solid var(--border-strong);
      color: var(--fg-muted); padding: 0.3rem 0.7rem;
      border-radius: var(--radius-sm); cursor: pointer;
      transition: all 120ms ease;
    }
    .btn-link-soft:hover { color: var(--neon); border-color: var(--neon); }

    .mark-done-form, .add-task-form {
      margin-top: 0.5rem; padding: 0.5rem;
      border-radius: var(--radius-sm);
      background: var(--surface);
      border: 1px solid var(--border);
    }
    .add-task-btn { margin-top: 0.5rem; }

    .history-block {
      margin-top: 0.6rem; padding: 0.5rem 0.75rem;
      border-left: 2px solid var(--neon-cyan);
      background: var(--surface);
      border-radius: var(--radius-sm);
    }
    .history-header {
      font-size: 0.8rem; color: var(--fg-muted);
      text-transform: uppercase; letter-spacing: 0.05em;
      margin-bottom: 0.4rem;
    }
    .history-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.3rem; }
    .history-row {
      display: flex; gap: 0.5rem; align-items: center;
      padding: 0.25rem 0;
      border-bottom: 1px solid var(--border);
    }
    .history-row:last-child { border-bottom: none; }
    .history-meta { flex: 1; }
    .history-date { font-weight: 500; }
    .history-actions { display: flex; gap: 0.2rem; }
  `]
})
export class PeriodicTasksComponent {
  private readonly api = inject(PeriodicTasksApi);

  readonly frequencyUnits = FREQUENCY_UNITS;

  readonly groups = signal<PeriodicGroup[]>([]);
  readonly tasks = signal<PeriodicTask[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);

  readonly addingGroup = signal(false);
  readonly editingGroup = signal<EditingGroup | null>(null);
  readonly addingTaskInGroup = signal<string | null>(null);
  readonly editingTask = signal<EditingTask | null>(null);

  readonly markingDoneTaskId = signal<string | null>(null);
  readonly expandedTaskId = signal<string | null>(null);
  readonly loadingHistoryFor = signal<string | null>(null);
  readonly historyByTask = signal<Record<string, PeriodicHistory[]>>({});
  readonly editingHistory = signal<EditingHistory | null>(null);

  readonly showingReports = signal(false);

  readonly taskStats = computed(() => {
    const ts = this.tasks();
    const active = ts.length;
    let overdue = 0, dueSoon = 0, never = 0;
    for (const t of ts) {
      if (!t.nextDueOn) { never++; continue; }
      const days = daysBetween(t.nextDueOn);
      if (days < 0) overdue++;
      else if (days <= 3) dueSoon++;
    }
    return { active, overdue, dueSoon, never };
  });

  newGroup: NewGroupForm = { name: '', description: '' };
  newTask: NewTaskForm = { groupId: '', title: '', description: '', frequencyValue: 7, frequencyUnit: 1 };
  markForm: MarkDoneForm = { date: todayIso(), note: '' };

  constructor() {
    this.refresh();
  }

  async refresh(): Promise<void> {
    this.loading.set(true);
    try {
      const [groups, tasks] = await Promise.all([
        firstValueFrom(this.api.getGroups()),
        firstValueFrom(this.api.getTasks(false))
      ]);
      this.groups.set(groups);
      this.tasks.set(tasks);
    } finally {
      this.loading.set(false);
    }
  }

  tasksInGroup(groupId: string): PeriodicTask[] {
    return this.tasks().filter(t => t.groupId === groupId);
  }

  unitLabel(unit: FrequencyUnit): string {
    return FREQUENCY_UNITS.find(u => u.value === unit)?.label ?? '';
  }

  dueLabel(t: PeriodicTask): string {
    if (!t.nextDueOn) return 'Never done';
    const days = daysBetween(t.nextDueOn);
    if (days < 0) return `Overdue ${-days}d`;
    if (days === 0) return 'Due today';
    if (days === 1) return 'Due tomorrow';
    return `Due in ${days}d`;
  }

  dueClass(t: PeriodicTask): string {
    if (!t.nextDueOn) return 'due-never';
    const days = daysBetween(t.nextDueOn);
    if (days < 0) return 'due-overdue';
    if (days <= 3) return 'due-soon';
    return 'due-ok';
  }

  toggleAddGroup(): void {
    this.addingGroup.update(v => !v);
    this.newGroup = { name: '', description: '' };
  }

  async saveNewGroup(): Promise<void> {
    const name = this.newGroup.name.trim();
    if (!name) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.createGroup({ name, description: this.newGroup.description.trim() || null }));
      this.addingGroup.set(false);
      this.newGroup = { name: '', description: '' };
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  startEditGroup(g: PeriodicGroup): void {
    this.editingGroup.set({ id: g.id, name: g.name, description: g.description ?? '' });
  }

  async saveEditGroup(): Promise<void> {
    const g = this.editingGroup();
    if (!g || !g.name.trim()) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.updateGroup(g.id, { name: g.name.trim(), description: g.description.trim() || null }));
      this.editingGroup.set(null);
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  async deleteGroup(g: PeriodicGroup): Promise<void> {
    if (!confirm(`Delete group "${g.name}" and all its tasks?`)) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.deleteGroup(g.id));
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  startAddTask(g: PeriodicGroup): void {
    this.addingTaskInGroup.set(g.id);
    this.newTask = { groupId: g.id, title: '', description: '', frequencyValue: 7, frequencyUnit: 1 };
  }

  async saveNewTask(g: PeriodicGroup): Promise<void> {
    if (!this.newTask.title.trim()) return;
    if (this.newTask.frequencyValue <= 0) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.createTask({
        groupId: g.id,
        title: this.newTask.title.trim(),
        description: this.newTask.description.trim() || null,
        status: 1,
        frequencyValue: this.newTask.frequencyValue,
        frequencyUnit: this.newTask.frequencyUnit
      }));
      this.addingTaskInGroup.set(null);
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  startEditTask(t: PeriodicTask): void {
    this.editingTask.set({
      id: t.id,
      groupId: t.groupId,
      title: t.title,
      description: t.description ?? '',
      status: t.status,
      frequencyValue: t.frequencyValue,
      frequencyUnit: t.frequencyUnit
    });
  }

  async saveEditTask(): Promise<void> {
    const t = this.editingTask();
    if (!t || !t.title.trim() || t.frequencyValue <= 0) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.updateTask(t.id, {
        groupId: t.groupId,
        title: t.title.trim(),
        description: t.description.trim() || null,
        status: t.status,
        frequencyValue: t.frequencyValue,
        frequencyUnit: t.frequencyUnit
      }));
      this.editingTask.set(null);
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  async deleteTask(t: PeriodicTask): Promise<void> {
    if (!confirm(`Delete task "${t.title}" and all its history?`)) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.deleteTask(t.id));
      this.cleanupTaskState(t.id);
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  startMarkDone(t: PeriodicTask): void {
    this.markingDoneTaskId.set(t.id);
    this.markForm = { date: todayIso(), note: '' };
  }

  async saveMarkDone(t: PeriodicTask): Promise<void> {
    if (!this.markForm.date) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.addHistory(t.id, {
        completedOn: this.markForm.date,
        note: this.markForm.note.trim() || null
      }));
      this.markingDoneTaskId.set(null);
      if (this.expandedTaskId() === t.id) {
        await this.loadHistory(t.id);
      }
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  async toggleHistory(t: PeriodicTask): Promise<void> {
    if (this.expandedTaskId() === t.id) {
      this.expandedTaskId.set(null);
      this.editingHistory.set(null);
      return;
    }
    this.expandedTaskId.set(t.id);
    if (!this.historyByTask()[t.id]) {
      await this.loadHistory(t.id);
    }
  }

  private async loadHistory(taskId: string): Promise<void> {
    this.loadingHistoryFor.set(taskId);
    try {
      const detail = await firstValueFrom(this.api.getTask(taskId));
      this.historyByTask.update(m => ({ ...m, [taskId]: detail.history }));
    } finally {
      this.loadingHistoryFor.set(null);
    }
  }

  startEditHistory(h: PeriodicHistory): void {
    this.editingHistory.set({ id: h.id, completedOn: h.completedOn, note: h.note ?? '' });
  }

  async saveEditHistory(t: PeriodicTask): Promise<void> {
    const h = this.editingHistory();
    if (!h) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.updateHistory(t.id, h.id, {
        completedOn: h.completedOn,
        note: h.note.trim() || null
      }));
      this.editingHistory.set(null);
      await this.loadHistory(t.id);
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  async deleteHistory(t: PeriodicTask, h: PeriodicHistory): Promise<void> {
    if (!confirm(`Delete history entry for ${h.completedOn}?`)) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.deleteHistory(t.id, h.id));
      await this.loadHistory(t.id);
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  private cleanupTaskState(taskId: string): void {
    if (this.expandedTaskId() === taskId) this.expandedTaskId.set(null);
    if (this.editingTask()?.id === taskId) this.editingTask.set(null);
    if (this.markingDoneTaskId() === taskId) this.markingDoneTaskId.set(null);
    this.historyByTask.update(m => {
      const next = { ...m };
      delete next[taskId];
      return next;
    });
  }
}
