import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { GoalsApi } from './goals.api';
import { Goal, GoalPlan, GoalStep, ReportFormat } from './goals.models';

function today(): string { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`; }
function nextMonth(): string { const d = new Date(); d.setMonth(d.getMonth() + 1); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`; }

@Component({
  selector: 'app-goals',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  template: `
    <section class="container py-4">
      <a routerLink="/home" class="text-muted-soft small">Back home</a>
      <div class="d-flex align-items-center gap-2 mt-2 mb-3">
        <h1 class="page-title m-0">Goal Tracker</h1>
        <button class="btn-neon btn-sm ms-auto" (click)="openPlan()">+ Plan</button>
        <button class="btn-link-soft btn-sm" (click)="download('Csv')">CSV</button>
        <button class="btn-link-soft btn-sm" (click)="download('Xlsx')">Excel</button>
        <button class="btn-link-soft btn-sm" (click)="download('Pdf')">PDF</button>
      </div>

      @if (planOpen()) {
        <form class="surface p-3 mb-3 form-row" [formGroup]="planForm" (ngSubmit)="savePlan()">
          <input class="form-control form-control-sm" placeholder="Plan name *" formControlName="name" />
          <input class="form-control form-control-sm" placeholder="Description" formControlName="description" />
          <input class="form-control form-control-sm" placeholder="Tag" formControlName="tag" />
          <button class="btn-neon btn-sm">Save</button><button class="btn-link-soft btn-sm" type="button" (click)="planOpen.set(false)">Cancel</button>
          @if (planError()) { <div class="alert alert-danger py-1 px-2 small mb-0 w-100">{{ planError() }}</div> }
        </form>
      }

      @if (goalOpen()) {
        <form class="surface p-3 mb-3 grid-form" [formGroup]="goalForm" (ngSubmit)="saveGoal()">
          <input class="form-control form-control-sm" placeholder="Goal name *" formControlName="name" />
          <input class="form-control form-control-sm" placeholder="Description" formControlName="description" />
          <input class="form-control form-control-sm" placeholder="Tag" formControlName="tag" />
          <input type="date" class="form-control form-control-sm" formControlName="startDate" />
          <input type="date" class="form-control form-control-sm" formControlName="deadline" />
          <input type="date" class="form-control form-control-sm" formControlName="achievedDate" />
          <input class="form-control form-control-sm wide" placeholder="Note" formControlName="note" />
          <button class="btn-neon btn-sm">Save Goal</button><button class="btn-link-soft btn-sm" type="button" (click)="goalOpen.set(false)">Cancel</button>
          @if (goalError()) { <div class="alert alert-danger py-1 px-2 small mb-0 wide">{{ goalError() }}</div> }
        </form>
      }

      @if (stepOpen()) {
        <form class="surface p-3 mb-3 grid-form" [formGroup]="stepForm" (ngSubmit)="saveStep()">
          <input class="form-control form-control-sm" placeholder="Step name *" formControlName="name" />
          <input class="form-control form-control-sm" placeholder="Description" formControlName="description" />
          <input type="date" class="form-control form-control-sm" formControlName="startDate" />
          <input type="date" class="form-control form-control-sm" formControlName="deadline" />
          <input type="date" class="form-control form-control-sm" formControlName="achievedDate" />
          <input class="form-control form-control-sm wide" placeholder="Note" formControlName="note" />
          <button class="btn-neon btn-sm">Save Step</button><button class="btn-link-soft btn-sm" type="button" (click)="stepOpen.set(false)">Cancel</button>
          @if (stepError()) { <div class="alert alert-danger py-1 px-2 small mb-0 wide">{{ stepError() }}</div> }
        </form>
      }

      <div class="goal-list">
        @for (plan of plans(); track plan.id) {
          <article class="surface p-3">
            <div class="d-flex gap-2 align-items-start">
              <div><h2 class="plan-title">{{ plan.name }}</h2><p class="text-muted-soft small mb-1">{{ plan.description || 'No description' }}</p><span class="tag" *ngIf="plan.tag">{{ plan.tag }}</span></div>
              <div class="ms-auto small text-muted-soft">{{ plan.achievedGoalCount }}/{{ plan.goalCount }} achieved</div>
            </div>
            <div class="actions mt-2"><button class="btn-link-soft btn-sm" (click)="editPlan(plan)">Edit Plan</button><button class="btn-link-soft btn-sm" (click)="openGoal(plan)">+ Goal</button><button class="btn-link-soft btn-sm danger" (click)="deletePlan(plan)">Delete</button></div>
            @for (goal of plan.goals; track goal.id) {
              <div class="goal-row">
                <div><strong>{{ goal.name }}</strong> <span class="status">{{ goal.status }}</span><div class="small text-muted-soft">{{ goal.startDate }} to {{ goal.deadline }}</div></div>
                <div class="actions"><button class="icon-btn" (click)="editGoal(plan, goal)">Edit</button><button class="icon-btn" (click)="openStep(goal)">+ Step</button><button class="icon-btn danger" (click)="deleteGoal(goal)">Delete</button></div>
              </div>
              @for (step of goal.steps; track step.id) {
                <div class="step-row"><span>{{ step.name }}</span><span class="status">{{ step.status }}</span><span>{{ step.deadline }}</span><button class="icon-btn" (click)="editStep(goal, step)">Edit</button><button class="icon-btn danger" (click)="deleteStep(step)">Delete</button></div>
              }
            }
          </article>
        }
      </div>
    </section>
  `,
  styles: [`
    .page-title { font-size: 1.5rem; font-weight: 600; }
    .btn-link-soft { background: transparent; border: 1px solid var(--border-strong); color: var(--fg-muted); padding: .3rem .7rem; border-radius: var(--radius-sm); }
    .btn-link-soft.danger, .icon-btn.danger { color: var(--danger); }
    .form-row { display: flex; gap: .5rem; flex-wrap: wrap; }
    .form-row input { min-width: 180px; flex: 1; }
    .grid-form { display: grid; grid-template-columns: repeat(auto-fit, minmax(170px, 1fr)); gap: .6rem; }
    .wide { grid-column: 1 / -1; }
    .goal-list { display: grid; gap: 1rem; }
    .plan-title { font-size: 1.05rem; font-weight: 600; margin: 0; }
    .tag, .status { border: 1px solid var(--neon-cyan); color: var(--neon-cyan); border-radius: 999px; padding: .08rem .45rem; font-size: .72rem; }
    .goal-row { display: flex; align-items: center; justify-content: space-between; gap: 1rem; border-top: 1px solid var(--border); padding: .75rem 0; margin-top: .5rem; }
    .step-row { display: grid; grid-template-columns: 1fr auto auto auto auto; gap: .6rem; align-items: center; padding: .35rem .5rem; margin-left: 1rem; border-left: 1px solid var(--border-strong); font-size: .86rem; }
    .actions { display: flex; gap: .4rem; flex-wrap: wrap; }
    .icon-btn { border: 0; background: transparent; color: var(--neon-cyan); }
  `]
})
export class GoalsComponent {
  private readonly api = inject(GoalsApi);
  private readonly fb = inject(FormBuilder);
  readonly plans = signal<GoalPlan[]>([]);
  readonly planOpen = signal(false);
  readonly goalOpen = signal(false);
  readonly stepOpen = signal(false);
  readonly planError = signal<string | null>(null);
  readonly goalError = signal<string | null>(null);
  readonly stepError = signal<string | null>(null);
  editingPlan: string | null = null;
  editingGoal: string | null = null;
  editingStep: string | null = null;
  selectedPlan: string | null = null;
  selectedGoal: string | null = null;
  planForm = this.fb.group({ name: ['', Validators.required], description: [''], tag: [''] });
  goalForm = this.fb.group({ name: ['', Validators.required], description: [''], tag: [''], startDate: [today(), Validators.required], deadline: [nextMonth(), Validators.required], achievedDate: [''], note: [''] });
  stepForm = this.fb.group({ name: ['', Validators.required], description: [''], startDate: [today(), Validators.required], deadline: [nextMonth(), Validators.required], achievedDate: [''], note: [''] });
  constructor() { this.refresh(); }
  async refresh() { this.plans.set(await firstValueFrom(this.api.plans())); }
  openPlan() { this.editingPlan = null; this.planError.set(null); this.planForm.reset({ name: '', description: '', tag: '' }); this.planOpen.set(true); }
  editPlan(p: GoalPlan) { this.editingPlan = p.id; this.planForm.patchValue(p as any); this.planOpen.set(true); }
  async savePlan() { const body = this.planForm.getRawValue(); if (!body.name?.trim()) { this.planError.set('Plan name is required.'); return; } try { this.editingPlan ? await firstValueFrom(this.api.updatePlan(this.editingPlan, body)) : await firstValueFrom(this.api.createPlan(body)); this.planOpen.set(false); await this.refresh(); } catch (e: any) { this.planError.set(e?.error?.message ?? 'Save failed.'); } }
  async deletePlan(p: GoalPlan) { if (confirm(`Delete plan ${p.name}?`)) { await firstValueFrom(this.api.deletePlan(p.id)); await this.refresh(); } }
  openGoal(p: GoalPlan) { this.selectedPlan = p.id; this.editingGoal = null; this.goalError.set(null); this.goalForm.reset({ startDate: today(), deadline: nextMonth() } as any); this.goalOpen.set(true); }
  editGoal(p: GoalPlan, g: Goal) { this.selectedPlan = p.id; this.editingGoal = g.id; this.goalForm.patchValue(g as any); this.goalOpen.set(true); }
  async saveGoal() { const body = { ...this.goalForm.getRawValue(), achievedDate: this.goalForm.value.achievedDate || null }; if (!body.name?.trim()) { this.goalError.set('Goal name is required.'); return; } try { this.editingGoal ? await firstValueFrom(this.api.updateGoal(this.editingGoal, body)) : await firstValueFrom(this.api.createGoal(this.selectedPlan!, body)); this.goalOpen.set(false); await this.refresh(); } catch (e: any) { this.goalError.set(e?.error?.message ?? 'Save failed.'); } }
  async deleteGoal(g: Goal) { if (confirm(`Delete goal ${g.name}?`)) { await firstValueFrom(this.api.deleteGoal(g.id)); await this.refresh(); } }
  openStep(g: Goal) { this.selectedGoal = g.id; this.editingStep = null; this.stepError.set(null); this.stepForm.reset({ startDate: today(), deadline: nextMonth() } as any); this.stepOpen.set(true); }
  editStep(g: Goal, s: GoalStep) { this.selectedGoal = g.id; this.editingStep = s.id; this.stepForm.patchValue(s as any); this.stepOpen.set(true); }
  async saveStep() { const body = { ...this.stepForm.getRawValue(), achievedDate: this.stepForm.value.achievedDate || null }; if (!body.name?.trim()) { this.stepError.set('Step name is required.'); return; } try { this.editingStep ? await firstValueFrom(this.api.updateStep(this.editingStep, body)) : await firstValueFrom(this.api.createStep(this.selectedGoal!, body)); this.stepOpen.set(false); await this.refresh(); } catch (e: any) { this.stepError.set(e?.error?.message ?? 'Save failed.'); } }
  async deleteStep(s: GoalStep) { if (confirm(`Delete step ${s.name}?`)) { await firstValueFrom(this.api.deleteStep(s.id)); await this.refresh(); } }
  async download(format: ReportFormat) { const res = await firstValueFrom(this.api.download(today(), nextMonth(), format)); const url = URL.createObjectURL(res.body!); const a = document.createElement('a'); a.href = url; a.download = `goals.${format.toLowerCase()}`; document.body.appendChild(a); a.click(); document.body.removeChild(a); URL.revokeObjectURL(url); }
}
