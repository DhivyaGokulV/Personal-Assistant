using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.AssetTracker.Assets;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/asset-tracker")]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _service;
    private readonly IReportExportService _exporter;

    public AssetsController(IAssetService service, IReportExportService exporter)
    {
        _service = service;
        _exporter = exporter;
    }

    // ===== Groups =====
    [HttpGet("asset-groups")]
    public async Task<ActionResult<IReadOnlyList<AssetGroupDto>>> GetGroups(CancellationToken ct)
        => Ok(await _service.GetGroupsAsync(ct));

    [HttpPost("asset-groups")]
    public async Task<ActionResult<AssetGroupDto>> CreateGroup([FromBody] CreateAssetGroupRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.CreateGroupAsync(req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("asset-groups/{id:guid}")]
    public async Task<ActionResult<AssetGroupDto>> UpdateGroup(Guid id, [FromBody] UpdateAssetGroupRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.UpdateGroupAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("asset-groups/{id:guid}")]
    public async Task<IActionResult> DeleteGroup(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteGroupAsync(id, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // ===== Assets =====
    [HttpGet("assets")]
    public async Task<ActionResult<IReadOnlyList<AssetDto>>> List(
        [FromQuery] AssetStatus? status,
        [FromQuery] Guid? tagId,
        [FromQuery] Guid? groupId,
        CancellationToken ct = default)
        => Ok(await _service.ListAsync(status, tagId, groupId, ct));

    [HttpGet("assets/{id:guid}")]
    public async Task<ActionResult<AssetWithHistoryDto>> Get(Guid id, CancellationToken ct)
    {
        try { return Ok(await _service.GetAsync(id, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("assets")]
    public async Task<ActionResult<AssetDto>> Create([FromBody] CreateAssetRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.CreateAsync(req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("assets/{id:guid}")]
    public async Task<ActionResult<AssetDto>> Update(Guid id, [FromBody] UpdateAssetRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.UpdateAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("assets/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteAsync(id, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // ===== Price history =====
    [HttpPost("assets/{id:guid}/price-history")]
    public async Task<ActionResult<AssetPriceHistoryDto>> AddPrice(Guid id, [FromBody] AddAssetPriceRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.AddPriceAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("assets/{id:guid}/price-history/{priceId:guid}")]
    public async Task<ActionResult<AssetPriceHistoryDto>> UpdatePrice(Guid id, Guid priceId, [FromBody] UpdateAssetPriceRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.UpdatePriceAsync(id, priceId, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("assets/{id:guid}/price-history/{priceId:guid}")]
    public async Task<IActionResult> DeletePrice(Guid id, Guid priceId, CancellationToken ct)
    {
        try { await _service.DeletePriceAsync(id, priceId, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // ===== Report =====
    [HttpGet("assets/reports")]
    public async Task<IActionResult> Report(
        [FromQuery] AssetStatus? status,
        [FromQuery] ReportFormat format = ReportFormat.Json,
        CancellationToken ct = default)
    {
        var report = await _service.GetReportAsync(status, ct);
        if (format == ReportFormat.Json) return Ok(report);

        var table = new ReportTable(
            "Assets-Report",
            new[] { "Group", "Name", "Tag", "Status", "Buying Date", "Buying Price", "Selling Date", "Selling Price", "Current Price" },
            report.Rows.Select(r => (IReadOnlyList<string?>)new[]
            {
                r.GroupName, r.Name, r.TagName, r.Status,
                r.BuyingDate?.ToString("yyyy-MM-dd"),
                r.BuyingPrice?.ToString("0.00"),
                r.SellingDate?.ToString("yyyy-MM-dd"),
                r.SellingPrice?.ToString("0.00"),
                r.CurrentPrice?.ToString("0.00")
            }).ToList());

        var exported = _exporter.Export(table, format);
        return File(exported.Data, exported.ContentType, exported.FileName);
    }
}
