import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { DailyTasksApi } from './daily-tasks.api';
import { DailyReportsComponent } from './daily-reports.component';
import {
  DailyByDateView,
  DailyGroupView,
  DailyTaskWithCompletion,
  TaskActiveStatus
} from './daily-tasks.models';

interface NewGroupForm { name: string; description: string; }
interface NewTaskForm { title: string; description: string; }
interface EditingTask {
  id: string;
  groupId: string;
  title: string;
  description: string;
  status: TaskActiveStatus;
}
interface EditingGroup {
  id: string;
  name: string;
  description: string;
}

function todayIso(): string {
  const d = new Date();
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}

@Component({
  selector: 'app-daily-tasks',
  imports: [CommonModule, FormsModule, DailyReportsComponent],
  template: `
    <div class="daily-shell">
      <div class="d-flex flex-wrap align-items-end gap-3 mb-3">
        <div>
          <label class="form-label small text-muted-soft mb-1">Date</label>
          <input type="date" class="form-control form-control-sm" [ngModel]="date()" (ngModelChange)="onDateChange($event)" />
        </div>

        @if (view(); as v) {
          <div class="counts ms-md-3">
            <span class="count-pill">Total <strong>{{ v.totals.total }}</strong></span>
            <span class="count-pill count-completed">Done <strong>{{ v.totals.completed }}</strong></span>
            <span class="count-pill count-pending">Pending <strong>{{ v.totals.notCompleted }}</strong></span>
          </div>
        }

        <div class="ms-auto d-flex gap-2">
          <button type="button" class="btn-link-soft btn-sm" (click)="toggleReports()">
            {{ showingReports() ? 'Hide Reports' : 'Reports' }}
          </button>
          <button type="button" class="btn-neon btn-sm" (click)="toggleAddGroup()">
            {{ addingGroup() ? 'Cancel' : '+ Add Group' }}
          </button>
        </div>
      </div>

      @if (showingReports()) {
        <div class="mb-3">
          <app-daily-reports />
        </div>
      }

      @if (addingGroup()) {
        <div class="surface p-3 mb-3">
          <div class="row g-2 align-items-end">
            <div class="col-12 col-md-4">
              <label class="form-label small">Name</label>
              <input class="form-control form-control-sm" [(ngModel)]="newGroup.name" placeholder="e.g. Morning Routine" />
            </div>
            <div class="col-12 col-md-6">
              <label class="form-label small">Description <span class="text-subtle">(optional)</span></label>
              <input class="form-control form-control-sm" [(ngModel)]="newGroup.description" />
            </div>
            <div class="col-12 col-md-2 d-grid">
              <button type="button" class="btn-neon btn-sm" [disabled]="!newGroup.name.trim() || saving()" (click)="saveNewGroup()">Save</button>
            </div>
          </div>
        </div>
      }

      @if (loading()) {
        <div class="text-muted-soft small">Loading…</div>
      } @else if (view()?.groups?.length === 0) {
        <div class="surface p-4 text-center text-muted-soft">
          No groups yet. Click <em>Add Group</em> to create one.
        </div>
      } @else {
        @for (g of view()?.groups ?? []; track g.id; let groupIndex = $index) {
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
                  <button class="btn-link-soft btn-sm" type="button" (click)="cancelEditGroup()">Cancel</button>
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
                <div class="group-counts">
                  <span class="count-pill count-completed">{{ g.counts.completed }}/{{ g.counts.total }}</span>
                </div>
                <div class="group-actions">
                  <button class="icon-btn" type="button" title="Move group up" [disabled]="groupIndex === 0 || saving()" (click)="moveGroup(groupIndex, -1)">Up</button>
                  <button class="icon-btn" type="button" title="Move group down" [disabled]="groupIndex === (view()?.groups?.length ?? 0) - 1 || saving()" (click)="moveGroup(groupIndex, 1)">Dn</button>
                  <button class="icon-btn" type="button" title="Edit group" (click)="startEditGroup(g)">✎</button>
                  <button class="icon-btn icon-danger" type="button" title="Delete group" (click)="deleteGroup(g)">🗑</button>
                </div>
              </header>
            }

            <ul class="task-list">
              @for (t of g.tasks; track t.id; let taskIndex = $index) {
                <li class="task-row" [class.task-done]="t.completion?.isCompleted">
                  @if (editingTask()?.id === t.id) {
                    <div class="task-edit-form">
                      <div class="row g-2 align-items-end">
                        <div class="col-12 col-md-4">
                          <label class="form-label small">Title</label>
                          <input class="form-control form-control-sm" [(ngModel)]="editingTask()!.title" />
                        </div>
                        <div class="col-12 col-md-4">
                          <label class="form-label small">Description</label>
                          <input class="form-control form-control-sm" [(ngModel)]="editingTask()!.description" />
                        </div>
                        <div class="col-6 col-md-2">
                          <label class="form-label small">Status</label>
                          <select class="form-select form-select-sm" [(ngModel)]="editingTask()!.status">
                            <option [ngValue]="1">Active</option>
                            <option [ngValue]="2">Inactive</option>
                          </select>
                        </div>
                        <div class="col-6 col-md-2 d-grid gap-1">
                          <button class="btn-neon btn-sm" [disabled]="!editingTask()!.title.trim() || saving()" (click)="saveEditTask()">Save</button>
                          <button class="btn-link-soft btn-sm" type="button" (click)="cancelEditTask()">Cancel</button>
                        </div>
                      </div>
                    </div>
                  } @else {
                    <input type="checkbox"
                           class="form-check-input task-check"
                           [checked]="t.completion?.isCompleted ?? false"
                           (change)="toggleCompleted(t, $event)" />
                    <div class="task-main">
                      <div class="task-title">{{ t.title }}</div>
                      @if (t.description) {
                        <div class="task-desc">{{ t.description }}</div>
                      }
                      <input class="form-control form-control-sm task-note"
                             placeholder="Add a note for today…"
                             [ngModel]="t.completion?.note ?? ''"
                             (blur)="saveNoteOnBlur(t, $event)" />
                    </div>
                    <div class="task-actions">
                      <button class="icon-btn" type="button" title="Move task up" [disabled]="taskIndex === 0 || saving()" (click)="moveTask(g, taskIndex, -1)">Up</button>
                      <button class="icon-btn" type="button" title="Move task down" [disabled]="taskIndex === g.tasks.length - 1 || saving()" (click)="moveTask(g, taskIndex, 1)">Dn</button>
                      <button class="icon-btn" type="button" title="Edit task" (click)="startEditTask(g, t)">✎</button>
                      <button class="icon-btn icon-danger" type="button" title="Delete task" (click)="deleteTask(t)">🗑</button>
                    </div>
                  }
                </li>
              }
            </ul>

            @if (addingTaskInGroup() === g.id) {
              <div class="add-task-form">
                <div class="row g-2 align-items-end">
                  <div class="col-12 col-md-5">
                    <label class="form-label small">Title</label>
                    <input class="form-control form-control-sm" [(ngModel)]="newTask.title" />
                  </div>
                  <div class="col-12 col-md-5">
                    <label class="form-label small">Description</label>
                    <input class="form-control form-control-sm" [(ngModel)]="newTask.description" />
                  </div>
                  <div class="col-12 col-md-2 d-grid gap-1">
                    <button class="btn-neon btn-sm" [disabled]="!newTask.title.trim() || saving()" (click)="saveNewTask(g)">Save</button>
                    <button class="btn-link-soft btn-sm" type="button" (click)="cancelAddTask()">Cancel</button>
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
    .daily-shell { padding: 0.25rem 0; }

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
    .count-completed { color: var(--success); border-color: var(--success); }
    .count-pending { color: var(--warning); border-color: var(--warning); }

    .group-card {
      padding: 1rem;
    }
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
    .group-counts { margin-left: auto; }
    .group-actions { display: flex; gap: 0.25rem; }

    .task-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.5rem; }
    .task-row {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      padding: 0.5rem;
      border: 1px solid var(--border);
      border-radius: var(--radius-sm);
      background: var(--surface-2);
    }
    .task-done .task-title { text-decoration: line-through; color: var(--fg-muted); }
    .task-check { margin-top: 0.4rem; flex-shrink: 0; }
    .task-main { flex: 1; min-width: 0; }
    .task-title { font-weight: 500; }
    .task-desc { font-size: 0.85rem; color: var(--fg-muted); margin-top: 0.1rem; }
    .task-note { margin-top: 0.4rem; }
    .task-actions { display: flex; gap: 0.2rem; }
    .task-edit-form { flex: 1; }

    .icon-btn {
      min-width: 28px;
      height: 28px;
      padding: 0 0.35rem;
      border-radius: var(--radius-sm);
      background: transparent;
      border: 1px solid transparent;
      color: var(--fg-muted);
      cursor: pointer;
      transition: all 120ms ease;
      font-size: 0.9rem;
    }
    .icon-btn:hover { color: var(--fg); border-color: var(--border-strong); }
    .icon-danger:hover { color: var(--danger); border-color: var(--danger); }

    .btn-link-soft {
      background: transparent;
      border: none;
      color: var(--fg-muted);
      padding: 0.3rem 0.6rem;
      cursor: pointer;
      transition: color 120ms ease;
    }
    .btn-link-soft:hover { color: var(--neon); }

    .add-task-btn { margin-top: 0.5rem; }
    .add-task-form { margin-top: 0.5rem; padding: 0.5rem; border-radius: var(--radius-sm); background: var(--surface-2); }
  `]
})
export class DailyTasksComponent {
  private readonly api = inject(DailyTasksApi);

