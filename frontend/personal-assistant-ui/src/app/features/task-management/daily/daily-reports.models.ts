export interface DailyDayWiseRow {
  date: string;
  groupName: string;
  taskTitle: string;
  isCompleted: boolean;
  note: string | null;
}

export interface DailyTaskWiseRow {
  groupName: string;
  taskTitle: string;
  timesDone: number;
  lastDoneOn: string | null;
}

export interface DailyDayWiseReport {
  from: string;
  to: string;
  rows: DailyDayWiseRow[];
}

export interface DailyTaskWiseReport {
  from: string;
  to: string;
  rows: DailyTaskWiseRow[];
}

export type ReportFormat = 'Json' | 'Csv' | 'Xlsx' | 'Pdf';
export type ReportKind = 'day-wise' | 'task-wise';
