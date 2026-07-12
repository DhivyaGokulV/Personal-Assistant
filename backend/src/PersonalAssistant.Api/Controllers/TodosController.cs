using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Application.Tasks.Todo;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/todos")]
public class TodosController : ControllerBase
{
    private readonly ITodoService _service;
    private readonly IReportExportService _exporter;

    public TodosController(ITodoService service, IReportExportService exporter)
    {
        _service = service;
        _exporter = exporter;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TodoDto>>> GetAll(
        [FromQuery] TodoStatus? status,
        [FromQuery] TodoSort sortBy = TodoSort.AddedDate,
        [FromQuery] SortOrder order = SortOrder.Desc,
        CancellationToken ct = default)
        => Ok(await _service.GetAllAsync(status, sortBy, order, ct));

    [HttpGet("summary")]
    public async Task<ActionResult<TodoSummary>> Summary(CancellationToken ct)
        => Ok(await _service.GetSummaryAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TodoDto>> Get(Guid id, CancellationToken ct)
    {
        try { return Ok(await _service.GetAsync(id, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost]
    public async Task<ActionResult<TodoDto>> Create([FromBody] CreateTodoRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest(new { message = "Title is required." });
        try { return Ok(await _service.CreateAsync(req, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TodoDto>> Update(Guid id, [FromBody] UpdateTodoRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest(new { message = "Title is required." });
        try { return Ok(await _service.UpdateAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteAsync(id, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("reports")]
    public async Task<IActionResult> Report(
        [FromQuery] DateOnly? asOf,
        [FromQuery] ReportFormat format = ReportFormat.Json,
        CancellationToken ct = default)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await _service.GetReportAsync(date, ct);
        if (format == ReportFormat.Json) return Ok(report);

        var table = new ReportTable(
            $"Todos-As-Of-{report.AsOf:yyyyMMdd}",
            new[] { "Title", "Added", "Deadline", "Days", "Status", "Completed On", "Note" },
            report.Rows.Select(r => (IReadOnlyList<string?>)new[]
            {
                r.Title,
                r.AddedDate.ToString("yyyy-MM-dd"),
                r.Deadline?.ToString("yyyy-MM-dd"),
                r.DaysLeft?.ToString(),
                r.Status.ToString(),
                r.CompletedOn?.ToString("yyyy-MM-dd"),
                r.StatusNote
            }).ToList());

        var exported = _exporter.Export(table, format);
        return File(exported.Data, exported.ContentType, exported.FileName);
    }
}
