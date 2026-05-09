export type TaskActiveStatus = 1 | 2; // 1=Active, 2=Inactive

export interface DailyGroup {
  id: string;
  name: string;
  description: string | null;
  taskCount: number;
}

export interface DailyTask {
  id: string;
  groupId: string;
  title: string;
  description: string | null;
  status: TaskActiveStatus;
}

export interface DailyCompletion {
  isCompleted: boolean;
  note: string | null;
}

export interface DailyCounts {
  total: number;
  completed: number;
  notCompleted: number;
}

export interface DailyTaskWithCompletion {
  id: string;
  title: string;
  description: string | null;
  status: TaskActiveStatus;
  completion: DailyCompletion | null;
}

export interface DailyGroupView {
  id: string;
  name: string;
  description: string | null;
  counts: DailyCounts;
  tasks: DailyTaskWithCompletion[];
}

export interface DailyByDateView {
  date: string; // ISO yyyy-MM-dd
  totals: DailyCounts;
  groups: DailyGroupView[];
}
