// ===== Enums =====
export type AssetStatus = 1 | 2;        // 1 InPossession, 2 Sold
export type InvestmentStatus = 1 | 2;   // 1 Active, 2 Inactive
export type InvestmentTxType = 1 | 2;   // 1 Buy, 2 Sell
export type LiabilityStatus = 1 | 2;    // 1 Active, 2 Past
export type LiabilityTxType = 1 | 2;    // 1 Acquisition, 2 Repayment
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
export interface InvestmentGroup {
  id: string;
  name: string;
  description: string | null;
  tag: AssetTagBadge | null;
  status: InvestmentStatus;
  totalInvested: number;
  totalCurrentValue: number;
  profitLoss: number;
}

export interface Investment {
  id: string;
  groupId: string;
  groupName: string;
  name: string;
  description: string | null;
  tag: AssetTagBadge | null;
  unit: string;
  status: InvestmentStatus;
  currentPrice: number | null;
  lastPriceAsOf: string | null;
  unitsHolding: number;
  averageBuyPrice: number;
  currentHoldingValue: number;
  invested: number;
  profitLoss: number;
}

export interface InvestmentPriceEntry {
  id: string;
  asOf: string;
  price: number;
  note: string | null;
}

export interface InvestmentTx {
  id: string;
  date: string;
  type: InvestmentTxType;
  units: number;
  price: number;
  total: number;
  note: string | null;
}

export interface InvestmentDetail {
  investment: Investment;
  prices: InvestmentPriceEntry[];
  transactions: InvestmentTx[];
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
export const CURRENCY = '$';

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
