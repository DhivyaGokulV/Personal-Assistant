using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.Finance.Settings;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/finance")]
public class FinanceSettingsController : ControllerBase
{
    private readonly IFinanceSettingsService _service;

    public FinanceSettingsController(IFinanceSettingsService service) => _service = service;

    // ===== Accounts =====
    [HttpGet("accounts")]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAccounts(
        [FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _service.GetAccountsAsync(includeInactive, ct));

    [HttpPost("accounts")]
    public async Task<ActionResult<AccountDto>> CreateAccount([FromBody] CreateAccountRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        return Ok(await _service.CreateAccountAsync(req, ct));
    }

    [HttpPut("accounts/{id:guid}")]
    public async Task<ActionResult<AccountDto>> UpdateAccount(Guid id, [FromBody] UpdateAccountRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.UpdateAccountAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("accounts/{id:guid}")]
    public async Task<IActionResult> DeleteAccount(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteAccountAsync(id, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // ===== Categories =====
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken ct)
        => Ok(await _service.GetCategoriesAsync(ct));

    [HttpPost("categories")]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        return Ok(await _service.CreateCategoryAsync(req, ct));
    }

    [HttpPut("categories/{id:guid}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.UpdateCategoryAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteCategoryAsync(id, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // ===== Payment Types =====
    [HttpGet("payment-types")]
    public async Task<ActionResult<IReadOnlyList<PaymentTypeDto>>> GetPaymentTypes(CancellationToken ct)
        => Ok(await _service.GetPaymentTypesAsync(ct));

    [HttpPost("payment-types")]
    public async Task<ActionResult<PaymentTypeDto>> CreatePaymentType([FromBody] CreatePaymentTypeRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        return Ok(await _service.CreatePaymentTypeAsync(req, ct));
    }

    [HttpPut("payment-types/{id:guid}")]
    public async Task<ActionResult<PaymentTypeDto>> UpdatePaymentType(Guid id, [FromBody] UpdatePaymentTypeRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.UpdatePaymentTypeAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("payment-types/{id:guid}")]
    public async Task<IActionResult> DeletePaymentType(Guid id, CancellationToken ct)
    {
        try { await _service.DeletePaymentTypeAsync(id, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // ===== Tags =====
    [HttpGet("tags")]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> GetTags(CancellationToken ct)
        => Ok(await _service.GetTagsAsync(ct));

    [HttpPost("tags")]
    public async Task<ActionResult<TagDto>> CreateTag([FromBody] CreateTagRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        return Ok(await _service.CreateTagAsync(req, ct));
    }

    [HttpPut("tags/{id:guid}")]
    public async Task<ActionResult<TagDto>> UpdateTag(Guid id, [FromBody] UpdateTagRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required." });
        try { return Ok(await _service.UpdateTagAsync(id, req, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("tags/{id:guid}")]
    public async Task<IActionResult> DeleteTag(Guid id, CancellationToken ct)
    {
        try { await _service.DeleteTagAsync(id, ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}
