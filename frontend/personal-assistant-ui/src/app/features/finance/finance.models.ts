// Shared finance enums + types

export type AccountStatus = 1 | 2;          // 1 Active, 2 Inactive
export type CategoryType = 1 | 2 | 3;       // Need / Want / Saving
export type TransactionType = 1 | 2;        // Credit / Debit
export type ReportFormat = 'Json' | 'Csv' | 'Xlsx' | 'Pdf';

export const CATEGORY_TYPES: { value: CategoryType; label: string; tone: string }[] = [
  { value: 1, label: 'Need', tone: 'tone-amber' },
  { value: 2, label: 'Want', tone: 'tone-violet' },
  { value: 3, label: 'Saving', tone: 'tone-green' }
];

export const TRANSACTION_TYPES: { value: TransactionType; label: string; tone: string }[] = [
  { value: 1, label: 'Credit', tone: 'tone-green' },
  { value: 2, label: 'Debit', tone: 'tone-red' }
];

export interface Account {
  id: string;
  name: string;
  description: string | null;
  openingBalance: number;
  openingDate: string;
  status: AccountStatus;
}

export interface Category {
  id: string;
  name: string;
  type: CategoryType;
}

export interface PaymentType {
  id: string;
  name: string;
  description: string | null;
}

export interface Tag {
  id: string;
  name: string;
  description: string | null;
  color: string;
}

export interface TagBadge {
  id: string;
  name: string;
  color: string;
}

export interface Transaction {
  id: string;
  date: string;
  type: TransactionType;
  accountId: string;
  accountName: string;
  amount: number;
  reason: string;
  note: string | null;
  categoryId: string | null;
  categoryName: string | null;
  paymentTypeId: string | null;
  paymentTypeName: string | null;
  transferGroupId: string | null;
  tags: TagBadge[];
  accountStanding: number | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface AccountStanding {
  accountId: string;
  accountName: string;
  startingStanding: number;
  currentStanding: number;
  delta: number;
}

export interface GroupStat {
  key: string;
  debits: number;
  credits: number;
  net: number;
}

export interface DashboardView {
  from: string;
  to: string;
  totalStartingStanding: number;
  totalCurrentStanding: number;
  totalDebits: number;
  totalCredits: number;
  accounts: AccountStanding[];
  byCategory: GroupStat[];
  byAccount: GroupStat[];
  byPaymentType: GroupStat[];
  byTag: GroupStat[];
}

export interface Budget {
  id: string;
  name: string;
  categoryId: string;
  categoryName: string;
  amount: number;
  from: string;
  to: string;
  note: string | null;
  spent: number;
  remaining: number;
  percentUsed: number;
}

export interface BudgetTransactionRow {
  date: string;
  reason: string;
  accountName: string;
  paymentTypeName: string | null;
  amount: number;
}

export interface BudgetReport {
  budget: Budget;
  transactions: BudgetTransactionRow[];
}

export const CURRENCY = '$';
export function fmtMoney(n: number | null | undefined): string {
  if (n === null || n === undefined) return '—';
  const sign = n < 0 ? '-' : '';
  const abs = Math.abs(n);
  return `${sign}${CURRENCY}${abs.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}
