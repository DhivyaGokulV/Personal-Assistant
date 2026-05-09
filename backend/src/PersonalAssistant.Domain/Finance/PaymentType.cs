using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Finance;

public class PaymentType : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
