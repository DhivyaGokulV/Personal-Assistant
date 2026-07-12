import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FoodDefinition, MeasurementEntry, NutritionDay, NutritionEntry, NutritionGoal, PagedResult, ReportFormat, WaterIntakeEntry, WorkoutDefinition, WorkoutEntry } from './health.models';

@Injectable({ providedIn: 'root' })
export class HealthApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/health`;
  private params(opts: Record<string, any>): HttpParams {
    let p = new HttpParams();
    Object.entries(opts).forEach(([k, v]) => { if (v !== undefined && v !== null && v !== '') p = p.set(k, String(v)); });
    return p;
  }
  measurements(opts: any) { return this.http.get<PagedResult<MeasurementEntry>>(`${this.base}/measurements`, { params: this.params(opts) }); }
  createMeasurement(body: any) { return this.http.post<MeasurementEntry>(`${this.base}/measurements`, body); }
  updateMeasurement(id: string, body: any) { return this.http.put<MeasurementEntry>(`${this.base}/measurements/${id}`, body); }
  deleteMeasurement(id: string) { return this.http.delete<void>(`${this.base}/measurements/${id}`); }
  downloadMeasurements(from: string, to: string, format: ReportFormat): Observable<HttpResponse<Blob>> { return this.http.get(`${this.base}/measurements/reports`, { params: this.params({ from, to, format }), responseType: 'blob', observe: 'response' }); }

  workoutDefinitions() { return this.http.get<WorkoutDefinition[]>(`${this.base}/workout-definitions`); }
  createWorkoutDefinition(body: any) { return this.http.post<WorkoutDefinition>(`${this.base}/workout-definitions`, body); }
  updateWorkoutDefinition(id: string, body: any) { return this.http.put<WorkoutDefinition>(`${this.base}/workout-definitions/${id}`, body); }
  deleteWorkoutDefinition(id: string) { return this.http.delete<void>(`${this.base}/workout-definitions/${id}`); }

  workouts(opts: any) { return this.http.get<PagedResult<WorkoutEntry>>(`${this.base}/workouts`, { params: this.params(opts) }); }
  createWorkout(body: any) { return this.http.post<WorkoutEntry>(`${this.base}/workouts`, body); }
  updateWorkout(id: string, body: any) { return this.http.put<WorkoutEntry>(`${this.base}/workouts/${id}`, body); }
  deleteWorkout(id: string) { return this.http.delete<void>(`${this.base}/workouts/${id}`); }
  downloadWorkouts(from: string, to: string, workoutName: string, format: ReportFormat): Observable<HttpResponse<Blob>> { return this.http.get(`${this.base}/workouts/reports`, { params: this.params({ from, to, workoutName, format }), responseType: 'blob', observe: 'response' }); }

  foods() { return this.http.get<FoodDefinition[]>(`${this.base}/foods`); }
  createFood(body: any) { return this.http.post<FoodDefinition>(`${this.base}/foods`, body); }
  updateFood(id: string, body: any) { return this.http.put<FoodDefinition>(`${this.base}/foods/${id}`, body); }
  deleteFood(id: string) { return this.http.delete<void>(`${this.base}/foods/${id}`); }

  nutrition(opts: any) { return this.http.get<PagedResult<NutritionEntry>>(`${this.base}/nutrition`, { params: this.params(opts) }); }
  createNutrition(body: any) { return this.http.post<NutritionEntry>(`${this.base}/nutrition`, body); }
  updateNutrition(id: string, body: any) { return this.http.put<NutritionEntry>(`${this.base}/nutrition/${id}`, body); }
  deleteNutrition(id: string) { return this.http.delete<void>(`${this.base}/nutrition/${id}`); }
  goal() { return this.http.get<NutritionGoal>(`${this.base}/nutrition-goal`); }
  saveGoal(body: any) { return this.http.put<NutritionGoal>(`${this.base}/nutrition-goal`, body); }
  day(date: string) { return this.http.get<NutritionDay>(`${this.base}/nutrition/day`, { params: this.params({ date }) }); }
  downloadNutrition(from: string, to: string, format: ReportFormat): Observable<HttpResponse<Blob>> { return this.http.get(`${this.base}/nutrition/reports`, { params: this.params({ from, to, format }), responseType: 'blob', observe: 'response' }); }

  water(opts: any) { return this.http.get<PagedResult<WaterIntakeEntry>>(`${this.base}/water`, { params: this.params(opts) }); }
  createWater(body: any) { return this.http.post<WaterIntakeEntry>(`${this.base}/water`, body); }
  updateWater(id: string, body: any) { return this.http.put<WaterIntakeEntry>(`${this.base}/water/${id}`, body); }
  deleteWater(id: string) { return this.http.delete<void>(`${this.base}/water/${id}`); }
  downloadWater(from: string, to: string, format: ReportFormat): Observable<HttpResponse<Blob>> { return this.http.get(`${this.base}/water/reports`, { params: this.params({ from, to, format }), responseType: 'blob', observe: 'response' }); }
}
