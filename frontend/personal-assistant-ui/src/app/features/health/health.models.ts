export type ReportFormat = 'Json' | 'Csv' | 'Xlsx' | 'Pdf';
export type WorkoutType = 1 | 2 | 3;
export type NutritionTimeOfDay = 1 | 2 | 3 | 4;
export interface PagedResult<T> { items: T[]; page: number; pageSize: number; totalCount: number; }
export interface MeasurementEntry { id: string; date: string; heightCm?: number | null; weightKg?: number | null; bmi?: number | null; bodyFatPercentage?: number | null; musclePercentage?: number | null; bicepsCm?: number | null; bellyCm?: number | null; forearmCm?: number | null; chestCm?: number | null; thighsCm?: number | null; calvesCm?: number | null; neckCm?: number | null; note?: string | null; }
export interface WorkoutSet { id?: string; setNumber?: number; reps: number; weight?: number | null; addedWeight?: number | null; }
export interface WorkoutEntry { id: string; date: string; type: WorkoutType; workoutName: string; targetedMuscle: string | null; tag: string | null; durationMinutes: number | null; intensity: string | null; distance: number | null; caloriesBurned: number | null; note: string | null; sets: WorkoutSet[]; }
export interface WorkoutDefinition { id: string; name: string; type: WorkoutType; targetedMuscle: string | null; tag: string | null; }
export interface FoodDefinition { id: string; name: string; unit: string; carbohydrates: number | null; protein: number | null; fat: number | null; calories: number | null; }
export interface NutritionEntry { id: string; date: string; timeOfDay: NutritionTimeOfDay; food: string; quantity: number; unit: string; carbohydrates: number | null; protein: number | null; fat: number | null; calories: number | null; note: string | null; }
export interface NutritionGoal { id: string | null; carbohydrates: number | null; protein: number | null; fat: number | null; calories: number | null; }
export interface NutritionDay { date: string; goal: NutritionGoal; carbohydrates: number; protein: number; fat: number; calories: number; entries: NutritionEntry[]; }
export interface WaterIntakeEntry { id: string; date: string; time: string; quantityMl: number; note: string | null; }
