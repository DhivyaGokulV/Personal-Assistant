import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { HealthApi } from './health.api';
import { FoodDefinition, MeasurementEntry, NutritionEntry, NutritionGoal, ReportFormat, WorkoutDefinition, WorkoutEntry, WorkoutType } from './health.models';

type Tab = 'measurements' | 'workouts' | 'nutrition' | 'settings';
function today(): string { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`; }
function firstOfMonth(): string { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-01`; }

@Component({
  selector: 'app-health',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  template: `
    <section class="container py-4">
      <a routerLink="/home" class="text-muted-soft small">Back home</a>
      <h1 class="page-title mt-2">Health & Nutrition</h1>
      <ul class="nav nav-tabs neon-tabs mb-3">
        <li class="nav-item"><button class="nav-link" [class.active]="tab()==='measurements'" (click)="tab.set('measurements')">Measurements</button></li>
        <li class="nav-item"><button class="nav-link" [class.active]="tab()==='workouts'" (click)="tab.set('workouts')">Workouts</button></li>
        <li class="nav-item"><button class="nav-link" [class.active]="tab()==='nutrition'" (click)="tab.set('nutrition')">Nutrition</button></li>
        <li class="nav-item"><button class="nav-link" [class.active]="tab()==='settings'" (click)="tab.set('settings')">Settings</button></li>
      </ul>

      @switch (tab()) {
        @case ('measurements') {
          <div class="toolbar"><button class="btn-neon btn-sm" (click)="openMeasurement()">+ Entry</button><span class="ms-auto"></span><button class="btn-link-soft btn-sm" (click)="downloadMeasurements('Csv')">CSV</button><button class="btn-link-soft btn-sm" (click)="downloadMeasurements('Xlsx')">Excel</button><button class="btn-link-soft btn-sm" (click)="downloadMeasurements('Pdf')">PDF</button></div>
          @if (measurementFormOpen()) {
            <form class="surface p-3 mb-3" [formGroup]="measurementForm" (ngSubmit)="saveMeasurement()">
              <div class="grid-form">
                <label>Date *<input type="date" class="form-control form-control-sm" formControlName="date" /></label>
                @for (field of measurementFields; track field.key) { <label>{{ field.label }}<input type="number" step="0.01" class="form-control form-control-sm" [formControlName]="field.key" /></label> }
                <label class="wide">Note<input class="form-control form-control-sm" formControlName="note" /></label>
              </div>
              @if (measurementError()) { <div class="alert alert-danger py-1 px-2 small mb-2">{{ measurementError() }}</div> }
              <button class="btn-neon btn-sm">Save</button><button type="button" class="btn-link-soft btn-sm ms-2" (click)="measurementFormOpen.set(false)">Cancel</button>
            </form>
          }
          <div class="table-wrap surface"><table><thead><tr><th>Date</th><th>Weight</th><th>BMI</th><th>Body fat</th><th>Muscle</th><th>Note</th><th></th></tr></thead><tbody>
            @for (m of measurements(); track m.id) { <tr><td>{{ m.date }}</td><td>{{ m.weightKg ?? '-' }}</td><td>{{ m.bmi ?? '-' }}</td><td>{{ m.bodyFatPercentage ?? '-' }}</td><td>{{ m.musclePercentage ?? '-' }}</td><td>{{ m.note ?? '-' }}</td><td><button class="icon-btn" (click)="editMeasurement(m)">Edit</button><button class="icon-btn danger" (click)="deleteMeasurement(m)">Delete</button></td></tr> }
          </tbody></table></div>
        }
        @case ('workouts') {
          <div class="toolbar"><button class="btn-neon btn-sm" (click)="openWorkout()">+ Workout</button><input class="form-control form-control-sm slim" placeholder="Filter workout" [(ngModel)]="workoutFilter" (keyup.enter)="loadWorkouts()" /><button class="btn-link-soft btn-sm" (click)="loadWorkouts()">Filter</button><span class="ms-auto"></span><button class="btn-link-soft btn-sm" (click)="downloadWorkouts('Csv')">CSV</button><button class="btn-link-soft btn-sm" (click)="downloadWorkouts('Xlsx')">Excel</button><button class="btn-link-soft btn-sm" (click)="downloadWorkouts('Pdf')">PDF</button></div>
          @if (workoutFormOpen()) {
            <form class="surface p-3 mb-3" [formGroup]="workoutForm" (ngSubmit)="saveWorkout()">
              <div class="grid-form">
                <label>Date *<input type="date" class="form-control form-control-sm" formControlName="date" /></label>
                <label>Type *<select class="form-select form-select-sm" formControlName="type"><option [ngValue]="1">Weight based</option><option [ngValue]="2">Calisthenics</option><option [ngValue]="3">Cardio</option></select></label>
                <label>Workout *<input class="form-control form-control-sm" formControlName="workoutName" /></label>
                <label>Muscle<input class="form-control form-control-sm" formControlName="targetedMuscle" /></label>
                <label>Tag<input class="form-control form-control-sm" formControlName="tag" /></label>
                <label>Duration<input type="number" class="form-control form-control-sm" formControlName="durationMinutes" /></label>
                <label>Intensity<input class="form-control form-control-sm" formControlName="intensity" /></label>
                <label>Distance<input type="number" step="0.01" class="form-control form-control-sm" formControlName="distance" /></label>
                <label>Calories<input type="number" step="0.01" class="form-control form-control-sm" formControlName="caloriesBurned" /></label>
                <label class="wide">Note<input class="form-control form-control-sm" formControlName="note" /></label>
              </div>
              @if (workoutType() !== 3) {
                <div class="set-list" formArrayName="sets">
                  @for (set of sets.controls; track $index) {
                    <div class="set-row" [formGroupName]="$index"><span>Set {{ $index + 1 }}</span><input type="number" class="form-control form-control-sm" placeholder="Reps" formControlName="reps" /><input type="number" step="0.01" class="form-control form-control-sm" [placeholder]="workoutType() === 1 ? 'Weight' : 'Added weight'" [formControlName]="workoutType() === 1 ? 'weight' : 'addedWeight'" /><button type="button" class="icon-btn danger" (click)="sets.removeAt($index)">Remove</button></div>
                  }
                  <button type="button" class="btn-link-soft btn-sm" (click)="addSet()">+ Set</button>
                </div>
              }
              @if (workoutError()) { <div class="alert alert-danger py-1 px-2 small mb-2">{{ workoutError() }}</div> }
              <button class="btn-neon btn-sm">Save</button><button type="button" class="btn-link-soft btn-sm ms-2" (click)="workoutFormOpen.set(false)">Cancel</button>
            </form>
          }
          <div class="table-wrap surface"><table><thead><tr><th>Date</th><th>Workout</th><th>Type</th><th>Details</th><th>Tag</th><th></th></tr></thead><tbody>
            @for (w of workouts(); track w.id) { <tr><td>{{ w.date }}</td><td>{{ w.workoutName }}</td><td>{{ typeLabel(w.type) }}</td><td>{{ workoutDetails(w) }}</td><td>{{ w.tag ?? '-' }}</td><td><button class="icon-btn" (click)="editWorkout(w)">Edit</button><button class="icon-btn danger" (click)="deleteWorkout(w)">Delete</button></td></tr> }
          </tbody></table></div>
        }
        @case ('nutrition') {
          <div class="surface p-3 mb-3">
            <h2 class="section-title">Daily goals</h2>
            <form [formGroup]="goalForm" class="macro-row" (ngSubmit)="saveGoal()"><input type="number" class="form-control form-control-sm" placeholder="Carbs" formControlName="carbohydrates" /><input type="number" class="form-control form-control-sm" placeholder="Protein" formControlName="protein" /><input type="number" class="form-control form-control-sm" placeholder="Fat" formControlName="fat" /><input type="number" class="form-control form-control-sm" placeholder="Calories" formControlName="calories" /><button class="btn-neon btn-sm">Save</button></form>
            @if (dayView(); as d) { <div class="macro-row mt-2 small"><span>Carbs {{ d.carbohydrates }}/{{ d.goal.carbohydrates ?? '-' }}</span><span>Protein {{ d.protein }}/{{ d.goal.protein ?? '-' }}</span><span>Fat {{ d.fat }}/{{ d.goal.fat ?? '-' }}</span><span>Calories {{ d.calories }}/{{ d.goal.calories ?? '-' }}</span></div> }
          </div>
          <div class="toolbar"><button class="btn-neon btn-sm" (click)="openNutrition()">+ Food</button><span class="ms-auto"></span><button class="btn-link-soft btn-sm" (click)="downloadNutrition('Csv')">CSV</button><button class="btn-link-soft btn-sm" (click)="downloadNutrition('Xlsx')">Excel</button><button class="btn-link-soft btn-sm" (click)="downloadNutrition('Pdf')">PDF</button></div>
          @if (nutritionFormOpen()) {
            <form class="surface p-3 mb-3" [formGroup]="nutritionForm" (ngSubmit)="saveNutrition()">
              <div class="grid-form">
                <label>Date *<input type="date" class="form-control form-control-sm" formControlName="date" /></label><label>Time *<select class="form-select form-select-sm" formControlName="timeOfDay"><option [ngValue]="1">Morning</option><option [ngValue]="2">Afternoon</option><option [ngValue]="3">Evening</option><option [ngValue]="4">Night</option></select></label>
                <label>Food *<input class="form-control form-control-sm" formControlName="food" /></label><label>Quantity *<input type="number" step="0.01" class="form-control form-control-sm" formControlName="quantity" /></label><label>Unit *<input class="form-control form-control-sm" formControlName="unit" /></label>
                <label>Carbs<input type="number" step="0.01" class="form-control form-control-sm" formControlName="carbohydrates" /></label><label>Protein<input type="number" step="0.01" class="form-control form-control-sm" formControlName="protein" /></label><label>Fat<input type="number" step="0.01" class="form-control form-control-sm" formControlName="fat" /></label><label>Calories<input type="number" step="0.01" class="form-control form-control-sm" formControlName="calories" /></label><label class="wide">Note<input class="form-control form-control-sm" formControlName="note" /></label>
              </div>
              @if (nutritionError()) { <div class="alert alert-danger py-1 px-2 small mb-2">{{ nutritionError() }}</div> }
              <button class="btn-neon btn-sm">Save</button><button type="button" class="btn-link-soft btn-sm ms-2" (click)="nutritionFormOpen.set(false)">Cancel</button>
            </form>
          }
          <div class="table-wrap surface"><table><thead><tr><th>Date</th><th>Food</th><th>Qty</th><th>Macros</th><th></th></tr></thead><tbody>@for (n of nutrition(); track n.id) { <tr><td>{{ n.date }}</td><td>{{ n.food }}</td><td>{{ n.quantity }} {{ n.unit }}</td><td>C {{ n.carbohydrates ?? 0 }} / P {{ n.protein ?? 0 }} / F {{ n.fat ?? 0 }} / Cal {{ n.calories ?? 0 }}</td><td><button class="icon-btn" (click)="editNutrition(n)">Edit</button><button class="icon-btn danger" (click)="deleteNutrition(n)">Delete</button></td></tr> }</tbody></table></div>
        }
        @case ('settings') {
          <div class="row g-3">
            <div class="col-md-6"><div class="surface p-3"><h2 class="section-title">Workout catalog</h2><form [formGroup]="definitionForm" (ngSubmit)="saveDefinition()" class="stack"><input class="form-control form-control-sm" placeholder="Name *" formControlName="name" /><select class="form-select form-select-sm" formControlName="type"><option [ngValue]="1">Weight based</option><option [ngValue]="2">Calisthenics</option><option [ngValue]="3">Cardio</option></select><input class="form-control form-control-sm" placeholder="Target muscle" formControlName="targetedMuscle" /><input class="form-control form-control-sm" placeholder="Tag" formControlName="tag" /><button class="btn-neon btn-sm">Save</button></form>@for (d of definitions(); track d.id) { <div class="list-line"><span>{{ d.name }}</span><button class="icon-btn danger" (click)="deleteDefinition(d)">Delete</button></div> }</div></div>
            <div class="col-md-6"><div class="surface p-3"><h2 class="section-title">Food catalog</h2><form [formGroup]="foodForm" (ngSubmit)="saveFood()" class="stack"><input class="form-control form-control-sm" placeholder="Food *" formControlName="name" /><input class="form-control form-control-sm" placeholder="Unit *" formControlName="unit" /><div class="macro-row"><input type="number" class="form-control form-control-sm" placeholder="Carbs" formControlName="carbohydrates" /><input type="number" class="form-control form-control-sm" placeholder="Protein" formControlName="protein" /><input type="number" class="form-control form-control-sm" placeholder="Fat" formControlName="fat" /><input type="number" class="form-control form-control-sm" placeholder="Calories" formControlName="calories" /></div><button class="btn-neon btn-sm">Save</button></form>@for (f of foods(); track f.id) { <div class="list-line"><span>{{ f.name }} / {{ f.unit }}</span><button class="icon-btn danger" (click)="deleteFood(f)">Delete</button></div> }</div></div>
          </div>
        }
      }
    </section>
  `,
  styles: [`
    .page-title { font-size: 1.5rem; font-weight: 600; }
    .neon-tabs .nav-link { color: var(--fg-muted); background: transparent; border: 1px solid transparent; }
    .neon-tabs .nav-link.active { color: var(--fg); background: var(--surface); border-color: var(--neon); box-shadow: 0 0 12px var(--neon-soft); }
    .toolbar { display: flex; gap: .5rem; align-items: center; margin-bottom: .75rem; flex-wrap: wrap; }
    .btn-link-soft { background: transparent; border: 1px solid var(--border-strong); color: var(--fg-muted); padding: .3rem .7rem; border-radius: var(--radius-sm); }
    .grid-form { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: .6rem; }
    .grid-form label, .stack { display: flex; flex-direction: column; gap: .25rem; font-size: .78rem; color: var(--fg-muted); }
    .wide { grid-column: 1 / -1; }
    .table-wrap { overflow: auto; }
    table { width: 100%; border-collapse: collapse; font-size: .86rem; }
    th, td { padding: .5rem .7rem; border-bottom: 1px solid var(--border); }
    .icon-btn { border: 0; background: transparent; color: var(--neon-cyan); }
    .icon-btn.danger { color: var(--danger); }
    .set-list { margin: .8rem 0; display: grid; gap: .5rem; }
    .set-row, .macro-row, .list-line { display: flex; gap: .5rem; align-items: center; flex-wrap: wrap; }
    .list-line { justify-content: space-between; border-bottom: 1px solid var(--border); padding: .4rem 0; }
    .section-title { font-size: .95rem; font-weight: 600; }
    .slim { max-width: 220px; }
  `]
})
export class HealthComponent {
  private readonly api = inject(HealthApi);
  private readonly fb = inject(FormBuilder);
  readonly tab = signal<Tab>('measurements');
  readonly measurements = signal<MeasurementEntry[]>([]);
  readonly workouts = signal<WorkoutEntry[]>([]);
  readonly nutrition = signal<NutritionEntry[]>([]);
  readonly definitions = signal<WorkoutDefinition[]>([]);
  readonly foods = signal<FoodDefinition[]>([]);
  readonly dayView = signal<any>(null);
  readonly measurementFormOpen = signal(false);
  readonly workoutFormOpen = signal(false);
  readonly nutritionFormOpen = signal(false);
  readonly measurementError = signal<string | null>(null);
  readonly workoutError = signal<string | null>(null);
  readonly nutritionError = signal<string | null>(null);
  editingMeasurement: string | null = null;
  editingWorkout: string | null = null;
  editingNutrition: string | null = null;
  workoutFilter = '';
  readonly reportFrom = firstOfMonth();
  readonly reportTo = today();
  readonly measurementFields = [
    ['heightCm', 'Height cm'], ['weightKg', 'Weight kg'], ['bmi', 'BMI'], ['bodyFatPercentage', 'Body fat %'], ['musclePercentage', 'Muscle %'], ['bicepsCm', 'Biceps cm'], ['bellyCm', 'Belly cm'], ['forearmCm', 'Forearm cm'], ['chestCm', 'Chest cm'], ['thighsCm', 'Thighs cm'], ['calvesCm', 'Calves cm'], ['neckCm', 'Neck cm']
  ].map(([key, label]) => ({ key, label }));
  measurementForm = this.fb.group({ date: [today(), Validators.required], heightCm: [null], weightKg: [null], bmi: [null], bodyFatPercentage: [null], musclePercentage: [null], bicepsCm: [null], bellyCm: [null], forearmCm: [null], chestCm: [null], thighsCm: [null], calvesCm: [null], neckCm: [null], note: [''] });
  workoutForm = this.fb.group({ date: [today(), Validators.required], type: [1 as WorkoutType, Validators.required], workoutName: ['', Validators.required], targetedMuscle: [''], tag: [''], durationMinutes: [null], intensity: [''], distance: [null], caloriesBurned: [null], note: [''], sets: this.fb.array<any>([]) });
  nutritionForm = this.fb.group({ date: [today(), Validators.required], timeOfDay: [1, Validators.required], food: ['', Validators.required], quantity: [1, Validators.required], unit: ['unit', Validators.required], carbohydrates: [null], protein: [null], fat: [null], calories: [null], note: [''] });
  goalForm = this.fb.group({ carbohydrates: [null], protein: [null], fat: [null], calories: [null] });
  definitionForm = this.fb.group({ name: ['', Validators.required], type: [1, Validators.required], targetedMuscle: [''], tag: [''] });
  foodForm = this.fb.group({ name: ['', Validators.required], unit: ['unit', Validators.required], carbohydrates: [null], protein: [null], fat: [null], calories: [null] });
  workoutType = computed(() => Number(this.workoutForm.controls.type.value) as WorkoutType);
  get sets(): FormArray { return this.workoutForm.controls.sets as FormArray; }
  constructor() { this.reload(); }
  async reload(): Promise<void> { await Promise.all([this.loadMeasurements(), this.loadWorkouts(), this.loadNutrition(), this.loadSettings(), this.loadDay()]); }
  async loadMeasurements() { this.measurements.set((await firstValueFrom(this.api.measurements({ pageSize: 100 }))).items); }
  async loadWorkouts() { this.workouts.set((await firstValueFrom(this.api.workouts({ workoutName: this.workoutFilter, pageSize: 100 }))).items); }
  async loadNutrition() { this.nutrition.set((await firstValueFrom(this.api.nutrition({ pageSize: 100 }))).items); }
  async loadSettings() { const [defs, foods, goal] = await Promise.all([firstValueFrom(this.api.workoutDefinitions()), firstValueFrom(this.api.foods()), firstValueFrom(this.api.goal())]); this.definitions.set(defs); this.foods.set(foods); this.goalForm.patchValue(goal as any); }
  async loadDay() { this.dayView.set(await firstValueFrom(this.api.day(today()))); }
  openMeasurement() { this.editingMeasurement = null; this.measurementError.set(null); this.measurementForm.reset({ date: today(), note: '' } as any); this.measurementFormOpen.set(true); }
  editMeasurement(m: MeasurementEntry) { this.editingMeasurement = m.id; this.measurementError.set(null); this.measurementForm.patchValue(m as any); this.measurementFormOpen.set(true); }
  async saveMeasurement() { const body = this.measurementForm.getRawValue(); if (!this.measurementFields.some(f => (body as any)[f.key] !== null && (body as any)[f.key] !== undefined && (body as any)[f.key] !== '')) { this.measurementError.set('At least one measurement is required.'); return; } try { this.editingMeasurement ? await firstValueFrom(this.api.updateMeasurement(this.editingMeasurement, body)) : await firstValueFrom(this.api.createMeasurement(body)); this.measurementFormOpen.set(false); await this.loadMeasurements(); } catch (e: any) { this.measurementError.set(e?.error?.message ?? 'Save failed.'); } }
  async deleteMeasurement(m: MeasurementEntry) { if (confirm('Delete measurement?')) { await firstValueFrom(this.api.deleteMeasurement(m.id)); await this.loadMeasurements(); } }
  openWorkout() { this.editingWorkout = null; this.workoutError.set(null); this.workoutForm.reset({ date: today(), type: 1, workoutName: '', targetedMuscle: '', tag: '', note: '' } as any); this.sets.clear(); this.addSet(); this.workoutFormOpen.set(true); }
  editWorkout(w: WorkoutEntry) { this.editingWorkout = w.id; this.workoutError.set(null); this.workoutForm.patchValue(w as any); this.sets.clear(); w.sets.forEach(s => this.addSet(s)); this.workoutFormOpen.set(true); }
  addSet(s: any = { reps: 10, weight: null, addedWeight: null }) { this.sets.push(this.fb.group({ reps: [s.reps, Validators.required], weight: [s.weight ?? null], addedWeight: [s.addedWeight ?? null] })); }
  async saveWorkout() { const body = this.workoutForm.getRawValue(); if (!body.workoutName?.trim()) { this.workoutError.set('Workout name is required.'); return; } try { this.editingWorkout ? await firstValueFrom(this.api.updateWorkout(this.editingWorkout, body)) : await firstValueFrom(this.api.createWorkout(body)); this.workoutFormOpen.set(false); await this.loadWorkouts(); } catch (e: any) { this.workoutError.set(e?.error?.message ?? 'Save failed.'); } }
  async deleteWorkout(w: WorkoutEntry) { if (confirm(`Delete ${w.workoutName}?`)) { await firstValueFrom(this.api.deleteWorkout(w.id)); await this.loadWorkouts(); } }
  openNutrition() { this.editingNutrition = null; this.nutritionError.set(null); this.nutritionForm.reset({ date: today(), timeOfDay: 1, quantity: 1, unit: 'unit' } as any); this.nutritionFormOpen.set(true); }
  editNutrition(n: NutritionEntry) { this.editingNutrition = n.id; this.nutritionError.set(null); this.nutritionForm.patchValue(n as any); this.nutritionFormOpen.set(true); }
  async saveNutrition() { const body = this.nutritionForm.getRawValue(); if (!body.food?.trim() || !body.unit?.trim() || !body.quantity || body.quantity <= 0) { this.nutritionError.set('Food, quantity, and unit are required.'); return; } try { this.editingNutrition ? await firstValueFrom(this.api.updateNutrition(this.editingNutrition, body)) : await firstValueFrom(this.api.createNutrition(body)); this.nutritionFormOpen.set(false); await Promise.all([this.loadNutrition(), this.loadDay()]); } catch (e: any) { this.nutritionError.set(e?.error?.message ?? 'Save failed.'); } }
  async deleteNutrition(n: NutritionEntry) { if (confirm(`Delete ${n.food}?`)) { await firstValueFrom(this.api.deleteNutrition(n.id)); await this.loadNutrition(); await this.loadDay(); } }
  async saveGoal() { await firstValueFrom(this.api.saveGoal(this.goalForm.getRawValue())); await this.loadDay(); }
  async saveDefinition() { if (!this.definitionForm.value.name?.trim()) return; await firstValueFrom(this.api.createWorkoutDefinition(this.definitionForm.getRawValue())); this.definitionForm.reset({ type: 1 } as any); await this.loadSettings(); }
  async deleteDefinition(d: WorkoutDefinition) { if (confirm(`Delete ${d.name}?`)) { await firstValueFrom(this.api.deleteWorkoutDefinition(d.id)); await this.loadSettings(); } }
  async saveFood() { if (!this.foodForm.value.name?.trim() || !this.foodForm.value.unit?.trim()) return; await firstValueFrom(this.api.createFood(this.foodForm.getRawValue())); this.foodForm.reset({ unit: 'unit' } as any); await this.loadSettings(); }
  async deleteFood(f: FoodDefinition) { if (confirm(`Delete ${f.name}?`)) { await firstValueFrom(this.api.deleteFood(f.id)); await this.loadSettings(); } }
  typeLabel(t: WorkoutType) { return t === 1 ? 'Weight' : t === 2 ? 'Calisthenics' : 'Cardio'; }
  workoutDetails(w: WorkoutEntry) { return w.type === 3 ? `${w.durationMinutes} min · ${w.distance} distance` : `${w.sets.length} sets · ${w.sets.reduce((a, s) => a + s.reps, 0)} reps`; }
  downloadMeasurements(format: ReportFormat) { this.download(this.api.downloadMeasurements(this.reportFrom, this.reportTo, format), `measurements.${format.toLowerCase()}`); }
  downloadWorkouts(format: ReportFormat) { this.download(this.api.downloadWorkouts(this.reportFrom, this.reportTo, this.workoutFilter, format), `workouts.${format.toLowerCase()}`); }
  downloadNutrition(format: ReportFormat) { this.download(this.api.downloadNutrition(this.reportFrom, this.reportTo, format), `nutrition.${format.toLowerCase()}`); }
  async download(req: any, filename: string) { const res: any = await firstValueFrom(req); const url = URL.createObjectURL(res.body!); const a = document.createElement('a'); a.href = url; a.download = filename; document.body.appendChild(a); a.click(); document.body.removeChild(a); URL.revokeObjectURL(url); }
}
