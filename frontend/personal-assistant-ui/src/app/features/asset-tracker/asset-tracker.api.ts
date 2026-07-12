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
  InvestmentEntry,
  InvestmentPriceEntry,
  InvestmentStatistics,
  InvestmentStatus,
  InvestmentType,
  InvestmentTxType,
  JewelleryItem,
  Liability,
  LiabilityDetail,
  LiabilityHistoryEntry,
  LiabilityAccount,
  LiabilityAccountEntry,
  LiabilityAccountStatistics,
  LiabilityAccountStatus,
  LiabilityAccountStatusEntry,
  LiabilityAccountTxType,
  LiabilityStatus,
  LiabilityTxType,
  PersonalAssetItem,
  PreciousMetal,
  PreciousMetalEntry,
  PreciousMetalPriceEntry,
  PreciousMetalStatistics,
  PreciousMetalTxType,
  ReportFormat,
  PagedResult
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
  listInvestments(opts: {
    search?: string; status?: InvestmentStatus; type?: InvestmentType; tagId?: string;
    sortBy?: string; sortDirection?: string; page?: number; pageSize?: number;
  } = {}): Observable<PagedResult<Investment>> {
    let p = new HttpParams();
    if (opts.search) p = p.set('search', opts.search);
    if (opts.status) p = p.set('status', String(opts.status));
    if (opts.type) p = p.set('type', String(opts.type));
    if (opts.tagId) p = p.set('tagId', opts.tagId);
    if (opts.sortBy) p = p.set('sortBy', opts.sortBy);
    if (opts.sortDirection) p = p.set('sortDirection', opts.sortDirection);
    p = p.set('page', String(opts.page ?? 1)).set('pageSize', String(opts.pageSize ?? 25));
    return this.http.get<PagedResult<Investment>>(`${this.base}/investments`, { params: p });
  }
  getInvestment(id: string): Observable<InvestmentDetail> { return this.http.get<InvestmentDetail>(`${this.base}/investments/${id}`); }
  createInvestment(body: any) { return this.http.post<Investment>(`${this.base}/investments`, body); }
  updateInvestment(id: string, body: any) { return this.http.patch<Investment>(`${this.base}/investments/${id}`, body); }
  deleteInvestment(id: string) { return this.http.delete<void>(`${this.base}/investments/${id}`); }

  changeInvestmentStatus(invId: string, body: { status: InvestmentStatus; effectiveDate: string }) {
    return this.http.post(`${this.base}/investments/${invId}/status-history`, body);
  }
  listInvestmentEntries(invId: string, page = 1) {
    return this.http.get<PagedResult<InvestmentEntry>>(`${this.base}/investments/${invId}/entries`, {
      params: new HttpParams().set('page', page).set('pageSize', 10)
    });
  }
  addInvestmentEntry(invId: string, body: any) {
    return this.http.post<InvestmentEntry>(`${this.base}/investments/${invId}/entries`, body);
  }
  updateInvestmentEntry(invId: string, entryId: string, body: any) {
    return this.http.patch<InvestmentEntry>(`${this.base}/investments/${invId}/entries/${entryId}`, body);
  }
  deleteInvestmentEntry(invId: string, entryId: string) {
    return this.http.delete<void>(`${this.base}/investments/${invId}/entries/${entryId}`);
  }
  listInvestmentPrices(invId: string, page = 1) {
    return this.http.get<PagedResult<InvestmentPriceEntry>>(`${this.base}/investments/${invId}/price-history`, {
      params: new HttpParams().set('page', page).set('pageSize', 10)
    });
  }
  addInvestmentPrice(invId: string, body: { date: string; pricePerUnit: number }) {
    return this.http.post<InvestmentPriceEntry>(`${this.base}/investments/${invId}/price-history`, body);
  }
  updateInvestmentPrice(invId: string, priceId: string, body: { date: string; pricePerUnit: number }) {
    return this.http.patch<InvestmentPriceEntry>(`${this.base}/investments/${invId}/price-history/${priceId}`, body);
  }
  deleteInvestmentPrice(invId: string, priceId: string) {
    return this.http.delete<void>(`${this.base}/investments/${invId}/price-history/${priceId}`);
  }

  getInvestmentStatistics(invId: string, source: 'Entries' | 'Prices', duration: string) {
    return this.http.get<InvestmentStatistics>(`${this.base}/investments/${invId}/statistics`, {
      params: new HttpParams().set('source', source).set('duration', duration)
    });
  }

  exportInvestments(body: any): Observable<HttpResponse<Blob>> {
    return this.http.post(`${this.base}/investments/exports`, body, { responseType: 'blob', observe: 'response' });
  }

  // ===== Precious Metals =====
  listPreciousMetals(opts: { search?: string; sortBy?: string; sortDirection?: string; page?: number; pageSize?: number } = {}): Observable<PagedResult<PreciousMetal>> {
    let p = new HttpParams();
    if (opts.search) p = p.set('search', opts.search);
    if (opts.sortBy) p = p.set('sortBy', opts.sortBy);
    if (opts.sortDirection) p = p.set('sortDirection', opts.sortDirection);
    p = p.set('page', String(opts.page ?? 1)).set('pageSize', String(opts.pageSize ?? 25));
    return this.http.get<PagedResult<PreciousMetal>>(`${this.base}/precious-metals`, { params: p });
  }
  createPreciousMetal(body: { name: string; description: string | null }) {
    return this.http.post<PreciousMetal>(`${this.base}/precious-metals`, body);
  }
  updatePreciousMetal(id: string, body: { name: string; description: string | null; creationDate: string }) {
    return this.http.patch<PreciousMetal>(`${this.base}/precious-metals/${id}`, body);
  }
  deletePreciousMetal(id: string) { return this.http.delete<void>(`${this.base}/precious-metals/${id}`); }
  listPreciousMetalEntries(id: string, page = 1) {
    return this.http.get<PagedResult<PreciousMetalEntry>>(`${this.base}/precious-metals/${id}/entries`, {
      params: new HttpParams().set('page', page).set('pageSize', 10)
    });
  }
  addPreciousMetalEntry(id: string, body: { type: PreciousMetalTxType; date: string; note: string | null; quantity: number; pricePerUnit: number }) {
    return this.http.post<PreciousMetalEntry>(`${this.base}/precious-metals/${id}/entries`, body);
  }
  updatePreciousMetalEntry(id: string, entryId: string, body: { type: PreciousMetalTxType; date: string; note: string | null; quantity: number; pricePerUnit: number }) {
    return this.http.patch<PreciousMetalEntry>(`${this.base}/precious-metals/${id}/entries/${entryId}`, body);
  }
  deletePreciousMetalEntry(id: string, entryId: string) { return this.http.delete<void>(`${this.base}/precious-metals/${id}/entries/${entryId}`); }
  listPreciousMetalPrices(id: string, page = 1) {
    return this.http.get<PagedResult<PreciousMetalPriceEntry>>(`${this.base}/precious-metals/${id}/price-history`, {
      params: new HttpParams().set('page', page).set('pageSize', 10)
    });
  }
  addPreciousMetalPrice(id: string, body: { date: string; pricePerUnit: number }) {
    return this.http.post<PreciousMetalPriceEntry>(`${this.base}/precious-metals/${id}/price-history`, body);
  }
  deletePreciousMetalPrice(id: string, priceId: string) { return this.http.delete<void>(`${this.base}/precious-metals/${id}/price-history/${priceId}`); }
  getPreciousMetalStatistics(id: string, source: 'Entries' | 'Prices', duration: string) {
    return this.http.get<PreciousMetalStatistics>(`${this.base}/precious-metals/${id}/statistics`, {
      params: new HttpParams().set('source', source).set('duration', duration)
    });
  }
  exportPreciousMetals(body: any): Observable<HttpResponse<Blob>> {
    return this.http.post(`${this.base}/precious-metals/exports`, body, { responseType: 'blob', observe: 'response' });
  }

  // ===== Jewellery =====
  listJewellery(opts: { search?: string; status?: AssetStatus | 'all'; sortBy?: string; sortDirection?: string; page?: number; pageSize?: number } = {}): Observable<PagedResult<JewelleryItem>> {
    let p = new HttpParams();
    if (opts.search) p = p.set('search', opts.search);
    if (opts.status && opts.status !== 'all') p = p.set('status', String(opts.status));
    if (opts.sortBy) p = p.set('sortBy', opts.sortBy);
    if (opts.sortDirection) p = p.set('sortDirection', opts.sortDirection);
    p = p.set('page', String(opts.page ?? 1)).set('pageSize', String(opts.pageSize ?? 25));
    return this.http.get<PagedResult<JewelleryItem>>(`${this.base}/jewellery`, { params: p });
  }
  createJewellery(body: any) { return this.http.post<JewelleryItem>(`${this.base}/jewellery`, body); }
  updateJewellery(id: string, body: any) { return this.http.patch<JewelleryItem>(`${this.base}/jewellery/${id}`, body); }
  sellJewellery(id: string, body: { sellingNote: string | null; sellingDate: string; sellingPrice: number }) {
    return this.http.post<JewelleryItem>(`${this.base}/jewellery/${id}/sell`, body);
  }
  deleteJewellery(id: string) { return this.http.delete<void>(`${this.base}/jewellery/${id}`); }
  exportJewellery(body: any): Observable<HttpResponse<Blob>> {
    return this.http.post(`${this.base}/jewellery/exports`, body, { responseType: 'blob', observe: 'response' });
  }

  // ===== Personal Assets =====
  listPersonalAssets(opts: { search?: string; status?: AssetStatus | 'all'; sortBy?: string; sortDirection?: string; page?: number; pageSize?: number } = {}): Observable<PagedResult<PersonalAssetItem>> {
    let p = new HttpParams();
    if (opts.search) p = p.set('search', opts.search);
    if (opts.status && opts.status !== 'all') p = p.set('status', String(opts.status));
    if (opts.sortBy) p = p.set('sortBy', opts.sortBy);
    if (opts.sortDirection) p = p.set('sortDirection', opts.sortDirection);
    p = p.set('page', String(opts.page ?? 1)).set('pageSize', String(opts.pageSize ?? 25));
    return this.http.get<PagedResult<PersonalAssetItem>>(`${this.base}/personal-assets`, { params: p });
  }
  createPersonalAsset(body: any) { return this.http.post<PersonalAssetItem>(`${this.base}/personal-assets`, body); }
  updatePersonalAsset(id: string, body: any) { return this.http.patch<PersonalAssetItem>(`${this.base}/personal-assets/${id}`, body); }
  sellPersonalAsset(id: string, body: { sellingNote: string | null; sellingDate: string; sellingPrice: number }) {
    return this.http.post<PersonalAssetItem>(`${this.base}/personal-assets/${id}/sell`, body);
  }
  deletePersonalAsset(id: string) { return this.http.delete<void>(`${this.base}/personal-assets/${id}`); }
  exportPersonalAssets(body: any): Observable<HttpResponse<Blob>> {
    return this.http.post(`${this.base}/personal-assets/exports`, body, { responseType: 'blob', observe: 'response' });
  }

  // ===== Liability Accounts =====
  listLiabilityAccounts(kind: 'loans' | 'debts' | 'credit-cards', opts: {
    search?: string; status?: LiabilityAccountStatus | 'all'; sortBy?: string; sortDirection?: string; page?: number; pageSize?: number;
  } = {}): Observable<PagedResult<LiabilityAccount>> {
    let p = new HttpParams();
    if (opts.search) p = p.set('search', opts.search);
    if (opts.status && opts.status !== 'all') p = p.set('status', String(opts.status));
    if (opts.sortBy) p = p.set('sortBy', opts.sortBy);
    if (opts.sortDirection) p = p.set('sortDirection', opts.sortDirection);
    p = p.set('page', String(opts.page ?? 1)).set('pageSize', String(opts.pageSize ?? 25));
    return this.http.get<PagedResult<LiabilityAccount>>(`${this.base}/${kind}`, { params: p });
  }
  createLiabilityAccount(kind: 'loans' | 'debts' | 'credit-cards', body: any) {
    return this.http.post<LiabilityAccount>(`${this.base}/${kind}`, body);
  }
  updateLiabilityAccount(kind: 'loans' | 'debts' | 'credit-cards', id: string, body: any) {
    return this.http.patch<LiabilityAccount>(`${this.base}/${kind}/${id}`, body);
  }
  deleteLiabilityAccount(kind: 'loans' | 'debts' | 'credit-cards', id: string) {
    return this.http.delete<void>(`${this.base}/${kind}/${id}`);
  }
  listLiabilityAccountStatuses(kind: 'loans' | 'debts' | 'credit-cards', id: string) {
    return this.http.get<LiabilityAccountStatusEntry[]>(`${this.base}/${kind}/${id}/status-history`);
  }
  changeLiabilityAccountStatus(kind: 'loans' | 'debts' | 'credit-cards', id: string, body: { status: LiabilityAccountStatus; effectiveDate: string }) {
    return this.http.post<LiabilityAccountStatusEntry>(`${this.base}/${kind}/${id}/status-history`, body);
  }
  listLiabilityAccountEntries(kind: 'loans' | 'debts' | 'credit-cards', id: string, page = 1) {
    return this.http.get<PagedResult<LiabilityAccountEntry>>(`${this.base}/${kind}/${id}/entries`, {
      params: new HttpParams().set('page', page).set('pageSize', 10)
    });
  }
  addLiabilityAccountEntry(kind: 'loans' | 'debts' | 'credit-cards', id: string, body: { type: LiabilityAccountTxType; date: string; note: string | null; amount: number }) {
    return this.http.post<LiabilityAccountEntry>(`${this.base}/${kind}/${id}/entries`, body);
  }
  updateLiabilityAccountEntry(kind: 'loans' | 'debts' | 'credit-cards', id: string, entryId: string, body: { type: LiabilityAccountTxType; date: string; note: string | null; amount: number }) {
    return this.http.patch<LiabilityAccountEntry>(`${this.base}/${kind}/${id}/entries/${entryId}`, body);
  }
  deleteLiabilityAccountEntry(kind: 'loans' | 'debts' | 'credit-cards', id: string, entryId: string) {
    return this.http.delete<void>(`${this.base}/${kind}/${id}/entries/${entryId}`);
  }
  getLiabilityAccountStatistics(kind: 'loans' | 'debts' | 'credit-cards', id: string, duration: string) {
    return this.http.get<LiabilityAccountStatistics>(`${this.base}/${kind}/${id}/statistics`, {
      params: new HttpParams().set('duration', duration)
    });
  }
  exportLiabilityAccounts(kind: 'loans' | 'debts' | 'credit-cards', body: any): Observable<HttpResponse<Blob>> {
    return this.http.post(`${this.base}/${kind}/exports`, body, { responseType: 'blob', observe: 'response' });
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
