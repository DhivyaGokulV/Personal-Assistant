using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Application.Health;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Domain.Health;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.Health;

public class HealthService : IHealthService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public HealthService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId ?? throw new InvalidOperationException("No authenticated user.");

    public async Task<HealthPagedResult<MeasurementEntryDto>> ListMeasurementsAsync(DateOnly? from, DateOnly? to, string? search, int page, int pageSize, CancellationToken ct)
    {
        var q = _db.MeasurementEntries.Where(x => x.OwnerUserId == OwnerId);
        if (from.HasValue) q = q.Where(x => x.Date >= from.Value);
        if (to.HasValue) q = q.Where(x => x.Date <= to.Value);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Note != null && EF.Functions.Like(x.Note, $"%{search.Trim()}%"));
        var total = await q.CountAsync(ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var rows = await q.OrderByDescending(x => x.Date).Skip((page - 1) * pageSize).Take(pageSize).Select(x => MapMeasurement(x)).ToListAsync(ct);
        return new HealthPagedResult<MeasurementEntryDto>(rows, page, pageSize, total);
    }

    public async Task<MeasurementEntryDto> CreateMeasurementAsync(MeasurementEntryRequest req, CancellationToken ct)
    {
        ValidateMeasurement(req);
        var e = new MeasurementEntry { OwnerUserId = OwnerId };
        ApplyMeasurement(e, req);
        _db.MeasurementEntries.Add(e);
        await _db.SaveChangesAsync(ct);
        return MapMeasurement(e);
    }

    public async Task<MeasurementEntryDto> UpdateMeasurementAsync(Guid id, MeasurementEntryRequest req, CancellationToken ct)
    {
        ValidateMeasurement(req);
        var e = await _db.MeasurementEntries.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Measurement entry not found.");
        ApplyMeasurement(e, req);
        await _db.SaveChangesAsync(ct);
        return MapMeasurement(e);
    }

    public async Task DeleteMeasurementAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.MeasurementEntries.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Measurement entry not found.");
        _db.MeasurementEntries.Remove(e);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<MeasurementReport> GetMeasurementReportAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        ValidateDateRange(from, to);
        var rows = await _db.MeasurementEntries.Where(x => x.OwnerUserId == OwnerId && x.Date >= from && x.Date <= to)
            .OrderBy(x => x.Date).Select(x => MapMeasurement(x)).ToListAsync(ct);
        return new MeasurementReport(from, to, rows);
    }

    public async Task<IReadOnlyList<WorkoutDefinitionDto>> ListWorkoutDefinitionsAsync(CancellationToken ct)
        => await _db.WorkoutDefinitions.Where(x => x.OwnerUserId == OwnerId).OrderBy(x => x.Name).Select(x => MapWorkoutDefinition(x)).ToListAsync(ct);

    public async Task<WorkoutDefinitionDto> CreateWorkoutDefinitionAsync(WorkoutDefinitionRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Workout name is required.");
        var e = new WorkoutDefinition { OwnerUserId = OwnerId, Name = req.Name.Trim(), Type = req.Type, TargetedMuscle = req.TargetedMuscle?.Trim(), Tag = req.Tag?.Trim() };
        _db.WorkoutDefinitions.Add(e);
        await _db.SaveChangesAsync(ct);
        return MapWorkoutDefinition(e);
    }

    public async Task<WorkoutDefinitionDto> UpdateWorkoutDefinitionAsync(Guid id, WorkoutDefinitionRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Workout name is required.");
        var e = await _db.WorkoutDefinitions.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Workout definition not found.");
        e.Name = req.Name.Trim(); e.Type = req.Type; e.TargetedMuscle = req.TargetedMuscle?.Trim(); e.Tag = req.Tag?.Trim();
        await _db.SaveChangesAsync(ct);
        return MapWorkoutDefinition(e);
    }

    public async Task DeleteWorkoutDefinitionAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.WorkoutDefinitions.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Workout definition not found.");
        _db.WorkoutDefinitions.Remove(e);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<HealthPagedResult<WorkoutEntryDto>> ListWorkoutsAsync(DateOnly? from, DateOnly? to, string? workoutName, string? tag, int page, int pageSize, CancellationToken ct)
    {
        var q = _db.WorkoutEntries.Include(x => x.Sets).Where(x => x.OwnerUserId == OwnerId);
        if (from.HasValue) q = q.Where(x => x.Date >= from.Value);
        if (to.HasValue) q = q.Where(x => x.Date <= to.Value);
        if (!string.IsNullOrWhiteSpace(workoutName)) q = q.Where(x => EF.Functions.Like(x.WorkoutName, $"%{workoutName.Trim()}%"));
        if (!string.IsNullOrWhiteSpace(tag)) q = q.Where(x => x.Tag != null && EF.Functions.Like(x.Tag, $"%{tag.Trim()}%"));
        var total = await q.CountAsync(ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var rows = await q.OrderByDescending(x => x.Date).ThenBy(x => x.WorkoutName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new HealthPagedResult<WorkoutEntryDto>(rows.Select(MapWorkout).ToList(), page, pageSize, total);
    }

    public async Task<WorkoutEntryDto> CreateWorkoutAsync(WorkoutEntryRequest req, CancellationToken ct)
    {
        ValidateWorkout(req);
        var e = new WorkoutEntry { OwnerUserId = OwnerId };
        ApplyWorkout(e, req);
        _db.WorkoutEntries.Add(e);
        await _db.SaveChangesAsync(ct);
        return MapWorkout(e);
    }

    public async Task<WorkoutEntryDto> UpdateWorkoutAsync(Guid id, WorkoutEntryRequest req, CancellationToken ct)
    {
        ValidateWorkout(req);
        var e = await _db.WorkoutEntries.Include(x => x.Sets).FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Workout entry not found.");
        _db.WorkoutSets.RemoveRange(e.Sets);
        e.Sets.Clear();
        ApplyWorkout(e, req);
        await _db.SaveChangesAsync(ct);
        return MapWorkout(e);
    }

    public async Task DeleteWorkoutAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.WorkoutEntries.Include(x => x.Sets).FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Workout entry not found.");
        _db.WorkoutSets.RemoveRange(e.Sets);
        _db.WorkoutEntries.Remove(e);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<WorkoutReport> GetWorkoutReportAsync(DateOnly from, DateOnly to, string? workoutName, CancellationToken ct)
    {
        ValidateDateRange(from, to);
        var q = _db.WorkoutEntries.Include(x => x.Sets).Where(x => x.OwnerUserId == OwnerId && x.Date >= from && x.Date <= to);
        if (!string.IsNullOrWhiteSpace(workoutName)) q = q.Where(x => EF.Functions.Like(x.WorkoutName, $"%{workoutName.Trim()}%"));
        var rows = await q.ToListAsync(ct);
        var reportRows = rows.GroupBy(x => new { x.WorkoutName, x.Type })
            .Select(g => new WorkoutReportRow(
                g.Key.WorkoutName, g.Key.Type, g.Count(), g.Max(x => (DateOnly?)x.Date),
                g.SelectMany(x => x.Sets).Sum(s => s.Reps * (s.Weight ?? s.AddedWeight ?? 0m)),
                g.SelectMany(x => x.Sets).Max(s => s.Weight ?? s.AddedWeight),
                g.SelectMany(x => x.Sets).Sum(s => s.Reps),
                g.Sum(x => x.DurationMinutes ?? 0),
                g.Sum(x => x.Distance ?? 0m)))
            .OrderBy(x => x.WorkoutName).ToList();
        return new WorkoutReport(from, to, reportRows);
    }

    public async Task<IReadOnlyList<FoodDefinitionDto>> ListFoodsAsync(CancellationToken ct)
        => await _db.FoodDefinitions.Where(x => x.OwnerUserId == OwnerId).OrderBy(x => x.Name).Select(x => MapFood(x)).ToListAsync(ct);

    public async Task<FoodDefinitionDto> CreateFoodAsync(FoodDefinitionRequest req, CancellationToken ct)
    {
        ValidateFood(req);
        var e = new FoodDefinition { OwnerUserId = OwnerId, Name = req.Name.Trim(), Unit = req.Unit.Trim(), Carbohydrates = req.Carbohydrates, Protein = req.Protein, Fat = req.Fat, Calories = req.Calories };
        _db.FoodDefinitions.Add(e);
        await _db.SaveChangesAsync(ct);
        return MapFood(e);
    }

    public async Task<FoodDefinitionDto> UpdateFoodAsync(Guid id, FoodDefinitionRequest req, CancellationToken ct)
    {
        ValidateFood(req);
        var e = await _db.FoodDefinitions.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Food item not found.");
        e.Name = req.Name.Trim(); e.Unit = req.Unit.Trim(); e.Carbohydrates = req.Carbohydrates; e.Protein = req.Protein; e.Fat = req.Fat; e.Calories = req.Calories;
        await _db.SaveChangesAsync(ct);
        return MapFood(e);
    }

    public async Task DeleteFoodAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.FoodDefinitions.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Food item not found.");
        _db.FoodDefinitions.Remove(e);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<HealthPagedResult<NutritionEntryDto>> ListNutritionAsync(DateOnly? from, DateOnly? to, string? food, int page, int pageSize, CancellationToken ct)
    {
        var q = _db.NutritionEntries.Where(x => x.OwnerUserId == OwnerId);
        if (from.HasValue) q = q.Where(x => x.Date >= from.Value);
        if (to.HasValue) q = q.Where(x => x.Date <= to.Value);
        if (!string.IsNullOrWhiteSpace(food)) q = q.Where(x => EF.Functions.Like(x.Food, $"%{food.Trim()}%"));
        var total = await q.CountAsync(ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var rows = await q.OrderByDescending(x => x.Date).ThenBy(x => x.TimeOfDay).Skip((page - 1) * pageSize).Take(pageSize).Select(x => MapNutrition(x)).ToListAsync(ct);
        return new HealthPagedResult<NutritionEntryDto>(rows, page, pageSize, total);
    }

    public async Task<NutritionEntryDto> CreateNutritionAsync(NutritionEntryRequest req, CancellationToken ct)
    {
        ValidateNutrition(req);
        var e = new NutritionEntry { OwnerUserId = OwnerId };
        ApplyNutrition(e, req);
        _db.NutritionEntries.Add(e);
        await _db.SaveChangesAsync(ct);
        return MapNutrition(e);
    }

    public async Task<NutritionEntryDto> UpdateNutritionAsync(Guid id, NutritionEntryRequest req, CancellationToken ct)
    {
        ValidateNutrition(req);
        var e = await _db.NutritionEntries.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Nutrition entry not found.");
        ApplyNutrition(e, req);
        await _db.SaveChangesAsync(ct);
        return MapNutrition(e);
    }

    public async Task DeleteNutritionAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.NutritionEntries.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Nutrition entry not found.");
        _db.NutritionEntries.Remove(e);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<NutritionGoalDto> GetNutritionGoalAsync(CancellationToken ct)
    {
        var e = await _db.NutritionGoals.OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(x => x.OwnerUserId == OwnerId, ct);
        return e is null ? new NutritionGoalDto(null, null, null, null, null) : new NutritionGoalDto(e.Id, e.Carbohydrates, e.Protein, e.Fat, e.Calories);
    }

    public async Task<NutritionGoalDto> SaveNutritionGoalAsync(NutritionGoalRequest req, CancellationToken ct)
    {
        var e = await _db.NutritionGoals.FirstOrDefaultAsync(x => x.OwnerUserId == OwnerId, ct);
        if (e is null)
        {
            e = new NutritionGoal { OwnerUserId = OwnerId };
            _db.NutritionGoals.Add(e);
        }
        e.Carbohydrates = req.Carbohydrates; e.Protein = req.Protein; e.Fat = req.Fat; e.Calories = req.Calories;
        await _db.SaveChangesAsync(ct);
        return new NutritionGoalDto(e.Id, e.Carbohydrates, e.Protein, e.Fat, e.Calories);
    }

    public async Task<NutritionDayView> GetNutritionDayAsync(DateOnly date, CancellationToken ct)
    {
        var entries = await _db.NutritionEntries.Where(x => x.OwnerUserId == OwnerId && x.Date == date).OrderBy(x => x.TimeOfDay).Select(x => MapNutrition(x)).ToListAsync(ct);
        var goal = await GetNutritionGoalAsync(ct);
        return new NutritionDayView(date, goal, entries.Sum(x => x.Carbohydrates ?? 0), entries.Sum(x => x.Protein ?? 0), entries.Sum(x => x.Fat ?? 0), entries.Sum(x => x.Calories ?? 0), entries);
    }

    public async Task<NutritionReport> GetNutritionReportAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        ValidateDateRange(from, to);
        var rows = await _db.NutritionEntries.Where(x => x.OwnerUserId == OwnerId && x.Date >= from && x.Date <= to).OrderBy(x => x.Date).ThenBy(x => x.TimeOfDay).Select(x => MapNutrition(x)).ToListAsync(ct);
        return new NutritionReport(from, to, rows.Sum(x => x.Carbohydrates ?? 0), rows.Sum(x => x.Protein ?? 0), rows.Sum(x => x.Fat ?? 0), rows.Sum(x => x.Calories ?? 0), rows);
    }

    private static void ValidateDateRange(DateOnly from, DateOnly to)
    {
        if (to < from) throw new ArgumentException("To date must be on/after From date.");
    }

    private static void ValidateMeasurement(MeasurementEntryRequest r)
    {
        if (new decimal?[] { r.HeightCm, r.WeightKg, r.Bmi, r.BodyFatPercentage, r.MusclePercentage, r.BicepsCm, r.BellyCm, r.ForearmCm, r.ChestCm, r.ThighsCm, r.CalvesCm, r.NeckCm }.All(x => !x.HasValue))
            throw new ArgumentException("At least one measurement value is required.");
    }

    private static void ValidateWorkout(WorkoutEntryRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.WorkoutName)) throw new ArgumentException("Workout name is required.");
        if (r.Type is WorkoutType.WeightBased or WorkoutType.Calisthenics)
        {
            if (string.IsNullOrWhiteSpace(r.TargetedMuscle)) throw new ArgumentException("Targeted muscle is required.");
            if (r.Sets is null || r.Sets.Count == 0) throw new ArgumentException("At least one set is required.");
            if (r.Sets.Any(s => s.Reps <= 0)) throw new ArgumentException("Set reps must be positive.");
            if (r.Type == WorkoutType.WeightBased && r.Sets.Any(s => !s.Weight.HasValue || s.Weight <= 0)) throw new ArgumentException("Weight-based sets require positive weight.");
        }
        if (r.Type == WorkoutType.Cardio)
        {
            if (!r.DurationMinutes.HasValue || r.DurationMinutes <= 0) throw new ArgumentException("Duration is required.");
            if (string.IsNullOrWhiteSpace(r.Intensity)) throw new ArgumentException("Intensity is required.");
            if (!r.Distance.HasValue || r.Distance <= 0) throw new ArgumentException("Distance is required.");
        }
    }

    private static void ValidateFood(FoodDefinitionRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) throw new ArgumentException("Food name is required.");
        if (string.IsNullOrWhiteSpace(r.Unit)) throw new ArgumentException("Unit is required.");
    }

    private static void ValidateNutrition(NutritionEntryRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Food)) throw new ArgumentException("Food is required.");
        if (string.IsNullOrWhiteSpace(r.Unit)) throw new ArgumentException("Unit is required.");
        if (r.Quantity <= 0) throw new ArgumentException("Quantity must be positive.");
    }

    private static void ApplyMeasurement(MeasurementEntry e, MeasurementEntryRequest r)
    {
        e.Date = r.Date; e.HeightCm = r.HeightCm; e.WeightKg = r.WeightKg; e.Bmi = r.Bmi; e.BodyFatPercentage = r.BodyFatPercentage; e.MusclePercentage = r.MusclePercentage;
        e.BicepsCm = r.BicepsCm; e.BellyCm = r.BellyCm; e.ForearmCm = r.ForearmCm; e.ChestCm = r.ChestCm; e.ThighsCm = r.ThighsCm; e.CalvesCm = r.CalvesCm; e.NeckCm = r.NeckCm; e.Note = r.Note?.Trim();
    }

    private void ApplyWorkout(WorkoutEntry e, WorkoutEntryRequest r)
    {
        e.Date = r.Date; e.Type = r.Type; e.WorkoutName = r.WorkoutName.Trim(); e.TargetedMuscle = r.TargetedMuscle?.Trim(); e.Tag = r.Tag?.Trim();
        e.DurationMinutes = r.Type == WorkoutType.Cardio ? r.DurationMinutes : null; e.Intensity = r.Type == WorkoutType.Cardio ? r.Intensity?.Trim() : null; e.Distance = r.Type == WorkoutType.Cardio ? r.Distance : null; e.CaloriesBurned = r.Type == WorkoutType.Cardio ? r.CaloriesBurned : null; e.Note = r.Note?.Trim();
        if (r.Type is WorkoutType.WeightBased or WorkoutType.Calisthenics && r.Sets is not null)
        {
            var i = 1;
            foreach (var s in r.Sets)
            {
                e.Sets.Add(new WorkoutSet { OwnerUserId = OwnerId, SetNumber = i++, Reps = s.Reps, Weight = r.Type == WorkoutType.WeightBased ? s.Weight : null, AddedWeight = r.Type == WorkoutType.Calisthenics ? s.AddedWeight : null });
            }
        }
    }

    private static void ApplyNutrition(NutritionEntry e, NutritionEntryRequest r)
    {
        e.Date = r.Date; e.TimeOfDay = r.TimeOfDay; e.Food = r.Food.Trim(); e.Quantity = r.Quantity; e.Unit = r.Unit.Trim();
        e.Carbohydrates = r.Carbohydrates; e.Protein = r.Protein; e.Fat = r.Fat; e.Calories = r.Calories; e.Note = r.Note?.Trim();
    }

    private static MeasurementEntryDto MapMeasurement(MeasurementEntry x) => new(x.Id, x.Date, x.HeightCm, x.WeightKg, x.Bmi, x.BodyFatPercentage, x.MusclePercentage, x.BicepsCm, x.BellyCm, x.ForearmCm, x.ChestCm, x.ThighsCm, x.CalvesCm, x.NeckCm, x.Note);
    private static WorkoutDefinitionDto MapWorkoutDefinition(WorkoutDefinition x) => new(x.Id, x.Name, x.Type, x.TargetedMuscle, x.Tag);
    private static WorkoutEntryDto MapWorkout(WorkoutEntry x) => new(x.Id, x.Date, x.Type, x.WorkoutName, x.TargetedMuscle, x.Tag, x.DurationMinutes, x.Intensity, x.Distance, x.CaloriesBurned, x.Note, x.Sets.OrderBy(s => s.SetNumber).Select(s => new WorkoutSetDto(s.Id, s.SetNumber, s.Reps, s.Weight, s.AddedWeight)).ToList());
    private static FoodDefinitionDto MapFood(FoodDefinition x) => new(x.Id, x.Name, x.Unit, x.Carbohydrates, x.Protein, x.Fat, x.Calories);
    private static NutritionEntryDto MapNutrition(NutritionEntry x) => new(x.Id, x.Date, x.TimeOfDay, x.Food, x.Quantity, x.Unit, x.Carbohydrates, x.Protein, x.Fat, x.Calories, x.Note);
}
