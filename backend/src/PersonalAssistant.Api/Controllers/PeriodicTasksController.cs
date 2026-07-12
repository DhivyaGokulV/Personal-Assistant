using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Application.Tasks.Periodic;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class PeriodicTasksController : ControllerBase
{
    private readonly IPeriodicTaskService _service;
    private readonly IReportExportService _exporter;

    public PeriodicTasksController(IPeriodicTaskService service, IReportExportService exporter)
    {
        _service = service;
        _exporter = exporter;
    }

    [HttpGet("periodic-groups")]
    public async Task<ActionResult<IReadOnlyList<PeriodicGroupDto>>> GetGroups(CancellationToken ct)
        => Ok(await _service.GetGroupsAsync(ct));

    [HttpPost("periodic-groups")]
    public async Task<ActionResult<PeriodicGroupDto>> CreateGroup([FromBody] CreatePeriodicGroupRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        return Ok(await _service.CreateGroupAsync(req, ct));
    }

    [HttpPut("periodic-groups/{id:guid}")]
    public async Task<ActionResult<PeriodicGroupDto>> UpdateGroup(Guid id, [FromBody] UpdatePeriodicGroupRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.UpdateGroupAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("periodic-groups/{id:guid}")]
    public async Task<IActionResult> DeleteGroup(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteGroupAsync(id, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("periodic-tasks")]
    public async Task<ActionResult<IReadOnlyList<PeriodicTaskDto>>> GetTasks(
        [FromQuery] Guid? groupId,
        [FromQuery] bool includeInactive,
        CancellationToken ct)
        => Ok(await _service.GetTasksAsync(groupId, includeInactive, ct));

    [HttpGet("periodic-tasks/{id:guid}")]
    public async Task<ActionResult<PeriodicTaskWithHistoryDto>> GetTask(Guid id, CancellationToken ct)
    {
        try { return Ok(await _service.GetTaskAsync(id, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("periodic-tasks")]
    public async Task<ActionResult<PeriodicTaskDto>> CreateTask([FromBody] CreatePeriodicTaskRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest(new { message = "Title is required." });
        try { return Ok(await _service.CreateTaskAsync(req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("periodic-tasks/{id:guid}")]
    public async Task<ActionResult<PeriodicTaskDto>> UpdateTask(Guid id, [FromBody] UpdatePeriodicTaskRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest(new { message = "Title is required." });
        try { return Ok(await _service.UpdateTaskAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("periodic-tasks/{id:guid}")]
    public async Task<IActionResult> DeleteTask(Guid id, [FromBody] ConfirmDeletePeriodicTaskRequest req, CancellationToken ct)
    {
        try { await _service.DeleteTaskAsync(id, req, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("periodic-groups/reorder")]
    public async Task<IActionResult> ReorderGroups([FromBody] IReadOnlyList<ReorderPeriodicGroupRequest> req, CancellationToken ct)
    {
        await _service.ReorderGroupsAsync(req, ct);
        return NoContent();
    }

    [HttpPut("periodic-tasks/reorder")]
    public async Task<IActionResult> ReorderTasks([FromBody] IReadOnlyList<ReorderPeriodicTaskRequest> req, CancellationToken ct)
    {
        await _service.ReorderTasksAsync(req, ct);
        return NoContent();
    }

    [HttpPost("periodic-tasks/{id:guid}/history")]
    public async Task<ActionResult<PeriodicHistoryDto>> AddHistory(Guid id, [FromBody] AddHistoryRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.AddHistoryAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("periodic-tasks/{id:guid}/history/{historyId:guid}")]
    public async Task<ActionResult<PeriodicHistoryDto>> UpdateHistory(Guid id, Guid historyId, [FromBody] UpdateHistoryRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.UpdateHistoryAsync(id, historyId, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("periodic-tasks/{id:guid}/history/{historyId:guid}")]
    public async Task<IActionResult> DeleteHistory(Guid id, Guid historyId, CancellationToken ct)
    {
        try { await _service.DeleteHistoryAsync(id, historyId, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("periodic-tasks/reports")]
    public async Task<IActionResult> Report(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] ReportFormat format = ReportFormat.Json,
        CancellationToken ct = default)
    {
        var report = await _service.GetReportAsync(from, to, ct);
        if (format == ReportFormat.Json) return Ok(report);

        var table = new ReportTable(
            $"Periodic-Tasks-{report.From:yyyyMMdd}-to-{report.To:yyyyMMdd}",
            new[] { "Group", "Task", "Times Done", "Last Done On", "Next Due On", "Task History" },
            report.Rows.Select(r => (IReadOnlyList<string?>)new[]
            {
                r.GroupName,
                r.TaskTitle,
                r.TimesDoneInRange.ToString(),
                r.LastDoneOn?.ToString("yyyy-MM-dd"),
                r.NextDueOn?.ToString("yyyy-MM-dd"),
                r.TaskHistory
            }).ToList());

        var exported = _exporter.Export(table, format);
        return File(exported.Data, exported.ContentType, exported.FileName);
    }
}
