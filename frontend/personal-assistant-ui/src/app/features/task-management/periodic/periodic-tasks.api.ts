import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  FrequencyUnit,
  PeriodicGroup,
  PeriodicHistory,
  PeriodicReport,
  PeriodicTask,
  PeriodicTaskWithHistory,
  ReportFormat,
  TaskActiveStatus
} from './periodic-tasks.models';

@Injectable({ providedIn: 'root' })
export class PeriodicTasksApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getGroups(): Observable<PeriodicGroup[]> {
    return this.http.get<PeriodicGroup[]>(`${this.base}/periodic-groups`);
  }

  createGroup(req: { name: string; description?: string | null }): Observable<PeriodicGroup> {
    return this.http.post<PeriodicGroup>(`${this.base}/periodic-groups`, req);
  }

  updateGroup(id: string, req: { name: string; description?: string | null }): Observable<PeriodicGroup> {
    return this.http.put<PeriodicGroup>(`${this.base}/periodic-groups/${id}`, req);
  }

  deleteGroup(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/periodic-groups/${id}`);
  }

  getTasks(includeInactive = false): Observable<PeriodicTask[]> {
    let params = new HttpParams();
    if (includeInactive) params = params.set('includeInactive', 'true');
    return this.http.get<PeriodicTask[]>(`${this.base}/periodic-tasks`, { params });
  }

  getTask(id: string): Observable<PeriodicTaskWithHistory> {
    return this.http.get<PeriodicTaskWithHistory>(`${this.base}/periodic-tasks/${id}`);
  }

  createTask(req: {
    groupId: string;
    title: string;
    description?: string | null;
    status: TaskActiveStatus;
    frequencyValue: number;
    frequencyUnit: FrequencyUnit;
  }): Observable<PeriodicTask> {
    return this.http.post<PeriodicTask>(`${this.base}/periodic-tasks`, req);
  }

  updateTask(id: string, req: {
    groupId: string;
    title: string;
    description?: string | null;
    status: TaskActiveStatus;
    frequencyValue: number;
    frequencyUnit: FrequencyUnit;
  }): Observable<PeriodicTask> {
    return this.http.put<PeriodicTask>(`${this.base}/periodic-tasks/${id}`, req);
  }

  deleteTask(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/periodic-tasks/${id}`);
  }

  addHistory(taskId: string, body: { completedOn: string; note: string | null }): Observable<PeriodicHistory> {
    return this.http.post<PeriodicHistory>(`${this.base}/periodic-tasks/${taskId}/history`, body);
  }

  updateHistory(taskId: string, historyId: string, body: { completedOn: string; note: string | null }): Observable<PeriodicHistory> {
    return this.http.put<PeriodicHistory>(`${this.base}/periodic-tasks/${taskId}/history/${historyId}`, body);
  }

  deleteHistory(taskId: string, historyId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/periodic-tasks/${taskId}/history/${historyId}`);
  }

  getReport(from: string, to: string): Observable<PeriodicReport> {
    return this.http.get<PeriodicReport>(`${this.base}/periodic-tasks/reports`, {
      params: new HttpParams().set('from', from).set('to', to).set('format', 'Json')
    });
  }

  downloadReport(from: string, to: string, format: ReportFormat): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.base}/periodic-tasks/reports`, {
      params: new HttpParams().set('from', from).set('to', to).set('format', format),
      responseType: 'blob',
      observe: 'response'
    });
  }
}
