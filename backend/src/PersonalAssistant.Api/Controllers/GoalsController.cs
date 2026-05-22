using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Application.Goals;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/goals")]
public class GoalsController : ControllerBase
{
    private readonly IGoalService _service;
    private readonly IReportExportService _exporter;

    public GoalsController(IGoalService service, IReportExportService exporter)
    {
        _service = service;
        _exporter = exporter;
    }

    [HttpGet("plans")]
    public async Task<ActionResult<IReadOnlyList<GoalPlanDto>>> ListPlans(CancellationToken ct)
        => Ok(await _service.ListPlansAsync(ct));

    [HttpGet("plans/{id:guid}")]
    public async Task<ActionResult<GoalPlanDto>> GetPlan(Guid id, CancellationToken ct)
        => await Execute(() => _service.GetPlanAsync(id, ct));

    [HttpPost("plans")]
    public async Task<ActionResult<GoalPlanDto>> CreatePlan([FromBody] GoalPlanRequest req, CancellationToken ct)
        => await Execute(() => _service.CreatePlanAsync(req, ct));

    [HttpPut("plans/{id:guid}")]
    public async Task<ActionResult<GoalPlanDto>> UpdatePlan(Guid id, [FromBody] GoalPlanRequest req, CancellationToken ct)
        => await Execute(() => _service.UpdatePlanAsync(id, req, ct));

    [HttpDelete("plans/{id:guid}")]
    public async Task<IActionResult> DeletePlan(Guid id, CancellationToken ct)
        => await DeleteExecute(() => _service.DeletePlanAsync(id, ct));

    [HttpPost("plans/{planId:guid}/goals")]
    public async Task<ActionResult<GoalDto>> CreateGoal(Guid planId, [FromBody] GoalRequest req, CancellationToken ct)
        => await Execute(() => _service.CreateGoalAsync(planId, req, ct));

    [HttpPut("goals/{goalId:guid}")]
    public async Task<ActionResult<GoalDto>> UpdateGoal(Guid goalId, [FromBody] GoalRequest req, CancellationToken ct)
        => await Execute(() => _service.UpdateGoalAsync(goalId, req, ct));

    [HttpDelete("goals/{goalId:guid}")]
    public async Task<IActionResult> DeleteGoal(Guid goalId, CancellationToken ct)
        => await DeleteExecute(() => _service.DeleteGoalAsync(goalId, ct));

    [HttpPost("goals/{goalId:guid}/steps")]
    public async Task<ActionResult<GoalStepDto>> CreateStep(Guid goalId, [FromBody] GoalStepRequest req, CancellationToken ct)
        => await Execute(() => _service.CreateStepAsync(goalId, req, ct));

    [HttpPut("steps/{stepId:guid}")]
    public async Task<ActionResult<GoalStepDto>> UpdateStep(Guid stepId, [FromBody] GoalStepRequest req, CancellationToken ct)
        => await Execute(() => _service.UpdateStepAsync(stepId, req, ct));

    [HttpDelete("steps/{stepId:guid}")]
    public async Task<IActionResult> DeleteStep(Guid stepId, CancellationToken ct)
        => await DeleteExecute(() => _service.DeleteStepAsync(stepId, ct));

    [HttpGet("reports")]
    public async Task<IActionResult> Report([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] ReportFormat format = ReportFormat.Json, CancellationToken ct = default)
    {
        try
        {
            var report = await _service.GetReportAsync(from, to, ct);
            if (format == ReportFormat.Json) return Ok(report);
            var table = new ReportTable($"Goals-{from:yyyyMMdd}-to-{to:yyyyMMdd}",
                new[] { "Plan", "Goal", "Step", "Start", "Deadline", "Achieved", "Status" },
                report.Rows.Select(r => (IReadOnlyList<string?>)new[] { r.PlanName, r.GoalName, r.StepName, r.StartDate.ToString("yyyy-MM-dd"), r.Deadline.ToString("yyyy-MM-dd"), r.AchievedDate?.ToString("yyyy-MM-dd"), r.Status }).ToList());
            var exported = _exporter.Export(table, format);
            return File(exported.Data, exported.ContentType, exported.FileName);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

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
