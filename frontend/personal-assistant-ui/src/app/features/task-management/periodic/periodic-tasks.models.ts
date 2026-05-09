export type TaskActiveStatus = 1 | 2;
export type FrequencyUnit = 1 | 2 | 3 | 4; // 1=Days, 2=Weeks, 3=Months, 4=Years

export const FREQUENCY_UNITS: { value: FrequencyUnit; label: string }[] = [
  { value: 1, label: 'Day(s)' },
  { value: 2, label: 'Week(s)' },
  { value: 3, label: 'Month(s)' },
  { value: 4, label: 'Year(s)' }
];

export interface PeriodicGroup {
  id: string;
  name: string;
  description: string | null;
  taskCount: number;
}

export interface PeriodicTask {
  id: string;
  groupId: string;
  groupName: string;
  title: string;
  description: string | null;
  status: TaskActiveStatus;
  frequencyValue: number;
  frequencyUnit: FrequencyUnit;
  lastDoneOn: string | null;
  nextDueOn: string | null;
  historyCount: number;
}

export interface PeriodicHistory {
  id: string;
  completedOn: string;
  note: string | null;
}

export interface PeriodicTaskWithHistory {
  task: PeriodicTask;
  history: PeriodicHistory[];
}

export interface PeriodicReportRow {
  groupName: string;
  taskTitle: string;
  timesDoneInRange: number;
  lastDoneOn: string | null;
  nextDueOn: string | null;
}

export interface PeriodicReport {
  from: string;
  to: string;
  rows: PeriodicReportRow[];
}

export type ReportFormat = 'Json' | 'Csv' | 'Xlsx' | 'Pdf';
