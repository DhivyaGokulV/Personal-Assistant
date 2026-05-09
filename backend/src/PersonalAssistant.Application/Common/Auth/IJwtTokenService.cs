using PersonalAssistant.Domain.Identity;

namespace PersonalAssistant.Application.Common.Auth;

public interface IJwtTokenService
{
    AuthResponse IssueToken(ApplicationUser user);
}
