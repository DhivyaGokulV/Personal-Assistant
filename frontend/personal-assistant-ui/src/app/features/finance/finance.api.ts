import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Account,
  AccountStatus,
  Budget,
  BudgetReport,
  Category,
  CategoryType,
  DashboardView,
  PagedResult,
  PaymentType,
  ReportFormat,
  Tag,
  Transaction,
  TransactionType
} from './finance.models';

@Injectable({ providedIn: 'root' })
export class FinanceApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/finance`;

  // ===== Settings — Accounts =====
  getAccounts(includeInactive = false): Observable<Account[]> {
    let params = new HttpParams();
    if (includeInactive) params = params.set('includeInactive', 'true');
    return this.http.get<Account[]>(`${this.base}/accounts`, { params });
  }
  createAccount(body: { name: string; description?: string | null; openingBalance: number; openingDate: string; status: AccountStatus }) {
    return this.http.post<Account>(`${this.base}/accounts`, body);
  }
  updateAccount(id: string, body: { name: string; description?: string | null; openingBalance: number; openingDate: string; status: AccountStatus }) {
    return this.http.put<Account>(`${this.base}/accounts/${id}`, body);
  }
  deleteAccount(id: string) { return this.http.delete<void>(`${this.base}/accounts/${id}`); }

  // ===== Settings — Categories =====
  getCategories(): Observable<Category[]> { return this.http.get<Category[]>(`${this.base}/categories`); }
  createCategory(body: { name: string; type: CategoryType }) { return this.http.post<Category>(`${this.base}/categories`, body); }
  updateCategory(id: string, body: { name: string; type: CategoryType }) { return this.http.put<Category>(`${this.base}/categories/${id}`, body); }
  deleteCategory(id: string) { return this.http.delete<void>(`${this.base}/categories/${id}`); }

  // ===== Settings — Payment Types =====
  getPaymentTypes(): Observable<PaymentType[]> { return this.http.get<PaymentType[]>(`${this.base}/payment-types`); }
  createPaymentType(body: { name: string; description?: string | null }) { return this.http.post<PaymentType>(`${this.base}/payment-types`, body); }
  updatePaymentType(id: string, body: { name: string; description?: string | null }) { return this.http.put<PaymentType>(`${this.base}/payment-types/${id}`, body); }
  deletePaymentType(id: string) { return this.http.delete<void>(`${this.base}/payment-types/${id}`); }

  // ===== Settings — Tags =====
  getTags(): Observable<Tag[]> { return this.http.get<Tag[]>(`${this.base}/tags`); }
  createTag(body: { name: string; description?: string | null; color: string }) { return this.http.post<Tag>(`${this.base}/tags`, body); }
  updateTag(id: string, body: { name: string; description?: string | null; color: string }) { return this.http.put<Tag>(`${this.base}/tags/${id}`, body); }
  deleteTag(id: string) { return this.http.delete<void>(`${this.base}/tags/${id}`); }

  // ===== Transactions =====
  listTransactions(opts: {
    from?: string; to?: string; accountId?: string; categoryId?: string; paymentTypeId?: string; tagId?: string;
    type?: TransactionType; search?: string; page?: number; pageSize?: number;
  } = {}): Observable<PagedResult<Transaction>> {
    let p = new HttpParams();
    if (opts.from) p = p.set('from', opts.from);
    if (opts.to) p = p.set('to', opts.to);
    if (opts.accountId) p = p.set('accountId', opts.accountId);
    if (opts.categoryId) p = p.set('categoryId', opts.categoryId);
    if (opts.paymentTypeId) p = p.set('paymentTypeId', opts.paymentTypeId);
    if (opts.tagId) p = p.set('tagId', opts.tagId);
    if (opts.type !== undefined) p = p.set('type', String(opts.type));
    if (opts.search) p = p.set('search', opts.search);
    p = p.set('page', String(opts.page ?? 1));
    p = p.set('pageSize', String(opts.pageSize ?? 25));
    return this.http.get<PagedResult<Transaction>>(`${this.base}/transactions`, { params: p });
  }

  createTransaction(body: {
    date: string; type: TransactionType; accountId: string; amount: number;
    reason: string; note?: string | null; categoryId?: string | null; paymentTypeId?: string | null; tagIds?: string[];
  }) { return this.http.post<Transaction>(`${this.base}/transactions`, body); }

  updateTransaction(id: string, body: {
    date: string; type: TransactionType; accountId: string; amount: number;
    reason: string; note?: string | null; categoryId?: string | null; paymentTypeId?: string | null; tagIds?: string[];
  }) { return this.http.put<Transaction>(`${this.base}/transactions/${id}`, body); }

  deleteTransaction(id: string) { return this.http.delete<void>(`${this.base}/transactions/${id}`); }

  createTransfer(body: {
    date: string; sourceAccountId: string; destinationAccountId: string; amount: number;
    reason: string; note?: string | null; paymentTypeId?: string | null; tagIds?: string[];
  }) { return this.http.post<Transaction[]>(`${this.base}/transactions/transfer`, body); }

  downloadTransactionsReport(from: string, to: string, accountId: string | null, format: ReportFormat) {
    let p = new HttpParams().set('from', from).set('to', to).set('format', format);
    if (accountId) p = p.set('accountId', accountId);
    return this.http.get(`${this.base}/transactions/reports`, {
      params: p, responseType: 'blob', observe: 'response'
    });
  }

  // ===== Dashboard =====
  getDashboard(from: string, to: string): Observable<DashboardView> {
    return this.http.get<DashboardView>(`${this.base}/dashboard`, {
      params: new HttpParams().set('from', from).set('to', to)
    });
  }

  // ===== Budgets =====
  listBudgets(): Observable<Budget[]> { return this.http.get<Budget[]>(`${this.base}/budgets`); }
  getBudget(id: string): Observable<Budget> { return this.http.get<Budget>(`${this.base}/budgets/${id}`); }
  createBudget(body: { name: string; categoryId: string; amount: number; from: string; to: string; note?: string | null; entries?: { categoryId: string; amount: number }[] }) {
    return this.http.post<Budget>(`${this.base}/budgets`, body);
  }
  updateBudget(id: string, body: { name: string; categoryId: string; amount: number; from: string; to: string; note?: string | null; entries?: { categoryId: string; amount: number }[] }) {
    return this.http.put<Budget>(`${this.base}/budgets/${id}`, body);
  }
  deleteBudget(id: string) { return this.http.delete<void>(`${this.base}/budgets/${id}`); }
  getBudgetReport(id: string): Observable<BudgetReport> {
    return this.http.get<BudgetReport>(`${this.base}/budgets/${id}/report`, {
      params: new HttpParams().set('format', 'Json')
    });
  }
  downloadBudgetReport(id: string, format: ReportFormat): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.base}/budgets/${id}/report`, {
      params: new HttpParams().set('format', format),
      responseType: 'blob', observe: 'response'
    });
  }
}
