import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { PasswordEntry, PasswordGroup, PasswordHistory, VaultStatus } from './password-vault.models';

@Injectable({ providedIn: 'root' })
export class PasswordVaultApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/password-vault`;
  status() { return this.http.get<VaultStatus>(`${this.base}/status`); }
  initialize(body: any) { return this.http.post<VaultStatus>(`${this.base}/initialize`, body); }
  groups() { return this.http.get<PasswordGroup[]>(`${this.base}/groups`); }
  createGroup(body: any) { return this.http.post<PasswordGroup>(`${this.base}/groups`, body); }
  updateGroup(id: string, body: any) { return this.http.put<PasswordGroup>(`${this.base}/groups/${id}`, body); }
  deleteGroup(id: string) { return this.http.delete<void>(`${this.base}/groups/${id}`); }
  entries(opts: { groupId?: string; search?: string } = {}) {
    let p = new HttpParams();
    if (opts.groupId) p = p.set('groupId', opts.groupId);
    if (opts.search) p = p.set('search', opts.search);
    return this.http.get<PasswordEntry[]>(`${this.base}/entries`, { params: p });
  }
  createEntry(body: any) { return this.http.post<PasswordEntry>(`${this.base}/entries`, body); }
  updateEntry(id: string, body: any) { return this.http.put<PasswordEntry>(`${this.base}/entries/${id}`, body); }
  deleteEntry(id: string) { return this.http.delete<void>(`${this.base}/entries/${id}`); }
  addHistory(entryId: string, body: any) { return this.http.post<PasswordHistory>(`${this.base}/entries/${entryId}/history`, body); }
}
