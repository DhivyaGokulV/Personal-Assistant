// ===== Enums =====
export type AssetStatus = 1 | 2;        // 1 InPossession, 2 Sold
export type InvestmentStatus = 1 | 2;   // 1 Active, 2 Inactive
export type InvestmentType = 1 | 2;     // 1 UnitBased, 2 AmountBased
export type InvestmentTxType = 1 | 2 | 3 | 4; // Buy, Sell, Credit, Debit
export type PreciousMetalTxType = 1 | 2; // Buy, Sell
export type LiabilityStatus = 1 | 2;    // 1 Active, 2 Past
export type LiabilityTxType = 1 | 2;    // 1 Acquisition, 2 Repayment
export type LiabilityAccountCategory = 1 | 2 | 3; // Loan, Debt, CreditCard
export type LiabilityAccountStatus = 1 | 2; // Active, Inactive
export type LiabilityAccountTxType = 1 | 2; // Credit, Debit
export type ReportFormat = 'Json' | 'Csv' | 'Xlsx' | 'Pdf';

export const ASSET_STATUSES = [
  { value: 1 as AssetStatus, label: 'In possession' },
  { value: 2 as AssetStatus, label: 'Sold' }
];

// ===== Tags =====
export interface AssetTag {
  id: string;
  name: string;
  description: string | null;
  color: string;
}

export interface AssetTagBadge {
  id: string;
  name: string;
  color: string;
}

// ===== Assets =====
export interface AssetGroup {
  id: string;
  name: string;
  description: string | null;
  tag: AssetTagBadge | null;
  assetCount: number;
  totalCurrentValue: number;
}

export interface Asset {
  id: string;
  groupId: string;
  groupName: string;
  name: string;
  description: string | null;
  tag: AssetTagBadge | null;
  buyingDate: string | null;
  buyingPrice: number | null;
  sellingDate: string | null;
  sellingPrice: number | null;
  status: AssetStatus;
  currentPrice: number | null;
  lastPriceAsOf: string | null;
}

export interface AssetPriceEntry {
  id: string;
  asOf: string;
  price: number;
  note: string | null;
}

export interface AssetWithHistory {
  asset: Asset;
  history: AssetPriceEntry[];
}

// ===== Investments =====
export interface Investment {
  id: string;
  name: string;
  description: string | null;
  tag: AssetTagBadge | null;
  investmentType: InvestmentType;
  currencyCode: string;
  creationDate: string;
  status: InvestmentStatus;
  units: number;
  amountInvested: number;
  currentPrice: number | null;
  currentValue: number;
  remainingCostBasis: number;
  profitLossPercent: number | null;
}

export interface InvestmentPriceEntry {
  id: string;
  date: string;
  pricePerUnit: number;
}

export interface InvestmentEntry {
  id: string;
  date: string;
  type: InvestmentTxType;
  note: string | null;
  quantity: number | null;
  pricePerUnit: number | null;
  amount: number;
}

export interface InvestmentStatusEntry {
  id: string;
  status: InvestmentStatus;
  effectiveDate: string;
}

export interface InvestmentDetail {
  investment: Investment;
  statusHistory: InvestmentStatusEntry[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface InvestmentStatistics {
  metric: string;
  currencyCode: string;
  points: TimeSeriesPoint[];
}

// ===== Liability Accounts =====
export interface LiabilityAccount {
  id: string;
  category: LiabilityAccountCategory;
  name: string;
  description: string | null;
  creationDate: string;
  status: LiabilityAccountStatus;
  currencyCode: string;
  standingAmount: number;
  lastEntryDate: string | null;
}

export interface LiabilityAccountEntry {
  id: string;
  type: LiabilityAccountTxType;
  date: string;
  note: string | null;
  amount: number;
  runningBalance: number;
}

export interface LiabilityAccountStatusEntry {
  id: string;
  status: LiabilityAccountStatus;
  effectiveDate: string;
}

export interface LiabilityAccountStatistics {
  metric: string;
  currencyCode: string;
  points: TimeSeriesPoint[];
}

// ===== Precious Metals =====
export interface PreciousMetal {
  id: string;
  name: string;
  description: string | null;
  creationDate: string;
  currencyCode: string;
  isDefault: boolean;
  quantity: number;
  currentPrice: number | null;
  currentValue: number;
}

export interface PreciousMetalEntry {
  id: string;
  type: PreciousMetalTxType;
  date: string;
  note: string | null;
  quantity: number;
  pricePerUnit: number;
  amount: number;
}

export interface PreciousMetalPriceEntry {
  id: string;
  date: string;
  pricePerUnit: number;
}

export interface PreciousMetalStatistics {
  metric: string;
  currencyCode: string;
  points: TimeSeriesPoint[];
}

// ===== Jewellery =====
export interface JewelleryItem {
  id: string;
  name: string;
  description: string | null;
  buyingDate: string;
  buyingPrice: number;
  quantityInGrams: number;
  status: AssetStatus;
  sellingDate: string | null;
  sellingPrice: number | null;
  sellingNote: string | null;
  currencyCode: string;
}

// ===== Personal Assets =====
export interface PersonalAssetItem {
  id: string;
  name: string;
  description: string | null;
  buyingDate: string;
  buyingPrice: number;
  status: AssetStatus;
  sellingDate: string | null;
  sellingPrice: number | null;
  sellingNote: string | null;
  currencyCode: string;
}

// ===== Liabilities =====
export interface Liability {
  id: string;
  name: string;
  description: string | null;
  tag: AssetTagBadge | null;
  status: LiabilityStatus;
  currentAmount: number;
  lastUpdate: string | null;
}

export interface LiabilityHistoryEntry {
  id: string;
  date: string;
  type: LiabilityTxType;
  amount: number;
  runningBalance: number;
  note: string | null;
}

export interface LiabilityDetail {
  liability: Liability;
  history: LiabilityHistoryEntry[];
}

// ===== Dashboard =====
export interface SliceItem {
  key: string;
  value: number;
  percentOfTotal: number;
}

export interface TimeSeriesPoint {
  date: string;
  value: number;
}

export interface AssetTrackerDashboard {
  netWorth: number;
  totalAssets: number;
  totalInvestments: number;
  totalLiabilities: number;
  assetsBreakdown: SliceItem[];
  investmentsBreakdown: SliceItem[];
  liabilitiesBreakdown: SliceItem[];
  assetsSeries: TimeSeriesPoint[];
  investmentsSeries: TimeSeriesPoint[];
  liabilitiesSeries: TimeSeriesPoint[];
  netWorthSeries: TimeSeriesPoint[];
}

// ===== Helpers =====
export const CURRENCY = '₹';

export function fmtMoney(n: number | null | undefined, fractionDigits = 2): string {
  if (n === null || n === undefined || Number.isNaN(n)) return '—';
  const sign = n < 0 ? '-' : '';
  const abs = Math.abs(n);
  return `${sign}${CURRENCY}${abs.toLocaleString(undefined, { minimumFractionDigits: fractionDigits, maximumFractionDigits: fractionDigits })}`;
}

export function fmtUnits(n: number | null | undefined): string {
  if (n === null || n === undefined || Number.isNaN(n)) return '—';
  return n.toLocaleString(undefined, { maximumFractionDigits: 4 });
}

export function isoToday(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

export function isoOffset(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}
