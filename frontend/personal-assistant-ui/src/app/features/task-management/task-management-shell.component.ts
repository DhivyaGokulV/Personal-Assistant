import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DailyTasksComponent } from './daily/daily-tasks.component';
import { PeriodicTasksComponent } from './periodic/periodic-tasks.component';
import { TodoListComponent } from './todo/todo.component';

type Tab = 'daily' | 'periodic' | 'todo';

@Component({
  selector: 'app-task-management',
  imports: [CommonModule, RouterLink, DailyTasksComponent, PeriodicTasksComponent, TodoListComponent],
  template: `
    <section class="container py-4">
      <div class="d-flex align-items-center mb-3 gap-2">
        <a routerLink="/home" class="text-muted-soft small">← Home</a>
      </div>
      <h1 class="page-title mb-3">Task Management</h1>

      <ul class="nav nav-tabs neon-tabs mb-3" role="tablist">
        <li class="nav-item" role="presentation">
          <button class="nav-link" [class.active]="active() === 'daily'" (click)="active.set('daily')">Daily Tasks</button>
        </li>
        <li class="nav-item" role="presentation">
          <button class="nav-link" [class.active]="active() === 'periodic'" (click)="active.set('periodic')">Periodic Tasks</button>
        </li>
        <li class="nav-item" role="presentation">
          <button class="nav-link" [class.active]="active() === 'todo'" (click)="active.set('todo')">To-Do List</button>
        </li>
      </ul>

      @switch (active()) {
        @case ('daily') {
          <app-daily-tasks />
        }
        @case ('periodic') {
          <app-periodic-tasks />
        }
        @case ('todo') {
          <app-todo-list />
        }
      }
    </section>
  `,
  styles: [`
    .page-title { font-size: 1.5rem; font-weight: 600; }
    .neon-tabs .nav-link {
      color: var(--fg-muted);
      background: transparent;
      border: 1px solid transparent;
      border-bottom: none;
      border-radius: var(--radius-sm) var(--radius-sm) 0 0;
      padding: 0.5rem 1rem;
    }
    .neon-tabs .nav-link.active {
      color: var(--fg);
      background: var(--surface);
      border: 1px solid var(--neon);
      border-bottom: 1px solid var(--surface);
      box-shadow: 0 -3px 12px var(--neon-soft);
    }
  `]
})
export class TaskManagementShellComponent {
  readonly active = signal<Tab>('daily');
}
