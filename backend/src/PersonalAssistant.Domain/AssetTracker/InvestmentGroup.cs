using PersonalAssistant.Domain.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Domain.AssetTracker;

public class InvestmentGroup : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? TagId { get; set; }
    public AssetTag? Tag { get; set; }
    public InvestmentStatus Status { get; set; } = InvestmentStatus.Active;

    public ICollection<Investment> Investments { get; set; } = new List<Investment>();
}
