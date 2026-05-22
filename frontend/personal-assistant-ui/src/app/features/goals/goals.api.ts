import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Goal, GoalPlan, GoalStep, ReportFormat } from './goals.models';

@Injectable({ providedIn: 'root' })
export class GoalsApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/goals`;
  plans() { return this.http.get<GoalPlan[]>(`${this.base}/plans`); }
  createPlan(body: any) { return this.http.post<GoalPlan>(`${this.base}/plans`, body); }
  updatePlan(id: string, body: any) { return this.http.put<GoalPlan>(`${this.base}/plans/${id}`, body); }
  deletePlan(id: string) { return this.http.delete<void>(`${this.base}/plans/${id}`); }
  createGoal(planId: string, body: any) { return this.http.post<Goal>(`${this.base}/plans/${planId}/goals`, body); }
  updateGoal(id: string, body: any) { return this.http.put<Goal>(`${this.base}/goals/${id}`, body); }
  deleteGoal(id: string) { return this.http.delete<void>(`${this.base}/goals/${id}`); }
  createStep(goalId: string, body: any) { return this.http.post<GoalStep>(`${this.base}/goals/${goalId}/steps`, body); }
  updateStep(id: string, body: any) { return this.http.put<GoalStep>(`${this.base}/steps/${id}`, body); }
  deleteStep(id: string) { return this.http.delete<void>(`${this.base}/steps/${id}`); }
  download(from: string, to: string, format: ReportFormat): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.base}/reports`, { params: new HttpParams().set('from', from).set('to', to).set('format', format), responseType: 'blob', observe: 'response' });
  }
}
