import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Asset,
  AssetGroup,
  AssetPriceEntry,
  AssetStatus,
  AssetTag,
  AssetTrackerDashboard,
  AssetWithHistory,
  Investment,
  InvestmentDetail,
  InvestmentGroup,
  InvestmentPriceEntry,
  InvestmentStatus,
  InvestmentTx,
  InvestmentTxType,
  Liability,
  LiabilityDetail,
  LiabilityHistoryEntry,
  LiabilityStatus,
  LiabilityTxType,
  ReportFormat
} from './asset-tracker.models';

@Injectable({ providedIn: 'root' })
export class AssetTrackerApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/asset-tracker`;

  // ===== Tags =====
  listTags(): Observable<AssetTag[]> { return this.http.get<AssetTag[]>(`${this.base}/tags`); }
  createTag(body: { name: string; description?: string | null; color: string }) {
    return this.http.post<AssetTag>(`${this.base}/tags`, body);
  }
  updateTag(id: string, body: { name: string; description?: string | null; color: string }) {
    return this.http.put<AssetTag>(`${this.base}/tags/${id}`, body);
  }
  deleteTag(id: string) { return this.http.delete<void>(`${this.base}/tags/${id}`); }

  // ===== Assets =====
  listAssetGroups(): Observable<AssetGroup[]> { return this.http.get<AssetGroup[]>(`${this.base}/asset-groups`); }
  createAssetGroup(body: { name: string; description?: string | null; tagId: string | null }) {
    return this.http.post<AssetGroup>(`${this.base}/asset-groups`, body);
  }
  updateAssetGroup(id: string, body: { name: string; description?: string | null; tagId: string | null }) {
    return this.http.put<AssetGroup>(`${this.base}/asset-groups/${id}`, body);
  }
  deleteAssetGroup(id: string) { return this.http.delete<void>(`${this.base}/asset-groups/${id}`); }

  listAssets(opts: { status?: AssetStatus; tagId?: string; groupId?: string } = {}): Observable<Asset[]> {
    let p = new HttpParams();
    if (opts.status) p = p.set('status', String(opts.status));
    if (opts.tagId) p = p.set('tagId', opts.tagId);
    if (opts.groupId) p = p.set('groupId', opts.groupId);
    return this.http.get<Asset[]>(`${this.base}/assets`, { params: p });
  }
  getAsset(id: string): Observable<AssetWithHistory> { return this.http.get<AssetWithHistory>(`${this.base}/assets/${id}`); }
  createAsset(body: any) { return this.http.post<Asset>(`${this.base}/assets`, body); }
  updateAsset(id: string, body: any) { return this.http.put<Asset>(`${this.base}/assets/${id}`, body); }
  deleteAsset(id: string) { return this.http.delete<void>(`${this.base}/assets/${id}`); }

  addAssetPrice(assetId: string, body: { asOf: string; price: number; note: string | null }) {
    return this.http.post<AssetPriceEntry>(`${this.base}/assets/${assetId}/price-history`, body);
  }
  updateAssetPrice(assetId: string, priceId: string, body: { asOf: string; price: number; note: string | null }) {
    return this.http.put<AssetPriceEntry>(`${this.base}/assets/${assetId}/price-history/${priceId}`, body);
  }
  deleteAssetPrice(assetId: string, priceId: string) {
    return this.http.delete<void>(`${this.base}/assets/${assetId}/price-history/${priceId}`);
  }

  downloadAssetsReport(status: AssetStatus | null, format: ReportFormat): Observable<HttpResponse<Blob>> {
    let p = new HttpParams().set('format', format);
    if (status) p = p.set('status', String(status));
    return this.http.get(`${this.base}/assets/reports`, { params: p, responseType: 'blob', observe: 'response' });
  }

  // ===== Investments =====
  listInvestmentGroups(status?: InvestmentStatus): Observable<InvestmentGroup[]> {
    let p = new HttpParams();
    if (status) p = p.set('status', String(status));
    return this.http.get<InvestmentGroup[]>(`${this.base}/investment-groups`, { params: p });
  }
  createInvestmentGroup(body: { name: string; description?: string | null; tagId: string | null; status: InvestmentStatus }) {
    return this.http.post<InvestmentGroup>(`${this.base}/investment-groups`, body);
  }
  updateInvestmentGroup(id: string, body: { name: string; description?: string | null; tagId: string | null; status: InvestmentStatus }) {
    return this.http.put<InvestmentGroup>(`${this.base}/investment-groups/${id}`, body);
  }
  deleteInvestmentGroup(id: string) { return this.http.delete<void>(`${this.base}/investment-groups/${id}`); }

  listInvestments(opts: { status?: InvestmentStatus; tagId?: string; groupId?: string } = {}): Observable<Investment[]> {
    let p = new HttpParams();
    if (opts.status) p = p.set('status', String(opts.status));
    if (opts.tagId) p = p.set('tagId', opts.tagId);
    if (opts.groupId) p = p.set('groupId', opts.groupId);
    return this.http.get<Investment[]>(`${this.base}/investments`, { params: p });
  }
  getInvestment(id: string): Observable<InvestmentDetail> { return this.http.get<InvestmentDetail>(`${this.base}/investments/${id}`); }
  createInvestment(body: any) { return this.http.post<Investment>(`${this.base}/investments`, body); }
  updateInvestment(id: string, body: any) { return this.http.put<Investment>(`${this.base}/investments/${id}`, body); }
  deleteInvestment(id: string) { return this.http.delete<void>(`${this.base}/investments/${id}`); }

  addInvestmentPrice(invId: string, body: { asOf: string; price: number; note: string | null }) {
    return this.http.post<InvestmentPriceEntry>(`${this.base}/investments/${invId}/price-history`, body);
  }
  updateInvestmentPrice(invId: string, priceId: string, body: { asOf: string; price: number; note: string | null }) {
    return this.http.put<InvestmentPriceEntry>(`${this.base}/investments/${invId}/price-history/${priceId}`, body);
  }
  deleteInvestmentPrice(invId: string, priceId: string) {
    return this.http.delete<void>(`${this.base}/investments/${invId}/price-history/${priceId}`);
  }

  addInvestmentTx(invId: string, body: { date: string; type: InvestmentTxType; units: number; price: number | null; note: string | null }) {
    return this.http.post<InvestmentTx>(`${this.base}/investments/${invId}/transactions`, body);
  }
  updateInvestmentTx(invId: string, txId: string, body: { date: string; type: InvestmentTxType; units: number; price: number | null; note: string | null }) {
    return this.http.put<InvestmentTx>(`${this.base}/investments/${invId}/transactions/${txId}`, body);
  }
  deleteInvestmentTx(invId: string, txId: string) {
    return this.http.delete<void>(`${this.base}/investments/${invId}/transactions/${txId}`);
  }

  downloadInvestmentsReport(status: InvestmentStatus | null, format: ReportFormat): Observable<HttpResponse<Blob>> {
    let p = new HttpParams().set('format', format);
    if (status) p = p.set('status', String(status));
    return this.http.get(`${this.base}/investments/reports`, { params: p, responseType: 'blob', observe: 'response' });
  }

  // ===== Liabilities =====
  listLiabilities(status?: LiabilityStatus): Observable<Liability[]> {
    let p = new HttpParams();
    if (status) p = p.set('status', String(status));
    return this.http.get<Liability[]>(`${this.base}/liabilities`, { params: p });
  }
  getLiability(id: string): Observable<LiabilityDetail> {
    return this.http.get<LiabilityDetail>(`${this.base}/liabilities/${id}`);
  }
  createLiability(body: { name: string; description?: string | null; tagId: string | null; initialAmount: number; date: string | null; note: string | null }) {
    return this.http.post<Liability>(`${this.base}/liabilities`, body);
  }
  updateLiability(id: string, body: { name: string; description?: string | null; tagId: string | null }) {
    return this.http.put<Liability>(`${this.base}/liabilities/${id}`, body);
  }
  deleteLiability(id: string) { return this.http.delete<void>(`${this.base}/liabilities/${id}`); }

  addLiabilityHistory(id: string, body: { date: string; type: LiabilityTxType; amount: number; note: string | null }) {
    return this.http.post<LiabilityHistoryEntry>(`${this.base}/liabilities/${id}/history`, body);
  }
  updateLiabilityHistory(id: string, hId: string, body: { date: string; type: LiabilityTxType; amount: number; note: string | null }) {
    return this.http.put<LiabilityHistoryEntry>(`${this.base}/liabilities/${id}/history/${hId}`, body);
  }
  deleteLiabilityHistory(id: string, hId: string) {
    return this.http.delete<void>(`${this.base}/liabilities/${id}/history/${hId}`);
  }

  downloadLiabilitiesReport(status: LiabilityStatus | null, format: ReportFormat): Observable<HttpResponse<Blob>> {
    let p = new HttpParams().set('format', format);
    if (status) p = p.set('status', String(status));
    return this.http.get(`${this.base}/liabilities/reports`, { params: p, responseType: 'blob', observe: 'response' });
  }

  // ===== Dashboard =====
  getDashboard(from: string, to: string): Observable<AssetTrackerDashboard> {
    return this.http.get<AssetTrackerDashboard>(`${this.base}/dashboard`, {
      params: new HttpParams().set('from', from).set('to', to)
    });
  }
}
