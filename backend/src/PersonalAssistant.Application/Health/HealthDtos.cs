using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.Health;

public record HealthPagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public record MeasurementEntryDto(
    Guid Id, DateOnly Date, decimal? HeightCm, decimal? WeightKg, decimal? Bmi,
    decimal? BodyFatPercentage, decimal? MusclePercentage, decimal? BicepsCm, decimal? BellyCm,
    decimal? ForearmCm, decimal? ChestCm, decimal? ThighsCm, decimal? CalvesCm, decimal? NeckCm,
    string? Note);

public record MeasurementEntryRequest(
    DateOnly Date, decimal? HeightCm, decimal? WeightKg, decimal? Bmi,
    decimal? BodyFatPercentage, decimal? MusclePercentage, decimal? BicepsCm, decimal? BellyCm,
    decimal? ForearmCm, decimal? ChestCm, decimal? ThighsCm, decimal? CalvesCm, decimal? NeckCm,
    string? Note);

public record WorkoutDefinitionDto(Guid Id, string Name, WorkoutType Type, string? TargetedMuscle, string? Tag);
public record WorkoutDefinitionRequest(string Name, WorkoutType Type, string? TargetedMuscle, string? Tag);

public record WorkoutSetDto(Guid Id, int SetNumber, int Reps, decimal? Weight, decimal? AddedWeight);
public record WorkoutSetRequest(int Reps, decimal? Weight, decimal? AddedWeight);

public record WorkoutEntryDto(
    Guid Id, DateOnly Date, WorkoutType Type, string WorkoutName, string? TargetedMuscle, string? Tag,
    int? DurationMinutes, string? Intensity, decimal? Distance, decimal? CaloriesBurned, string? Note,
    IReadOnlyList<WorkoutSetDto> Sets);

public record WorkoutEntryRequest(
    DateOnly Date, WorkoutType Type, string WorkoutName, string? TargetedMuscle, string? Tag,
    int? DurationMinutes, string? Intensity, decimal? Distance, decimal? CaloriesBurned, string? Note,
    IReadOnlyList<WorkoutSetRequest>? Sets);

public record FoodDefinitionDto(Guid Id, string Name, string Unit, decimal? Carbohydrates, decimal? Protein, decimal? Fat, decimal? Calories);
public record FoodDefinitionRequest(string Name, string Unit, decimal? Carbohydrates, decimal? Protein, decimal? Fat, decimal? Calories);

public record NutritionEntryDto(
    Guid Id, DateOnly Date, NutritionTimeOfDay TimeOfDay, string Food, decimal Quantity, string Unit,
    decimal? Carbohydrates, decimal? Protein, decimal? Fat, decimal? Calories, string? Note);

public record NutritionEntryRequest(
    DateOnly Date, NutritionTimeOfDay TimeOfDay, string Food, decimal Quantity, string Unit,
    decimal? Carbohydrates, decimal? Protein, decimal? Fat, decimal? Calories, string? Note);

public record NutritionGoalDto(Guid? Id, decimal? Carbohydrates, decimal? Protein, decimal? Fat, decimal? Calories);
public record NutritionGoalRequest(decimal? Carbohydrates, decimal? Protein, decimal? Fat, decimal? Calories);

public record NutritionDayView(DateOnly Date, NutritionGoalDto Goal, decimal Carbohydrates, decimal Protein, decimal Fat, decimal Calories, IReadOnlyList<NutritionEntryDto> Entries);

public record WorkoutReportRow(string WorkoutName, WorkoutType Type, int TimesDone, DateOnly? LastDoneOn, decimal TotalVolume, decimal? MaxWeight, int TotalReps, int? TotalMinutes, decimal? TotalDistance);
public record MeasurementReport(DateOnly From, DateOnly To, IReadOnlyList<MeasurementEntryDto> Rows);
public record WorkoutReport(DateOnly From, DateOnly To, IReadOnlyList<WorkoutReportRow> Rows);
public record NutritionReport(DateOnly From, DateOnly To, decimal Carbohydrates, decimal Protein, decimal Fat, decimal Calories, IReadOnlyList<NutritionEntryDto> Rows);

public interface IHealthService
{
    Task<HealthPagedResult<MeasurementEntryDto>> ListMeasurementsAsync(DateOnly? from, DateOnly? to, string? search, int page, int pageSize, CancellationToken ct);
    Task<MeasurementEntryDto> CreateMeasurementAsync(MeasurementEntryRequest req, CancellationToken ct);
    Task<MeasurementEntryDto> UpdateMeasurementAsync(Guid id, MeasurementEntryRequest req, CancellationToken ct);
    Task DeleteMeasurementAsync(Guid id, CancellationToken ct);
    Task<MeasurementReport> GetMeasurementReportAsync(DateOnly from, DateOnly to, CancellationToken ct);

    Task<IReadOnlyList<WorkoutDefinitionDto>> ListWorkoutDefinitionsAsync(CancellationToken ct);
    Task<WorkoutDefinitionDto> CreateWorkoutDefinitionAsync(WorkoutDefinitionRequest req, CancellationToken ct);
    Task<WorkoutDefinitionDto> UpdateWorkoutDefinitionAsync(Guid id, WorkoutDefinitionRequest req, CancellationToken ct);
    Task DeleteWorkoutDefinitionAsync(Guid id, CancellationToken ct);

    Task<HealthPagedResult<WorkoutEntryDto>> ListWorkoutsAsync(DateOnly? from, DateOnly? to, string? workoutName, string? tag, int page, int pageSize, CancellationToken ct);
    Task<WorkoutEntryDto> CreateWorkoutAsync(WorkoutEntryRequest req, CancellationToken ct);
    Task<WorkoutEntryDto> UpdateWorkoutAsync(Guid id, WorkoutEntryRequest req, CancellationToken ct);
    Task DeleteWorkoutAsync(Guid id, CancellationToken ct);
    Task<WorkoutReport> GetWorkoutReportAsync(DateOnly from, DateOnly to, string? workoutName, CancellationToken ct);

    Task<IReadOnlyList<FoodDefinitionDto>> ListFoodsAsync(CancellationToken ct);
    Task<FoodDefinitionDto> CreateFoodAsync(FoodDefinitionRequest req, CancellationToken ct);
    Task<FoodDefinitionDto> UpdateFoodAsync(Guid id, FoodDefinitionRequest req, CancellationToken ct);
    Task DeleteFoodAsync(Guid id, CancellationToken ct);

    Task<HealthPagedResult<NutritionEntryDto>> ListNutritionAsync(DateOnly? from, DateOnly? to, string? food, int page, int pageSize, CancellationToken ct);
    Task<NutritionEntryDto> CreateNutritionAsync(NutritionEntryRequest req, CancellationToken ct);
    Task<NutritionEntryDto> UpdateNutritionAsync(Guid id, NutritionEntryRequest req, CancellationToken ct);
    Task DeleteNutritionAsync(Guid id, CancellationToken ct);
    Task<NutritionGoalDto> GetNutritionGoalAsync(CancellationToken ct);
    Task<NutritionGoalDto> SaveNutritionGoalAsync(NutritionGoalRequest req, CancellationToken ct);
    Task<NutritionDayView> GetNutritionDayAsync(DateOnly date, CancellationToken ct);
    Task<NutritionReport> GetNutritionReportAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
