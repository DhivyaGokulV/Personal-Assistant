import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult, ReportFormat, TimeEntry, TimeReport } from './time-tracker.models';

@Injectable({ providedIn: 'root' })
export class TimeTrackerApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/time-tracker`;

  list(opts: { from?: string; to?: string; activity?: string; tag?: string; page?: number; pageSize?: number }) {
    let p = new HttpParams().set('page', opts.page ?? 1).set('pageSize', opts.pageSize ?? 25);
    if (opts.from) p = p.set('from', opts.from);
    if (opts.to) p = p.set('to', opts.to);
    if (opts.activity) p = p.set('activity', opts.activity);
    if (opts.tag) p = p.set('tag', opts.tag);
    return this.http.get<PagedResult<TimeEntry>>(`${this.base}/entries`, { params: p });
  }

  create(body: any): Observable<TimeEntry> { return this.http.post<TimeEntry>(`${this.base}/entries`, body); }
  update(id: string, body: any): Observable<TimeEntry> { return this.http.put<TimeEntry>(`${this.base}/entries/${id}`, body); }
  delete(id: string) { return this.http.delete<void>(`${this.base}/entries/${id}`); }

  report(opts: { from: string; to: string; activity?: string; tag?: string }): Observable<TimeReport> {
    let p = new HttpParams().set('from', opts.from).set('to', opts.to);
    if (opts.activity) p = p.set('activity', opts.activity);
    if (opts.tag) p = p.set('tag', opts.tag);
    return this.http.get<TimeReport>(`${this.base}/reports`, { params: p });
  }

  download(opts: { from: string; to: string; activity?: string; tag?: string; format: ReportFormat }): Observable<HttpResponse<Blob>> {
    let p = new HttpParams().set('from', opts.from).set('to', opts.to).set('format', opts.format);
    if (opts.activity) p = p.set('activity', opts.activity);
    if (opts.tag) p = p.set('tag', opts.tag);
    return this.http.get(`${this.base}/reports`, { params: p, responseType: 'blob', observe: 'response' });
  }
}
