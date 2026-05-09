import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  DailyByDateView,
  DailyCompletion,
  DailyGroup,
  DailyTask,
  TaskActiveStatus
} from './daily-tasks.models';
import {
  DailyDayWiseReport,
  DailyTaskWiseReport,
  ReportFormat,
  ReportKind
} from './daily-reports.models';

@Injectable({ providedIn: 'root' })
export class DailyTasksApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getGroups(): Observable<DailyGroup[]> {
    return this.http.get<DailyGroup[]>(`${this.base}/daily-groups`);
  }

  createGroup(req: { name: string; description?: string | null }): Observable<DailyGroup> {
    return this.http.post<DailyGroup>(`${this.base}/daily-groups`, req);
  }

  updateGroup(id: string, req: { name: string; description?: string | null }): Observable<DailyGroup> {
    return this.http.put<DailyGroup>(`${this.base}/daily-groups/${id}`, req);
  }

  deleteGroup(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/daily-groups/${id}`);
  }

  getTasks(opts: { groupId?: string; includeInactive?: boolean } = {}): Observable<DailyTask[]> {
    let params = new HttpParams();
    if (opts.groupId) params = params.set('groupId', opts.groupId);
    if (opts.includeInactive) params = params.set('includeInactive', 'true');
    return this.http.get<DailyTask[]>(`${this.base}/daily-tasks`, { params });
  }

  createTask(req: { groupId: string; title: string; description?: string | null; status: TaskActiveStatus }): Observable<DailyTask> {
    return this.http.post<DailyTask>(`${this.base}/daily-tasks`, req);
  }

  updateTask(id: string, req: { groupId: string; title: string; description?: string | null; status: TaskActiveStatus }): Observable<DailyTask> {
    return this.http.put<DailyTask>(`${this.base}/daily-tasks/${id}`, req);
  }

  deleteTask(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/daily-tasks/${id}`);
  }

  getByDate(date: string): Observable<DailyByDateView> {
    return this.http.get<DailyByDateView>(`${this.base}/daily-tasks/by-date`, {
      params: new HttpParams().set('date', date)
    });
  }

  upsertCompletion(taskId: string, date: string, body: { isCompleted: boolean; note: string | null }): Observable<DailyCompletion> {
    return this.http.put<DailyCompletion>(
      `${this.base}/daily-tasks/${taskId}/completion`,
      body,
      { params: new HttpParams().set('date', date) }
    );
  }

  getDayWiseReport(from: string, to: string): Observable<DailyDayWiseReport> {
    return this.http.get<DailyDayWiseReport>(`${this.base}/daily-tasks/reports/day-wise`, {
      params: new HttpParams().set('from', from).set('to', to).set('format', 'Json')
    });
  }

  getTaskWiseReport(from: string, to: string): Observable<DailyTaskWiseReport> {
    return this.http.get<DailyTaskWiseReport>(`${this.base}/daily-tasks/reports/task-wise`, {
      params: new HttpParams().set('from', from).set('to', to).set('format', 'Json')
    });
  }

  downloadReport(kind: ReportKind, from: string, to: string, format: ReportFormat): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.base}/daily-tasks/reports/${kind}`, {
      params: new HttpParams().set('from', from).set('to', to).set('format', format),
      responseType: 'blob',
      observe: 'response'
    });
  }
}