  readonly date = signal(todayIso());
  readonly view = signal<DailyByDateView | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);

  readonly addingGroup = signal(false);
  readonly addingTaskInGroup = signal<string | null>(null);
  readonly editingGroup = signal<EditingGroup | null>(null);
  readonly editingTask = signal<EditingTask | null>(null);
  readonly showingReports = signal(false);

  newGroup: NewGroupForm = { name: '', description: '' };
  newTask: NewTaskForm = { title: '', description: '' };

  constructor() {
    this.refresh();
  }

  async refresh(): Promise<void> {
    this.loading.set(true);
    try {
      const v = await firstValueFrom(this.api.getByDate(this.date()));
      this.view.set(v);
    } finally {
      this.loading.set(false);
    }
  }

  onDateChange(value: string): void {
    if (!value) return;
    this.date.set(value);
    this.refresh();
  }

  toggleAddGroup(): void {
    this.addingGroup.update(v => !v);
    this.newGroup = { name: '', description: '' };
  }

  toggleReports(): void {
    this.showingReports.update(v => !v);
  }

  async saveNewGroup(): Promise<void> {
    const name = this.newGroup.name.trim();
    if (!name) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.createGroup({
        name,
        description: this.newGroup.description.trim() || null
      }));
      this.addingGroup.set(false);
      this.newGroup = { name: '', description: '' };
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  startEditGroup(g: DailyGroupView): void {
    this.editingGroup.set({
      id: g.id,
      name: g.name,
      description: g.description ?? ''
    });
  }

  cancelEditGroup(): void {
    this.editingGroup.set(null);
  }

  async saveEditGroup(): Promise<void> {
    const g = this.editingGroup();
    if (!g || !g.name.trim()) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.updateGroup(g.id, {
        name: g.name.trim(),
        description: g.description.trim() || null
      }));
      this.editingGroup.set(null);
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  async deleteGroup(g: DailyGroupView): Promise<void> {
    if (!confirm(`Delete group "${g.name}" and all its tasks?`)) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.deleteGroup(g.id));
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  startAddTask(g: DailyGroupView): void {
    this.addingTaskInGroup.set(g.id);
    this.newTask = { title: '', description: '' };
  }

  cancelAddTask(): void {
    this.addingTaskInGroup.set(null);
  }

  async saveNewTask(g: DailyGroupView): Promise<void> {
    const title = this.newTask.title.trim();
    if (!title) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.createTask({
        groupId: g.id,
        title,
        description: this.newTask.description.trim() || null,
        status: 1
      }));
      this.addingTaskInGroup.set(null);
      this.newTask = { title: '', description: '' };
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  startEditTask(g: DailyGroupView, t: DailyTaskWithCompletion): void {
    this.editingTask.set({
      id: t.id,
      groupId: g.id,
      title: t.title,
      description: t.description ?? '',
      status: t.status
    });
  }

  cancelEditTask(): void {
    this.editingTask.set(null);
  }

  async saveEditTask(): Promise<void> {
    const t = this.editingTask();
    if (!t || !t.title.trim()) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.updateTask(t.id, {
        groupId: t.groupId,
        title: t.title.trim(),
        description: t.description.trim() || null,
        status: t.status
      }));
      this.editingTask.set(null);
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  async deleteTask(t: DailyTaskWithCompletion): Promise<void> {
    const confirmationTitle = prompt(`Type "${t.title}" to delete this task.`);
    if (confirmationTitle !== t.title) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.deleteTask(t.id, confirmationTitle));
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  async moveGroup(index: number, direction: -1 | 1): Promise<void> {
    const groups = [...(this.view()?.groups ?? [])];
    const target = index + direction;
    if (target < 0 || target >= groups.length) return;
    [groups[index], groups[target]] = [groups[target], groups[index]];
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.reorderGroups(groups.map((g, i) => ({ groupId: g.id, displayOrder: i + 1 }))));
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  async moveTask(group: DailyGroupView, index: number, direction: -1 | 1): Promise<void> {
    const tasks = [...group.tasks];
    const target = index + direction;
    if (target < 0 || target >= tasks.length) return;
    [tasks[index], tasks[target]] = [tasks[target], tasks[index]];
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.reorderTasks(tasks.map((t, i) => ({ taskId: t.id, groupId: group.id, displayOrder: i + 1 }))));
      await this.refresh();
    } finally {
      this.saving.set(false);
    }
  }

  async toggleCompleted(t: DailyTaskWithCompletion, evt: Event): Promise<void> {
    const checked = (evt.target as HTMLInputElement).checked;
    await this.persistCompletion(t, checked, t.completion?.note ?? null);
  }

  async saveNoteOnBlur(t: DailyTaskWithCompletion, evt: FocusEvent): Promise<void> {
    const value = (evt.target as HTMLInputElement).value.trim();
    const currentNote = t.completion?.note ?? '';
    if (value === currentNote) return;
    await this.persistCompletion(t, t.completion?.isCompleted ?? false, value || null);
  }

  private async persistCompletion(t: DailyTaskWithCompletion, isCompleted: boolean, note: string | null): Promise<void> {
    try {
      await firstValueFrom(this.api.upsertCompletion(t.id, this.date(), { isCompleted, note }));
      await this.refresh();
    } catch (err) {
      console.error('Failed to save completion', err);
    }
  }
}
