export type TodoStatus = 1 | 2 | 3 | 4 | 5 | 6;
export type TodoSort = 'AddedDate' | 'Deadline' | 'Status' | 'DaysLeft';
export type SortOrder = 'Asc' | 'Desc';
export type ReportFormat = 'Json' | 'Csv' | 'Xlsx' | 'Pdf';

export const TODO_STATUSES: { value: TodoStatus; label: string; tone: string }[] = [
  { value: 1, label: 'Incomplete', tone: 'tone-amber' },
  { value: 2, label: 'Not started yet', tone: 'tone-grey' },
  { value: 3, label: 'In progress', tone: 'tone-cyan' },
  { value: 4, label: 'Almost completed', tone: 'tone-violet' },
  { value: 5, label: 'Completed', tone: 'tone-green' },
  { value: 6, label: 'Cancelled', tone: 'tone-red' }
];

export const TODO_STATUS_BY_ID: Record<TodoStatus, { label: string; tone: string }> = TODO_STATUSES.reduce(
  (acc, s) => {
    acc[s.value] = { label: s.label, tone: s.tone };
    return acc;
  },
  {} as Record<TodoStatus, { label: string; tone: string }>
);

export interface Todo {
  id: string;
  title: string;
  description: string | null;
  addedDate: string;
  deadline: string | null;
  daysLeft: number | null;
  status: TodoStatus;
  completedOn: string | null;
  completionNote: string | null;
}

export interface TodoStatusCount {
  status: TodoStatus;
  count: number;
}

export interface TodoSummary {
  total: number;
  byStatus: TodoStatusCount[];
}

export interface TodoReportRow {
  title: string;
  addedDate: string;
  deadline: string | null;
  daysLeft: number | null;
  status: TodoStatus;
  completedOn: string | null;
  completionNote: string | null;
}

export interface TodoReport {
  from: string;
  to: string;
  rows: TodoReportRow[];
}
