import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  ReportFormat,
  SortOrder,
  Todo,
  TodoReport,
  TodoSort,
  TodoStatus,
  TodoSummary
} from './todo.models';

@Injectable({ providedIn: 'root' })
export class TodoApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/todos`;

  list(opts: { status?: TodoStatus; sortBy?: TodoSort; order?: SortOrder } = {}): Observable<Todo[]> {
    let params = new HttpParams();
    if (opts.status !== undefined) params = params.set('status', String(opts.status));
    if (opts.sortBy) params = params.set('sortBy', opts.sortBy);
    if (opts.order) params = params.set('order', opts.order);
    return this.http.get<Todo[]>(this.base, { params });
  }

  summary(): Observable<TodoSummary> {
    return this.http.get<TodoSummary>(`${this.base}/summary`);
  }

  create(req: { title: string; description?: string | null; deadline?: string | null; status: TodoStatus }): Observable<Todo> {
    return this.http.post<Todo>(this.base, req);
  }

  update(id: string, req: {
    title: string;
    description?: string | null;
    deadline?: string | null;
    status: TodoStatus;
    completedOn?: string | null;
    statusNote?: string | null;
  }): Observable<Todo> {
    return this.http.put<Todo>(`${this.base}/${id}`, req);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  getReport(asOf: string): Observable<TodoReport> {
    return this.http.get<TodoReport>(`${this.base}/reports`, {
      params: new HttpParams().set('asOf', asOf).set('format', 'Json')
    });
  }

  downloadReport(asOf: string, format: ReportFormat): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.base}/reports`, {
      params: new HttpParams().set('asOf', asOf).set('format', format),
      responseType: 'blob',
      observe: 'response'
    });
  }
}
