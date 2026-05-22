using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Application.Health;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IHealthService _service;
    private readonly IReportExportService _exporter;

    public HealthController(IHealthService service, IReportExportService exporter)
    {
        _service = service;
        _exporter = exporter;
    }

    [HttpGet("measurements")]
    public async Task<ActionResult<HealthPagedResult<MeasurementEntryDto>>> Measurements([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
        => Ok(await _service.ListMeasurementsAsync(from, to, search, page, pageSize, ct));

    [HttpPost("measurements")]
    public async Task<ActionResult<MeasurementEntryDto>> CreateMeasurement([FromBody] MeasurementEntryRequest req, CancellationToken ct)
        => await Execute(() => _service.CreateMeasurementAsync(req, ct));

    [HttpPut("measurements/{id:guid}")]
    public async Task<ActionResult<MeasurementEntryDto>> UpdateMeasurement(Guid id, [FromBody] MeasurementEntryRequest req, CancellationToken ct)
        => await Execute(() => _service.UpdateMeasurementAsync(id, req, ct));

    [HttpDelete("measurements/{id:guid}")]
    public async Task<IActionResult> DeleteMeasurement(Guid id, CancellationToken ct)
        => await DeleteExecute(() => _service.DeleteMeasurementAsync(id, ct));

    [HttpGet("measurements/reports")]
    public async Task<IActionResult> MeasurementReport([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] ReportFormat format = ReportFormat.Json, CancellationToken ct = default)
    {
        try
        {
            var report = await _service.GetMeasurementReportAsync(from, to, ct);
            if (format == ReportFormat.Json) return Ok(report);
            var table = new ReportTable($"Measurements-{from:yyyyMMdd}-to-{to:yyyyMMdd}",
                new[] { "Date", "Height", "Weight", "BMI", "Body Fat", "Muscle", "Biceps", "Belly", "Forearm", "Chest", "Thighs", "Calves", "Neck", "Note" },
                report.Rows.Select(r => (IReadOnlyList<string?>)new[] { r.Date.ToString("yyyy-MM-dd"), S(r.HeightCm), S(r.WeightKg), S(r.Bmi), S(r.BodyFatPercentage), S(r.MusclePercentage), S(r.BicepsCm), S(r.BellyCm), S(r.ForearmCm), S(r.ChestCm), S(r.ThighsCm), S(r.CalvesCm), S(r.NeckCm), r.Note }).ToList());
            var exported = _exporter.Export(table, format);
            return File(exported.Data, exported.ContentType, exported.FileName);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("workout-definitions")]
    public async Task<ActionResult<IReadOnlyList<WorkoutDefinitionDto>>> WorkoutDefinitions(CancellationToken ct)
        => Ok(await _service.ListWorkoutDefinitionsAsync(ct));

    [HttpPost("workout-definitions")]
    public async Task<ActionResult<WorkoutDefinitionDto>> CreateWorkoutDefinition([FromBody] WorkoutDefinitionRequest req, CancellationToken ct)
        => await Execute(() => _service.CreateWorkoutDefinitionAsync(req, ct));

    [HttpPut("workout-definitions/{id:guid}")]
    public async Task<ActionResult<WorkoutDefinitionDto>> UpdateWorkoutDefinition(Guid id, [FromBody] WorkoutDefinitionRequest req, CancellationToken ct)
        => await Execute(() => _service.UpdateWorkoutDefinitionAsync(id, req, ct));

    [HttpDelete("workout-definitions/{id:guid}")]
    public async Task<IActionResult> DeleteWorkoutDefinition(Guid id, CancellationToken ct)
        => await DeleteExecute(() => _service.DeleteWorkoutDefinitionAsync(id, ct));

    [HttpGet("workouts")]
    public async Task<ActionResult<HealthPagedResult<WorkoutEntryDto>>> Workouts([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? workoutName, [FromQuery] string? tag, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
        => Ok(await _service.ListWorkoutsAsync(from, to, workoutName, tag, page, pageSize, ct));

    [HttpPost("workouts")]
    public async Task<ActionResult<WorkoutEntryDto>> CreateWorkout([FromBody] WorkoutEntryRequest req, CancellationToken ct)
        => await Execute(() => _service.CreateWorkoutAsync(req, ct));

    [HttpPut("workouts/{id:guid}")]
    public async Task<ActionResult<WorkoutEntryDto>> UpdateWorkout(Guid id, [FromBody] WorkoutEntryRequest req, CancellationToken ct)
        => await Execute(() => _service.UpdateWorkoutAsync(id, req, ct));

    [HttpDelete("workouts/{id:guid}")]
    public async Task<IActionResult> DeleteWorkout(Guid id, CancellationToken ct)
        => await DeleteExecute(() => _service.DeleteWorkoutAsync(id, ct));

    [HttpGet("workouts/reports")]
    public async Task<IActionResult> WorkoutReport([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] string? workoutName, [FromQuery] ReportFormat format = ReportFormat.Json, CancellationToken ct = default)
    {
        try
        {
            var report = await _service.GetWorkoutReportAsync(from, to, workoutName, ct);
            if (format == ReportFormat.Json) return Ok(report);
            var table = new ReportTable($"Workouts-{from:yyyyMMdd}-to-{to:yyyyMMdd}",
                new[] { "Workout", "Type", "Times Done", "Last Done", "Volume", "Max Weight", "Total Reps", "Minutes", "Distance" },
                report.Rows.Select(r => (IReadOnlyList<string?>)new[] { r.WorkoutName, r.Type.ToString(), r.TimesDone.ToString(), r.LastDoneOn?.ToString("yyyy-MM-dd"), S(r.TotalVolume), S(r.MaxWeight), r.TotalReps.ToString(), r.TotalMinutes?.ToString(), S(r.TotalDistance) }).ToList());
            var exported = _exporter.Export(table, format);
            return File(exported.Data, exported.ContentType, exported.FileName);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("foods")]
    public async Task<ActionResult<IReadOnlyList<FoodDefinitionDto>>> Foods(CancellationToken ct)
        => Ok(await _service.ListFoodsAsync(ct));

    [HttpPost("foods")]
    public async Task<ActionResult<FoodDefinitionDto>> CreateFood([FromBody] FoodDefinitionRequest req, CancellationToken ct)
        => await Execute(() => _service.CreateFoodAsync(req, ct));

    [HttpPut("foods/{id:guid}")]
    public async Task<ActionResult<FoodDefinitionDto>> UpdateFood(Guid id, [FromBody] FoodDefinitionRequest req, CancellationToken ct)
        => await Execute(() => _service.UpdateFoodAsync(id, req, ct));

    [HttpDelete("foods/{id:guid}")]
    public async Task<IActionResult> DeleteFood(Guid id, CancellationToken ct)
        => await DeleteExecute(() => _service.DeleteFoodAsync(id, ct));

    [HttpGet("nutrition")]
    public async Task<ActionResult<HealthPagedResult<NutritionEntryDto>>> Nutrition([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? food, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
        => Ok(await _service.ListNutritionAsync(from, to, food, page, pageSize, ct));

    [HttpPost("nutrition")]
    public async Task<ActionResult<NutritionEntryDto>> CreateNutrition([FromBody] NutritionEntryRequest req, CancellationToken ct)
        => await Execute(() => _service.CreateNutritionAsync(req, ct));

    [HttpPut("nutrition/{id:guid}")]
    public async Task<ActionResult<NutritionEntryDto>> UpdateNutrition(Guid id, [FromBody] NutritionEntryRequest req, CancellationToken ct)
        => await Execute(() => _service.UpdateNutritionAsync(id, req, ct));

    [HttpDelete("nutrition/{id:guid}")]
    public async Task<IActionResult> DeleteNutrition(Guid id, CancellationToken ct)
        => await DeleteExecute(() => _service.DeleteNutritionAsync(id, ct));

    [HttpGet("nutrition-goal")]
    public async Task<ActionResult<NutritionGoalDto>> GetGoal(CancellationToken ct) => Ok(await _service.GetNutritionGoalAsync(ct));

    [HttpPut("nutrition-goal")]
    public async Task<ActionResult<NutritionGoalDto>> SaveGoal([FromBody] NutritionGoalRequest req, CancellationToken ct)
        => Ok(await _service.SaveNutritionGoalAsync(req, ct));

    [HttpGet("nutrition/day")]
    public async Task<ActionResult<NutritionDayView>> NutritionDay([FromQuery] DateOnly date, CancellationToken ct)
        => Ok(await _service.GetNutritionDayAsync(date, ct));

    [HttpGet("nutrition/reports")]
    public async Task<IActionResult> NutritionReport([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] ReportFormat format = ReportFormat.Json, CancellationToken ct = default)
    {
        try
        {
            var report = await _service.GetNutritionReportAsync(from, to, ct);
            if (format == ReportFormat.Json) return Ok(report);
            var table = new ReportTable($"Nutrition-{from:yyyyMMdd}-to-{to:yyyyMMdd}",
                new[] { "Date", "Time", "Food", "Quantity", "Unit", "Carbs", "Protein", "Fat", "Calories", "Note" },
                report.Rows.Select(r => (IReadOnlyList<string?>)new[] { r.Date.ToString("yyyy-MM-dd"), r.TimeOfDay.ToString(), r.Food, S(r.Quantity), r.Unit, S(r.Carbohydrates), S(r.Protein), S(r.Fat), S(r.Calories), r.Note }).ToList());
            var exported = _exporter.Export(table, format);
            return File(exported.Data, exported.ContentType, exported.FileName);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    private static string? S(decimal? value) => value?.ToString("0.##");

    private static async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (KeyNotFoundException ex) { return new NotFoundObjectResult(new { message = ex.Message }); }
        catch (ArgumentException ex) { return new BadRequestObjectResult(new { message = ex.Message }); }
    }

    private static async Task<IActionResult> DeleteExecute(Func<Task> action)
    {
        try { await action(); return new NoContentResult(); }
        catch (KeyNotFoundException ex) { return new NotFoundObjectResult(new { message = ex.Message }); }
    }
}
