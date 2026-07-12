using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.PasswordVault;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/password-vault")]
public class PasswordVaultController : ControllerBase
{
    private readonly IPasswordVaultService _service;

    public PasswordVaultController(IPasswordVaultService service) => _service = service;

    [HttpGet("status")]
    public async Task<ActionResult<PasswordVaultStatusDto>> Status(CancellationToken ct)
        => Ok(await _service.GetStatusAsync(ct));

    [HttpPost("initialize")]
    public async Task<ActionResult<PasswordVaultStatusDto>> Initialize([FromBody] InitializeVaultRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.InitializeAsync(req, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("reset-master-password")]
    public async Task<ActionResult<PasswordVaultStatusDto>> ResetMasterPassword([FromBody] ResetMasterPasswordRequest req, CancellationToken ct)
    {
        try { return Ok(await _service.ResetMasterPasswordAsync(req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("groups")]
    public async Task<ActionResult<IReadOnlyList<PasswordGroupDto>>> ListGroups(CancellationToken ct)
        => Ok(await _service.ListGroupsAsync(ct));

    [HttpPost("groups")]
    public async Task<ActionResult<PasswordGroupDto>> CreateGroup([FromBody] PasswordGroupRequest req, CancellationToken ct)
        => await Execute(() => _service.CreateGroupAsync(req, ct));

    [HttpPut("groups/{id:guid}")]
    public async Task<ActionResult<PasswordGroupDto>> UpdateGroup(Guid id, [FromBody] PasswordGroupRequest req, CancellationToken ct)
        => await Execute(() => _service.UpdateGroupAsync(id, req, ct));

    [HttpDelete("groups/{id:guid}")]
    public async Task<IActionResult> DeleteGroup(Guid id, CancellationToken ct)
        => await DeleteExecute(() => _service.DeleteGroupAsync(id, ct));

    [HttpGet("entries")]
    public async Task<ActionResult<IReadOnlyList<PasswordEntryDto>>> ListEntries([FromQuery] Guid? groupId, [FromQuery] string? search, CancellationToken ct)
        => Ok(await _service.ListEntriesAsync(groupId, search, ct));

    [HttpGet("entries/{id:guid}")]
    public async Task<ActionResult<PasswordEntryDto>> GetEntry(Guid id, CancellationToken ct)
        => await Execute(() => _service.GetEntryAsync(id, ct));

    [HttpPost("entries")]
    public async Task<ActionResult<PasswordEntryDto>> CreateEntry([FromBody] PasswordEntryRequest req, CancellationToken ct)
        => await Execute(() => _service.CreateEntryAsync(req, ct));

    [HttpPut("entries/{id:guid}")]
    public async Task<ActionResult<PasswordEntryDto>> UpdateEntry(Guid id, [FromBody] PasswordEntryRequest req, CancellationToken ct)
        => await Execute(() => _service.UpdateEntryAsync(id, req, ct));

    [HttpDelete("entries/{id:guid}")]
    public async Task<IActionResult> DeleteEntry(Guid id, CancellationToken ct)
        => await DeleteExecute(() => _service.DeleteEntryAsync(id, ct));

    [HttpPost("entries/{entryId:guid}/history")]
    public async Task<ActionResult<PasswordHistoryDto>> AddHistory(Guid entryId, [FromBody] PasswordHistoryRequest req, CancellationToken ct)
        => await Execute(() => _service.AddHistoryAsync(entryId, req, ct));

    [HttpDelete("entries/{entryId:guid}/history/{historyId:guid}")]
    public async Task<IActionResult> DeleteHistory(Guid entryId, Guid historyId, CancellationToken ct)
        => await DeleteExecute(() => _service.DeleteHistoryAsync(entryId, historyId, ct));

    private static async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (KeyNotFoundException ex) { return new NotFoundObjectResult(new { message = ex.Message }); }
        catch (ArgumentException ex) { return new BadRequestObjectResult(new { message = ex.Message }); }
    }

    private static async Task<IActionResult> DeleteExecute(Func<Task> action)
    {
        try { await action(); return new NoContentResult(); }
        catch (KeyNotFoundException ex) { return new NotFoundObjectResult(new { message = ex.Message }); }
    }
}
