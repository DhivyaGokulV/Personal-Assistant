using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Application.Finance.Budgets;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/finance/budgets")]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _service;
    private readonly IReportExportService _exporter;

    public BudgetsController(IBudgetService service, IReportExportService exporter)
    {
        _service = service;
        _exporter = exporter;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BudgetDto>>> List(CancellationToken ct)
        => Ok(await _service.ListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BudgetDto>> Get(Guid id, CancellationToken ct)
    {
        try { return Ok(await _service.GetAsync(id, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost]
    public async Task<ActionResult<BudgetDto>> Create([FromBody] CreateBudgetRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.CreateAsync(req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BudgetDto>> Update(Guid id, [FromBody] UpdateBudgetRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.UpdateAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteAsync(id, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("{id:guid}/report")]
    public async Task<IActionResult> Report(
        Guid id,
        [FromQuery] ReportFormat format = ReportFormat.Json,
        CancellationToken ct = default)
    {
        BudgetReport report;
        try { report = await _service.GetReportAsync(id, ct); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }

        if (format == ReportFormat.Json) return Ok(report);

        var b = report.Budget;
        var table = new ReportTable(
            $"Budget-{Sanitize(b.Name)}-{b.From:yyyyMMdd}-to-{b.To:yyyyMMdd}",
            new[] { "Date", "Reason", "Account", "Payment Type", "Amount" },
            report.Transactions.Select(r => (IReadOnlyList<string?>)new[]
            {
                r.Date.ToString("yyyy-MM-dd"),
                r.Reason,
                r.AccountName,
                r.PaymentTypeName,
                r.Amount.ToString("0.00")
            }).ToList());

        var exported = _exporter.Export(table, format);
        return File(exported.Data, exported.ContentType, exported.FileName);
    }

    private static string Sanitize(string s)
    {
        var ok = new System.Text.StringBuilder();
        foreach (var ch in s)
        {
            if (char.IsLetterOrDigit(ch) || ch is '-' or '_') ok.Append(ch);
            else if (char.IsWhiteSpace(ch)) ok.Append('-');
        }
        return ok.ToString();
    }
}
