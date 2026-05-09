using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.AssetTracker.Common;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/asset-tracker/tags")]
public class AssetTagsController : ControllerBase
{
    private readonly IAssetTagService _service;
    public AssetTagsController(IAssetTagService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssetTagDto>>> List(CancellationToken ct)
        => Ok(await _service.ListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<AssetTagDto>> Create([FromBody] CreateAssetTagRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        return Ok(await _service.CreateAsync(req, ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AssetTagDto>> Update(Guid id, [FromBody] UpdateAssetTagRequest req, CancellationToken ct)
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
}
