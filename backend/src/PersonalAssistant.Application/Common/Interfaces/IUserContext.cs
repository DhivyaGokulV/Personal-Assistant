namespace PersonalAssistant.Application.Common.Interfaces;

public interface IUserContext
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
