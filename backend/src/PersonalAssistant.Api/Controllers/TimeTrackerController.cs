using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Application.TimeTracker;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/time-tracker")]
public class TimeTrackerController : ControllerBase
{
    private readonly ITimeTrackerService _service;
    private readonly IReportExportService _exporter;

    public TimeTrackerController(ITimeTrackerService service, IReportExportService exporter)
    {
        _service = service;
        _exporter = exporter;
    }

    [HttpGet("entries")]
    public async Task<ActionResult<TimePagedResult<TimeEntryDto>>> List(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] string? activity, [FromQuery] string? tag,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
        => Ok(await _service.ListAsync(new TimeEntryFilters(from, to, activity, tag), page, pageSize, ct));

    [HttpGet("entries/{id:guid}")]
    public async Task<ActionResult<TimeEntryDto>> Get(Guid id, CancellationToken ct)
    {
        try { return Ok(await _service.GetAsync(id, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("entries")]
    public async Task<ActionResult<TimeEntryDto>> Create([FromBody] TimeEntryRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.CreateAsync(req, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("entries/{id:guid}")]
    public async Task<ActionResult<TimeEntryDto>> Update(Guid id, [FromBody] TimeEntryRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.UpdateAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("entries/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteAsync(id, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("reports")]
    public async Task<IActionResult> Report(
        [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] string? activity, [FromQuery] string? tag,
        [FromQuery] ReportFormat format = ReportFormat.Json, CancellationToken ct = default)
    {
        try
        {
            var report = await _service.GetReportAsync(new TimeEntryFilters(from, to, activity, tag), ct);
            if (format == ReportFormat.Json) return Ok(report);
            var table = new ReportTable(
                $"Time-Report-{from:yyyyMMddHHmm}-to-{to:yyyyMMddHHmm}",
                new[] { "Start", "End", "Activity", "Tag", "Minutes", "Note" },
                report.Rows.Select(r => (IReadOnlyList<string?>)new[] { r.StartTime.ToString("yyyy-MM-dd HH:mm"), r.EndTime.ToString("yyyy-MM-dd HH:mm"), r.Activity, r.Tag, r.Minutes.ToString(), r.Note }).ToList());
            var exported = _exporter.Export(table, format);
            return File(exported.Data, exported.ContentType, exported.FileName);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
