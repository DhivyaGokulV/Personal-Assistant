using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.AssetTracker.Possessions;
using PersonalAssistant.Application.AssetTracker.PreciousMetals;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/asset-tracker/personal-assets")]
public class PersonalAssetsController : ControllerBase
{
    private readonly IPersonalAssetService _service;
    private readonly IReportExportService _exporter;

    public PersonalAssetsController(IPersonalAssetService service, IReportExportService exporter)
    {
        _service = service;
        _exporter = exporter;
    }

    [HttpGet]
    public async Task<ActionResult<AssetTrackerPage<PersonalAssetDto>>> List(
        [FromQuery] string? search,
        [FromQuery] AssetStatus? status = AssetStatus.InPossession,
        [FromQuery] PossessionSort sortBy = PossessionSort.Name,
        [FromQuery] AssetSortDirection sortDirection = AssetSortDirection.Asc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default) =>
        Ok(await _service.ListAsync(new PossessionQuery(search, status, sortBy, sortDirection, page, pageSize), ct));

    [HttpPost]
    public async Task<ActionResult<PersonalAssetDto>> Create(SavePersonalAssetRequest request, CancellationToken ct) =>
        await Execute(() => _service.CreateAsync(request, ct));

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<PersonalAssetDto>> Update(Guid id, SavePersonalAssetRequest request, CancellationToken ct) =>
        await Execute(() => _service.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/sell")]
    public async Task<ActionResult<PersonalAssetDto>> Sell(Guid id, SellPossessionRequest request, CancellationToken ct) =>
        await Execute(() => _service.SellAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteAsync(id, ct); return NoContent(); }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    [HttpPost("exports")]
    public async Task<IActionResult> Export(PossessionExportRequest request, CancellationToken ct)
    {
        try
        {
            if (request.Format is not (ReportFormat.Xlsx or ReportFormat.Pdf))
                return BadRequest(new ProblemDetails { Title = "Invalid export format", Detail = "Use Xlsx or Pdf.", Status = 400 });
            var rows = await _service.GetExportAsync(request, ct);
            var table = new ReportTable(
                $"Personal-Assets-{request.From:yyyyMMdd}-{request.To:yyyyMMdd}",
                new[] { "Name", "Description", "Buying date", "Buying price", "Status", "Selling date", "Selling price", "Selling note" },
                rows.Select(x => (IReadOnlyList<string?>)new[]
                {
                    x.Name, x.Description, x.BuyingDate.ToString("yyyy-MM-dd"), x.BuyingPrice.ToString("0.####"),
                    x.Status.ToString(), x.SellingDate?.ToString("yyyy-MM-dd"), x.SellingPrice?.ToString("0.####"), x.SellingNote
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
