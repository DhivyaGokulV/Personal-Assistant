using PersonalAssistant.Domain.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Domain.Finance;

public class Account : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal OpeningBalance { get; set; }
    public DateOnly OpeningDate { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Active;
}
