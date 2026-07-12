using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.AssetTracker.LiabilityAccounts;
using PersonalAssistant.Application.AssetTracker.PreciousMetals;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Api.Controllers;

public abstract class LiabilityAccountControllerBase : ControllerBase
{
    private readonly ILiabilityAccountService _service;
    private readonly IReportExportService _exporter;

    protected LiabilityAccountControllerBase(ILiabilityAccountService service, IReportExportService exporter)
    {
        _service = service;
        _exporter = exporter;
    }

    protected abstract LiabilityAccountCategory Category { get; }
    protected abstract string ReportTitle { get; }

    [HttpGet]
    public async Task<ActionResult<AssetTrackerPage<LiabilityAccountDto>>> List(
        [FromQuery] string? search,
        [FromQuery] LiabilityAccountStatus? status = LiabilityAccountStatus.Active,
        [FromQuery] LiabilityAccountSort sortBy = LiabilityAccountSort.Name,
        [FromQuery] AssetSortDirection sortDirection = AssetSortDirection.Asc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default) =>
        Ok(await _service.ListAsync(Category, new LiabilityAccountQuery(search, status, sortBy, sortDirection, page, pageSize), ct));

    [HttpPost]
    public async Task<ActionResult<LiabilityAccountDto>> Create(SaveLiabilityAccountRequest request, CancellationToken ct) =>
        await Execute(() => _service.CreateAsync(Category, request, ct));

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<LiabilityAccountDto>> Update(Guid id, SaveLiabilityAccountRequest request, CancellationToken ct) =>
        await Execute(() => _service.UpdateAsync(Category, id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteAsync(Category, id, ct); return NoContent(); }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    [HttpGet("{id:guid}/status-history")]
    public async Task<ActionResult<IReadOnlyList<LiabilityAccountStatusDto>>> StatusHistory(Guid id, CancellationToken ct) =>
        await Execute(() => _service.GetStatusHistoryAsync(Category, id, ct));

    [HttpPost("{id:guid}/status-history")]
    public async Task<ActionResult<LiabilityAccountStatusDto>> ChangeStatus(Guid id, ChangeLiabilityAccountStatusRequest request, CancellationToken ct) =>
        await Execute(() => _service.ChangeStatusAsync(Category, id, request, ct));

    [HttpGet("{id:guid}/entries")]
    public async Task<ActionResult<AssetTrackerPage<LiabilityAccountEntryDto>>> Entries(
        Guid id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default) =>
        await Execute(() => _service.GetEntriesAsync(Category, id, from, to, page, pageSize, ct));

    [HttpPost("{id:guid}/entries")]
    public async Task<ActionResult<LiabilityAccountEntryDto>> AddEntry(Guid id, SaveLiabilityAccountEntryRequest request, CancellationToken ct) =>
        await Execute(() => _service.AddEntryAsync(Category, id, request, ct));

    [HttpPatch("{id:guid}/entries/{entryId:guid}")]
    public async Task<ActionResult<LiabilityAccountEntryDto>> UpdateEntry(Guid id, Guid entryId, SaveLiabilityAccountEntryRequest request, CancellationToken ct) =>
        await Execute(() => _service.UpdateEntryAsync(Category, id, entryId, request, ct));

    [HttpDelete("{id:guid}/entries/{entryId:guid}")]
    public async Task<IActionResult> DeleteEntry(Guid id, Guid entryId, CancellationToken ct)
    {
        try { await _service.DeleteEntryAsync(Category, id, entryId, ct); return NoContent(); }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    [HttpGet("{id:guid}/statistics")]
    public async Task<ActionResult<LiabilityStatisticsDto>> Statistics(
        Guid id, [FromQuery] LiabilityStatisticsDuration duration, CancellationToken ct) =>
        await Execute(() => _service.GetStatisticsAsync(Category, id, duration, ct));

    [HttpPost("exports")]
    public async Task<IActionResult> Export(LiabilityAccountExportRequest request, CancellationToken ct)
    {
        try
        {
            if (request.Format is not (ReportFormat.Xlsx or ReportFormat.Pdf))
                return BadRequest(new ProblemDetails { Title = "Invalid export format", Detail = "Use Xlsx or Pdf.", Status = 400 });
            var rows = await _service.GetExportAsync(Category, request, ct);
            var table = new ReportTable(
                $"{ReportTitle}-{request.From:yyyyMMdd}-{request.To:yyyyMMdd}",
                new[] { "Name", "Record type", "Date", "Currency", "Details", "Amount", "Running balance" },
                rows.Select(x => (IReadOnlyList<string?>)new[]
                {
                    x.Account, x.RecordType, x.Date.ToString("yyyy-MM-dd"), x.Currency, x.Details,
                    x.Amount?.ToString("0.####"), x.RunningBalance?.ToString("0.####")
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

[ApiController]
[Authorize]
[Route("api/asset-tracker/loans")]
public class LoansController : LiabilityAccountControllerBase
{
    public LoansController(ILiabilityAccountService service, IReportExportService exporter) : base(service, exporter) { }
    protected override LiabilityAccountCategory Category => LiabilityAccountCategory.Loan;
    protected override string ReportTitle => "Loans";
}

[ApiController]
[Authorize]
[Route("api/asset-tracker/debts")]
public class DebtsController : LiabilityAccountControllerBase
{
    public DebtsController(ILiabilityAccountService service, IReportExportService exporter) : base(service, exporter) { }
    protected override LiabilityAccountCategory Category => LiabilityAccountCategory.Debt;
    protected override string ReportTitle => "Debts";
}

[ApiController]
[Authorize]
[Route("api/asset-tracker/credit-cards")]
public class CreditCardsController : LiabilityAccountControllerBase
{
    public CreditCardsController(ILiabilityAccountService service, IReportExportService exporter) : base(service, exporter) { }
    protected override LiabilityAccountCategory Category => LiabilityAccountCategory.CreditCard;
    protected override string ReportTitle => "Credit-Cards";
}
