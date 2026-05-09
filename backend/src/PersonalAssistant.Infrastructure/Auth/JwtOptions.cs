namespace PersonalAssistant.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "PersonalAssistant";
    public string Audience { get; set; } = "PersonalAssistant.Client";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60 * 12;
}
