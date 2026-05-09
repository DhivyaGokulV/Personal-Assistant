using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.AssetTracker.Investments;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/asset-tracker")]
public class InvestmentsController : ControllerBase
{
    private readonly IInvestmentService _service;
    private readonly IReportExportService _exporter;

    public InvestmentsController(IInvestmentService service, IReportExportService exporter)
    {
        _service = service;
        _exporter = exporter;
    }

    // ===== Groups =====
    [HttpGet("investment-groups")]
    public async Task<ActionResult<IReadOnlyList<InvestmentGroupDto>>> GetGroups(
        [FromQuery] InvestmentStatus? status, CancellationToken ct = default)
        => Ok(await _service.GetGroupsAsync(status, ct));

    [HttpPost("investment-groups")]
    public async Task<ActionResult<InvestmentGroupDto>> CreateGroup([FromBody] CreateInvestmentGroupRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.CreateGroupAsync(req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("investment-groups/{id:guid}")]
    public async Task<ActionResult<InvestmentGroupDto>> UpdateGroup(Guid id, [FromBody] UpdateInvestmentGroupRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.UpdateGroupAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("investment-groups/{id:guid}")]
    public async Task<IActionResult> DeleteGroup(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteGroupAsync(id, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // ===== Investments =====
    [HttpGet("investments")]
    public async Task<ActionResult<IReadOnlyList<InvestmentDto>>> List(
        [FromQuery] InvestmentStatus? status,
        [FromQuery] Guid? tagId,
        [FromQuery] Guid? groupId,
        CancellationToken ct = default)
        => Ok(await _service.ListAsync(status, tagId, groupId, ct));

    [HttpGet("investments/{id:guid}")]
    public async Task<ActionResult<InvestmentDetailDto>> Get(Guid id, CancellationToken ct)
    {
        try { return Ok(await _service.GetAsync(id, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("investments")]
    public async Task<ActionResult<InvestmentDto>> Create([FromBody] CreateInvestmentRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.CreateAsync(req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("investments/{id:guid}")]
    public async Task<ActionResult<InvestmentDto>> Update(Guid id, [FromBody] UpdateInvestmentRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.UpdateAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("investments/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteAsync(id, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // ===== Price history =====
    [HttpPost("investments/{id:guid}/price-history")]
    public async Task<ActionResult<InvestmentPriceHistoryDto>> AddPrice(Guid id, [FromBody] AddInvestmentPriceRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.AddPriceAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("investments/{id:guid}/price-history/{priceId:guid}")]
    public async Task<ActionResult<InvestmentPriceHistoryDto>> UpdatePrice(Guid id, Guid priceId, [FromBody] UpdateInvestmentPriceRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.UpdatePriceAsync(id, priceId, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("investments/{id:guid}/price-history/{priceId:guid}")]
    public async Task<IActionResult> DeletePrice(Guid id, Guid priceId, CancellationToken ct)
    {
        try { await _service.DeletePriceAsync(id, priceId, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // ===== Buy/sell transactions =====
    [HttpPost("investments/{id:guid}/transactions")]
    public async Task<ActionResult<InvestmentTxDto>> AddTx(Guid id, [FromBody] AddInvestmentTxRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.AddTxAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("investments/{id:guid}/transactions/{txId:guid}")]
    public async Task<ActionResult<InvestmentTxDto>> UpdateTx(Guid id, Guid txId, [FromBody] UpdateInvestmentTxRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.UpdateTxAsync(id, txId, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("investments/{id:guid}/transactions/{txId:guid}")]
    public async Task<IActionResult> DeleteTx(Guid id, Guid txId, CancellationToken ct)
    {
        try { await _service.DeleteTxAsync(id, txId, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // ===== Report =====
    [HttpGet("investments/reports")]
    public async Task<IActionResult> Report(
        [FromQuery] InvestmentStatus? status,
        [FromQuery] ReportFormat format = ReportFormat.Json,
        CancellationToken ct = default)
    {
        var report = await _service.GetReportAsync(status, ct);
        if (format == ReportFormat.Json) return Ok(report);

        var table = new ReportTable(
            "Investments-Report",
            new[] { "Group", "Name", "Unit", "Tag", "Status", "Holding", "Current Price", "Current Value", "Invested", "P/L" },
            report.Rows.Select(r => (IReadOnlyList<string?>)new[]
            {
                r.GroupName, r.Name, r.Unit, r.TagName, r.Status,
                r.UnitsHolding.ToString("0.####"),
                r.CurrentPrice.ToString("0.00"),
                r.CurrentValue.ToString("0.00"),
                r.Invested.ToString("0.00"),
                r.ProfitLoss.ToString("0.00")
            }).ToList());

        var exported = _exporter.Export(table, format);
        return File(exported.Data, exported.ContentType, exported.FileName);
    }
}
