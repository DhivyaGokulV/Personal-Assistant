export type ReportFormat = 'Json' | 'Csv' | 'Xlsx' | 'Pdf';
export interface GoalStep { id: string; name: string; description: string | null; startDate: string; deadline: string; achievedDate: string | null; note: string | null; status: string; }
export interface Goal { id: string; name: string; description: string | null; tag: string | null; startDate: string; deadline: string; achievedDate: string | null; note: string | null; status: string; steps: GoalStep[]; }
export interface GoalPlan { id: string; name: string; description: string | null; tag: string | null; goalCount: number; achievedGoalCount: number; goals: Goal[]; }
