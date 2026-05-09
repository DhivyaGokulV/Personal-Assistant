using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Application.Finance.Transactions;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/finance/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _service;
    private readonly IReportExportService _exporter;

    public TransactionsController(ITransactionService service, IReportExportService exporter)
    {
        _service = service;
        _exporter = exporter;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TransactionDto>>> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? paymentTypeId,
        [FromQuery] Guid? tagId,
        [FromQuery] TransactionType? type,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var filters = new TransactionFilters(from, to, accountId, categoryId, paymentTypeId, tagId, type, search);
        return Ok(await _service.ListAsync(filters, page, pageSize, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> Get(Guid id, CancellationToken ct)
    {
        try { return Ok(await _service.GetAsync(id, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create([FromBody] CreateTransactionRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Reason)) return BadRequest(new { message = "Reason is required." });
        try { return Ok(await _service.CreateAsync(req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> Update(Guid id, [FromBody] UpdateTransactionRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Reason)) return BadRequest(new { message = "Reason is required." });
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

    [HttpPost("transfer")]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> Transfer([FromBody] CreateSelfTransferRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.CreateSelfTransferAsync(req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("reports")]
    public async Task<IActionResult> Report(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] Guid? accountId,
        [FromQuery] ReportFormat format = ReportFormat.Json,
        CancellationToken ct = default)
    {
        var report = await _service.GetReportAsync(from, to, accountId, ct);
        if (format == ReportFormat.Json) return Ok(report);

        var table = new ReportTable(
            $"Transactions-{report.From:yyyyMMdd}-to-{report.To:yyyyMMdd}",
            new[] { "Date", "Type", "Account", "Amount", "Reason", "Note", "Category", "Payment Type", "Tags" },
            report.Rows.Select(r => (IReadOnlyList<string?>)new[]
            {
                r.Date.ToString("yyyy-MM-dd"),
                r.Type,
                r.AccountName,
                r.Amount.ToString("0.00"),
                r.Reason,
                r.Note,
                r.CategoryName,
                r.PaymentTypeName,
                r.TagsCsv
            }).ToList());

        var exported = _exporter.Export(table, format);
        return File(exported.Data, exported.ContentType, exported.FileName);
    }
}
