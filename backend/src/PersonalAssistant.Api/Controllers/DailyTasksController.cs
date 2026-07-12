using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Application.Tasks.Daily;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class DailyTasksController : ControllerBase
{
    private readonly IDailyTaskService _service;
    private readonly IReportExportService _exporter;

    public DailyTasksController(IDailyTaskService service, IReportExportService exporter)
    {
        _service = service;
        _exporter = exporter;
    }

    [HttpGet("daily-groups")]
    public async Task<ActionResult<IReadOnlyList<DailyTaskGroupDto>>> GetGroups(CancellationToken ct)
        => Ok(await _service.GetGroupsAsync(ct));

    [HttpPost("daily-groups")]
    public async Task<ActionResult<DailyTaskGroupDto>> CreateGroup([FromBody] CreateDailyGroupRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        var dto = await _service.CreateGroupAsync(req, ct);
        return CreatedAtAction(nameof(GetGroups), new { id = dto.Id }, dto);
    }

    [HttpPut("daily-groups/{id:guid}")]
    public async Task<ActionResult<DailyTaskGroupDto>> UpdateGroup(Guid id, [FromBody] UpdateDailyGroupRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.UpdateGroupAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("daily-groups/{id:guid}")]
    public async Task<IActionResult> DeleteGroup(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteGroupAsync(id, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("daily-tasks")]
    public async Task<ActionResult<IReadOnlyList<DailyTaskDto>>> GetTasks(
        [FromQuery] Guid? groupId,
        [FromQuery] bool includeInactive,
        CancellationToken ct)
        => Ok(await _service.GetTasksAsync(groupId, includeInactive, ct));

    [HttpPost("daily-tasks")]
    public async Task<ActionResult<DailyTaskDto>> CreateTask([FromBody] CreateDailyTaskRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest(new { message = "Title is required." });
        try
        {
            var dto = await _service.CreateTaskAsync(req, ct);
            return CreatedAtAction(nameof(GetTasks), new { id = dto.Id }, dto);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("daily-tasks/{id:guid}")]
    public async Task<ActionResult<DailyTaskDto>> UpdateTask(Guid id, [FromBody] UpdateDailyTaskRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest(new { message = "Title is required." });
        try { return Ok(await _service.UpdateTaskAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("daily-tasks/{id:guid}")]
    public async Task<IActionResult> DeleteTask(Guid id, [FromBody] ConfirmDeleteTaskRequest req, CancellationToken ct)
    {
        try { await _service.DeleteTaskAsync(id, req, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("daily-groups/reorder")]
    public async Task<IActionResult> ReorderGroups([FromBody] IReadOnlyList<ReorderDailyGroupRequest> req, CancellationToken ct)
    {
        await _service.ReorderGroupsAsync(req, ct);
        return NoContent();
    }

    [HttpPut("daily-tasks/reorder")]
    public async Task<IActionResult> ReorderTasks([FromBody] IReadOnlyList<ReorderDailyTaskRequest> req, CancellationToken ct)
    {
        await _service.ReorderTasksAsync(req, ct);
        return NoContent();
    }

    [HttpGet("daily-tasks/by-date")]
    public async Task<ActionResult<DailyByDateView>> GetByDate([FromQuery] DateOnly? date, CancellationToken ct)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await _service.GetByDateAsync(d, ct));
    }

    [HttpPut("daily-tasks/{id:guid}/completion")]
    public async Task<ActionResult<DailyCompletionDto>> UpsertCompletion(
        Guid id,
        [FromQuery] DateOnly date,
        [FromBody] UpsertCompletionRequest req,
        CancellationToken ct)
    {
        try { return Ok(await _service.UpsertCompletionAsync(id, date, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("daily-tasks/reports/day-wise")]
    public async Task<IActionResult> DayWiseReport(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] ReportFormat format = ReportFormat.Json,
        CancellationToken ct = default)
    {
        var report = await _service.GetDayWiseReportAsync(from, to, ct);
        if (format == ReportFormat.Json) return Ok(report);

        var table = new ReportTable(
            $"Daily-Day-Wise-{report.From:yyyyMMdd}-to-{report.To:yyyyMMdd}",
            new[] { "Date", "Group", "Task", "Status", "Note" },
            report.Rows.Select(r => (IReadOnlyList<string?>)new[]
            {
                r.Date.ToString("yyyy-MM-dd"),
                r.GroupName,
                r.TaskTitle,
                r.IsCompleted ? "Completed" : "Not completed",
                r.Note
            }).ToList());

        var exported = _exporter.Export(table, format);
        return File(exported.Data, exported.ContentType, exported.FileName);
    }

    [HttpGet("daily-tasks/reports/consolidated")]
    public async Task<IActionResult> ConsolidatedReport(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] ReportFormat format = ReportFormat.Json,
        CancellationToken ct = default)
    {
        var report = await _service.GetConsolidatedReportAsync(from, to, ct);
        if (format == ReportFormat.Json) return Ok(report);

        var columns = new List<string> { "Task Group", "Task Name", "Number of days completed", "Number of days in period" };
        columns.AddRange(report.Dates.Select(d => d.ToString("yyyy-MM-dd")));

        var rows = report.Rows.Select(r =>
        {
            var values = new List<string?> { r.TaskGroup, r.TaskName, r.DaysCompleted.ToString(), r.DaysInPeriod.ToString() };
            values.AddRange(report.Dates.Select(d => r.StatusByDate.TryGetValue(d, out var s) ? s : ""));
            return (IReadOnlyList<string?>)values;
        }).ToList();

        rows.Add((IReadOnlyList<string?>)new[] { "TOTAL", "Tasks completed", "", "" }.Concat(report.Dates.Select(d => report.Totals.First(t => t.Date == d).TasksCompleted.ToString())).ToList());
        rows.Add((IReadOnlyList<string?>)new[] { "TOTAL", "Tasks active", "", "" }.Concat(report.Dates.Select(d => report.Totals.First(t => t.Date == d).TasksActive.ToString())).ToList());

        var table = new ReportTable($"Daily-Consolidated-{report.From:yyyyMMdd}-to-{report.To:yyyyMMdd}", columns, rows);
        var exported = _exporter.Export(table, format);
        return File(exported.Data, exported.ContentType, exported.FileName);
    }

    [HttpGet("daily-tasks/reports/task-wise")]
    public async Task<IActionResult> TaskWiseReport(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] ReportFormat format = ReportFormat.Json,
        CancellationToken ct = default)
    {
        var report = await _service.GetTaskWiseReportAsync(from, to, ct);
        if (format == ReportFormat.Json) return Ok(report);

        var table = new ReportTable(
            $"Daily-Task-Wise-{report.From:yyyyMMdd}-to-{report.To:yyyyMMdd}",
            new[] { "Group", "Task", "Times Done", "Last Done On" },
            report.Rows.Select(r => (IReadOnlyList<string?>)new[]
            {
                r.GroupName,
                r.TaskTitle,
                r.TimesDone.ToString(),
                r.LastDoneOn?.ToString("yyyy-MM-dd")
            }).ToList());

        var exported = _exporter.Export(table, format);
        return File(exported.Data, exported.ContentType, exported.FileName);
    }

    [HttpGet("daily-tasks/archive")]
    public async Task<ActionResult<IReadOnlyList<TaskArchiveDto>>> Archive(CancellationToken ct)
        => Ok(await _service.GetArchiveAsync(ct));
}
