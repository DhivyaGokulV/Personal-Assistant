using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.AssetTracker.Liabilities;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/asset-tracker/liabilities")]
public class LiabilitiesController : ControllerBase
{
    private readonly ILiabilityService _service;
    private readonly IReportExportService _exporter;

    public LiabilitiesController(ILiabilityService service, IReportExportService exporter)
    {
        _service = service;
        _exporter = exporter;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LiabilityDto>>> List(
        [FromQuery] LiabilityStatus? status,
        [FromQuery] Guid? tagId,
        CancellationToken ct = default)
        => Ok(await _service.ListAsync(status, tagId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LiabilityDetailDto>> Get(Guid id, CancellationToken ct)
    {
        try { return Ok(await _service.GetAsync(id, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost]
    public async Task<ActionResult<LiabilityDto>> Create([FromBody] CreateLiabilityRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.CreateAsync(req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LiabilityDto>> Update(Guid id, [FromBody] UpdateLiabilityRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.UpdateAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteAsync(id, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/history")]
    public async Task<ActionResult<LiabilityHistoryDto>> AddHistory(Guid id, [FromBody] AddLiabilityEntryRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.AddHistoryAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:guid}/history/{historyId:guid}")]
    public async Task<ActionResult<LiabilityHistoryDto>> UpdateHistory(Guid id, Guid historyId, [FromBody] UpdateLiabilityEntryRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.UpdateHistoryAsync(id, historyId, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:guid}/history/{historyId:guid}")]
    public async Task<IActionResult> DeleteHistory(Guid id, Guid historyId, CancellationToken ct)
    {
        try { await _service.DeleteHistoryAsync(id, historyId, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("reports")]
    public async Task<IActionResult> Report(
        [FromQuery] LiabilityStatus? status,
        [FromQuery] ReportFormat format = ReportFormat.Json,
        CancellationToken ct = default)
    {
        var report = await _service.GetReportAsync(status, ct);
        if (format == ReportFormat.Json) return Ok(report);

        var table = new ReportTable(
            "Liabilities-Report",
            new[] { "Name", "Tag", "Status", "Current Amount", "Last Update" },
            report.Rows.Select(r => (IReadOnlyList<string?>)new[]
            {
                r.Name, r.TagName, r.Status,
                r.CurrentAmount.ToString("0.00"),
                r.LastUpdate?.ToString("yyyy-MM-dd")
            }).ToList());

        var exported = _exporter.Export(table, format);
        return File(exported.Data, exported.ContentType, exported.FileName);
    }
}
