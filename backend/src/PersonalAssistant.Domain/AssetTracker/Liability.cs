using PersonalAssistant.Domain.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Domain.AssetTracker;

public class Liability : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? TagId { get; set; }
    public AssetTag? Tag { get; set; }

    /// <summary>Active = currently outstanding. Past = fully paid off.</summary>
    public LiabilityStatus Status { get; set; } = LiabilityStatus.Active;

    public ICollection<LiabilityHistory> History { get; set; } = new List<LiabilityHistory>();
}

public class LiabilityHistory : EntityBase
{
    public Guid LiabilityId { get; set; }
    public Liability? Liability { get; set; }

    public DateOnly Date { get; set; }
    public LiabilityTxType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}
