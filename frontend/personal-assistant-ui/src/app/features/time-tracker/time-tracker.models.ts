export type ReportFormat = 'Json' | 'Csv' | 'Xlsx' | 'Pdf';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface TimeEntry {
  id: string;
  startTime: string;
  endTime: string;
  activity: string;
  note: string | null;
  tag: string | null;
  minutes: number;
}

export interface TimeReport {
  from: string;
  to: string;
  rows: TimeEntry[];
  activitySummary: { key: string; numberOfDays: number; numberOfMinutes: number }[];
  tagSummary: { key: string; numberOfDays: number; numberOfMinutes: number }[];
}
