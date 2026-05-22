using PersonalAssistant.Domain.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Domain.Health;

public class MeasurementEntry : EntityBase
{
    public DateOnly Date { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? Bmi { get; set; }
    public decimal? BodyFatPercentage { get; set; }
    public decimal? MusclePercentage { get; set; }
    public decimal? BicepsCm { get; set; }
    public decimal? BellyCm { get; set; }
    public decimal? ForearmCm { get; set; }
    public decimal? ChestCm { get; set; }
    public decimal? ThighsCm { get; set; }
    public decimal? CalvesCm { get; set; }
    public decimal? NeckCm { get; set; }
    public string? Note { get; set; }
}

public class WorkoutDefinition : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public WorkoutType Type { get; set; }
    public string? TargetedMuscle { get; set; }
    public string? Tag { get; set; }
}

public class WorkoutEntry : EntityBase
{
    public DateOnly Date { get; set; }
    public WorkoutType Type { get; set; }
    public string WorkoutName { get; set; } = string.Empty;
    public string? TargetedMuscle { get; set; }
    public string? Tag { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Intensity { get; set; }
    public decimal? Distance { get; set; }
    public decimal? CaloriesBurned { get; set; }
    public string? Note { get; set; }
    public ICollection<WorkoutSet> Sets { get; set; } = new List<WorkoutSet>();
}

public class WorkoutSet : EntityBase
{
    public Guid WorkoutEntryId { get; set; }
    public WorkoutEntry? WorkoutEntry { get; set; }
    public int SetNumber { get; set; }
    public int Reps { get; set; }
    public decimal? Weight { get; set; }
    public decimal? AddedWeight { get; set; }
}

public class FoodDefinition : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "unit";
    public decimal? Carbohydrates { get; set; }
    public decimal? Protein { get; set; }
    public decimal? Fat { get; set; }
    public decimal? Calories { get; set; }
}

public class NutritionEntry : EntityBase
{
    public DateOnly Date { get; set; }
    public NutritionTimeOfDay TimeOfDay { get; set; }
    public string Food { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? Carbohydrates { get; set; }
    public decimal? Protein { get; set; }
    public decimal? Fat { get; set; }
    public decimal? Calories { get; set; }
    public string? Note { get; set; }
}

public class NutritionGoal : EntityBase
{
    public decimal? Carbohydrates { get; set; }
    public decimal? Protein { get; set; }
    public decimal? Fat { get; set; }
    public decimal? Calories { get; set; }
}
