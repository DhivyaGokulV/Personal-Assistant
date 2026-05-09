using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PersonalAssistant.Application.Common.Auth;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Domain.Identity;

namespace PersonalAssistant.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IJwtTokenService _tokens;
    private readonly IUserContext _currentUser;

    public AuthController(UserManager<ApplicationUser> users, IJwtTokenService tokens, IUserContext currentUser)
    {
        _users = users;
        _tokens = tokens;
        _currentUser = currentUser;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var existing = await _users.FindByEmailAsync(request.Email);
        if (existing is not null)
            return Conflict(new { message = "An account with that email already exists." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            EmailConfirmed = true
        };
        var result = await _users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(_tokens.IssueToken(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email);
        if (user is null) return Unauthorized(new { message = "Invalid credentials." });

        var ok = await _users.CheckPasswordAsync(user, request.Password);
        if (!ok) return Unauthorized(new { message = "Invalid credentials." });

        return Ok(_tokens.IssueToken(user));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfile>> Me()
    {
        if (_currentUser.UserId is null) return Unauthorized();
        var user = await _users.FindByIdAsync(_currentUser.UserId.Value.ToString());
        if (user is null) return NotFound();
        return Ok(new UserProfile(user.Id, user.Email ?? string.Empty, user.DisplayName));
    }
}
