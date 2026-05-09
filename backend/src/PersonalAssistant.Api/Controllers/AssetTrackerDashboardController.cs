using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.AssetTracker.Dashboard;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/asset-tracker/dashboard")]
public class AssetTrackerDashboardController : ControllerBase
{
    private readonly IAssetTrackerDashboardService _service;
    public AssetTrackerDashboardController(IAssetTrackerDashboardService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<AssetTrackerDashboardView>> Get(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct = default)
        => Ok(await _service.GetAsync(from, to, ct));
}
