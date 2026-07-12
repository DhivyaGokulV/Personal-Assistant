using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.AssetTracker.Investments;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/asset-tracker/investments")]
public class InvestmentsController : ControllerBase
{
    private readonly IInvestmentService _service;
    private readonly IReportExportService _exporter;

    public InvestmentsController(IInvestmentService service, IReportExportService exporter)
    {
        _service = service;
        _exporter = exporter;
    }

    [HttpGet]
    public async Task<ActionResult<InvestmentPage<InvestmentDto>>> List(
        [FromQuery] string? search, [FromQuery] InvestmentStatus? status, [FromQuery] InvestmentType? type,
        [FromQuery] Guid? tagId, [FromQuery] string? currency,
        [FromQuery] InvestmentSort sortBy = InvestmentSort.Name,
        [FromQuery] SortDirection sortDirection = SortDirection.Asc,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
        => Ok(await _service.ListAsync(new InvestmentQuery(search, status, type, tagId, currency, sortBy, sortDirection, page, pageSize), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvestmentDetailDto>> Get(Guid id, CancellationToken ct) =>
        await Execute(() => _service.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<InvestmentDto>> Create(CreateInvestmentRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException)
        {
            return Error<InvestmentDto>(ex);
        }
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<InvestmentDto>> Update(Guid id, UpdateInvestmentRequest request, CancellationToken ct) =>
        await Execute(() => _service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteAsync(id, ct); return NoContent(); }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    [HttpGet("{id:guid}/status-history")]
    public async Task<ActionResult<IReadOnlyList<InvestmentStatusDto>>> StatusHistory(Guid id, CancellationToken ct) =>
        await Execute(() => _service.GetStatusHistoryAsync(id, ct));

    [HttpPost("{id:guid}/status-history")]
    public async Task<ActionResult<InvestmentStatusDto>> ChangeStatus(Guid id, ChangeInvestmentStatusRequest request, CancellationToken ct) =>
        await Execute(() => _service.ChangeStatusAsync(id, request, ct));

    [HttpGet("{id:guid}/entries")]
    public async Task<ActionResult<InvestmentPage<InvestmentEntryDto>>> Entries(
        Guid id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default) =>
        await Execute(() => _service.GetEntriesAsync(id, from, to, page, pageSize, ct));

    [HttpPost("{id:guid}/entries")]
    public async Task<ActionResult<InvestmentEntryDto>> AddEntry(Guid id, SaveInvestmentEntryRequest request, CancellationToken ct) =>
        await Execute(() => _service.AddEntryAsync(id, request, ct));

    [HttpPatch("{id:guid}/entries/{entryId:guid}")]
    public async Task<ActionResult<InvestmentEntryDto>> UpdateEntry(Guid id, Guid entryId, SaveInvestmentEntryRequest request, CancellationToken ct) =>
        await Execute(() => _service.UpdateEntryAsync(id, entryId, request, ct));

    [HttpDelete("{id:guid}/entries/{entryId:guid}")]
    public async Task<IActionResult> DeleteEntry(Guid id, Guid entryId, CancellationToken ct)
    {
        try { await _service.DeleteEntryAsync(id, entryId, ct); return NoContent(); }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    [HttpGet("{id:guid}/price-history")]
    public async Task<ActionResult<InvestmentPage<InvestmentPriceHistoryDto>>> Prices(
        Guid id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default) =>
        await Execute(() => _service.GetPricesAsync(id, from, to, page, pageSize, ct));

    [HttpPost("{id:guid}/price-history")]
    public async Task<ActionResult<InvestmentPriceHistoryDto>> AddPrice(Guid id, SaveInvestmentPriceRequest request, CancellationToken ct) =>
        await Execute(() => _service.AddPriceAsync(id, request, ct));

    [HttpPatch("{id:guid}/price-history/{priceId:guid}")]
    public async Task<ActionResult<InvestmentPriceHistoryDto>> UpdatePrice(Guid id, Guid priceId, SaveInvestmentPriceRequest request, CancellationToken ct) =>
        await Execute(() => _service.UpdatePriceAsync(id, priceId, request, ct));

    [HttpDelete("{id:guid}/price-history/{priceId:guid}")]
    public async Task<IActionResult> DeletePrice(Guid id, Guid priceId, CancellationToken ct)
    {
        try { await _service.DeletePriceAsync(id, priceId, ct); return NoContent(); }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    [HttpGet("{id:guid}/statistics")]
    public async Task<ActionResult<InvestmentStatisticsDto>> Statistics(
        Guid id, [FromQuery] StatisticsSource source, [FromQuery] StatisticsDuration duration, CancellationToken ct) =>
        await Execute(() => _service.GetStatisticsAsync(id, source, duration, ct));

    [HttpPost("exports")]
    public async Task<IActionResult> Export(InvestmentExportRequest request, CancellationToken ct)
    {
        try
        {
            if (request.Format is not (ReportFormat.Xlsx or ReportFormat.Pdf))
                return BadRequest(new ProblemDetails { Title = "Invalid export format", Detail = "Use Xlsx or Pdf.", Status = 400 });
            var rows = await _service.GetExportAsync(request, ct);
            var table = new ReportTable(
                $"Investments-{request.From:yyyyMMdd}-{request.To:yyyyMMdd}",
                new[] { "Investment", "Record type", "Date", "Currency", "Details", "Quantity", "Unit price", "Amount" },
                rows.Select(x => (IReadOnlyList<string?>)new[]
                {
                    x.Investment, x.RecordType, x.Date.ToString("yyyy-MM-dd"), x.Currency, x.Details,
                    x.Quantity?.ToString("0.########"), x.UnitPrice?.ToString("0.####"), x.Amount?.ToString("0.####")
                }).ToList());
            var file = _exporter.Export(table, request.Format);
            return File(file.Data, file.ContentType, file.FileName);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return Error<T>(ex); }
    }

    private ActionResult<T> Error<T>(Exception ex) => ex switch
    {
        KeyNotFoundException => NotFound(new ProblemDetails { Title = "Not found", Detail = ex.Message, Status = 404 }),
        InvalidOperationException => Conflict(new ProblemDetails { Title = "Conflict", Detail = ex.Message, Status = 409 }),
        _ => BadRequest(new ProblemDetails { Title = "Validation failed", Detail = ex.Message, Status = 400 })
    };

    private IActionResult Error(Exception ex) => ex switch
    {
        KeyNotFoundException => NotFound(new ProblemDetails { Title = "Not found", Detail = ex.Message, Status = 404 }),
        InvalidOperationException => Conflict(new ProblemDetails { Title = "Conflict", Detail = ex.Message, Status = 409 }),
        _ => BadRequest(new ProblemDetails { Title = "Validation failed", Detail = ex.Message, Status = 400 })
    };
}
