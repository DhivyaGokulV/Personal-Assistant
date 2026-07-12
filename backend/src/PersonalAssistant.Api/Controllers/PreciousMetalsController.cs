using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.AssetTracker.PreciousMetals;
using PersonalAssistant.Application.Common.Reports;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/asset-tracker/precious-metals")]
public class PreciousMetalsController : ControllerBase
{
    private readonly IPreciousMetalService _service;
    private readonly IReportExportService _exporter;

    public PreciousMetalsController(IPreciousMetalService service, IReportExportService exporter)
    {
        _service = service;
        _exporter = exporter;
    }

    [HttpGet]
    public async Task<ActionResult<AssetTrackerPage<PreciousMetalDto>>> List(
        [FromQuery] string? search,
        [FromQuery] PreciousMetalSort sortBy = PreciousMetalSort.Name,
        [FromQuery] AssetSortDirection sortDirection = AssetSortDirection.Asc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default) =>
        Ok(await _service.ListAsync(new PreciousMetalQuery(search, sortBy, sortDirection, page, pageSize), ct));

    [HttpPost]
    public async Task<ActionResult<PreciousMetalDto>> Create(SavePreciousMetalRequest request, CancellationToken ct) =>
        await Execute(() => _service.CreateAsync(request, ct));

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<PreciousMetalDto>> Update(Guid id, SavePreciousMetalRequest request, CancellationToken ct) =>
        await Execute(() => _service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteAsync(id, ct); return NoContent(); }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    [HttpGet("{id:guid}/entries")]
    public async Task<ActionResult<AssetTrackerPage<PreciousMetalEntryDto>>> Entries(
        Guid id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default) =>
        await Execute(() => _service.GetEntriesAsync(id, from, to, page, pageSize, ct));

    [HttpPost("{id:guid}/entries")]
    public async Task<ActionResult<PreciousMetalEntryDto>> AddEntry(Guid id, SavePreciousMetalEntryRequest request, CancellationToken ct) =>
        await Execute(() => _service.AddEntryAsync(id, request, ct));

    [HttpPatch("{id:guid}/entries/{entryId:guid}")]
    public async Task<ActionResult<PreciousMetalEntryDto>> UpdateEntry(Guid id, Guid entryId, SavePreciousMetalEntryRequest request, CancellationToken ct) =>
        await Execute(() => _service.UpdateEntryAsync(id, entryId, request, ct));

    [HttpDelete("{id:guid}/entries/{entryId:guid}")]
    public async Task<IActionResult> DeleteEntry(Guid id, Guid entryId, CancellationToken ct)
    {
        try { await _service.DeleteEntryAsync(id, entryId, ct); return NoContent(); }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    [HttpGet("{id:guid}/price-history")]
    public async Task<ActionResult<AssetTrackerPage<PreciousMetalPriceDto>>> Prices(
        Guid id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default) =>
        await Execute(() => _service.GetPricesAsync(id, from, to, page, pageSize, ct));

    [HttpPost("{id:guid}/price-history")]
    public async Task<ActionResult<PreciousMetalPriceDto>> AddPrice(Guid id, SavePreciousMetalPriceRequest request, CancellationToken ct) =>
        await Execute(() => _service.AddPriceAsync(id, request, ct));

    [HttpDelete("{id:guid}/price-history/{priceId:guid}")]
    public async Task<IActionResult> DeletePrice(Guid id, Guid priceId, CancellationToken ct)
    {
        try { await _service.DeletePriceAsync(id, priceId, ct); return NoContent(); }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    [HttpGet("{id:guid}/statistics")]
    public async Task<ActionResult<PreciousMetalStatisticsDto>> Statistics(
        Guid id, [FromQuery] PreciousMetalStatisticsSource source, [FromQuery] PreciousMetalStatisticsDuration duration, CancellationToken ct) =>
        await Execute(() => _service.GetStatisticsAsync(id, source, duration, ct));

    [HttpPost("exports")]
    public async Task<IActionResult> Export(PreciousMetalExportRequest request, CancellationToken ct)
    {
        try
        {
            if (request.Format is not (ReportFormat.Xlsx or ReportFormat.Pdf))
                return BadRequest(new ProblemDetails { Title = "Invalid export format", Detail = "Use Xlsx or Pdf.", Status = 400 });
            var rows = await _service.GetExportAsync(request, ct);
            var table = new ReportTable(
                $"Precious-Metals-{request.From:yyyyMMdd}-{request.To:yyyyMMdd}",
                new[] { "Metal", "Record type", "Date", "Currency", "Details", "Quantity", "Unit price", "Amount" },
                rows.Select(x => (IReadOnlyList<string?>)new[]
                {
                    x.Metal, x.RecordType, x.Date.ToString("yyyy-MM-dd"), x.Currency, x.Details,
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
