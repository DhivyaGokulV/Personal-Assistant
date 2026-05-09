namespace PersonalAssistant.Application.Common.Auth;

public record RegisterRequest(string Email, string Password, string? DisplayName);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, DateTime ExpiresAt, UserProfile User);
public record UserProfile(Guid Id, string Email, string? DisplayName);
