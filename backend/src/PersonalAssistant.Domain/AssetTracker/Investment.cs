using PersonalAssistant.Domain.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Domain.AssetTracker;

public class Investment : EntityBase
{
    public Guid GroupId { get; set; }
    public InvestmentGroup? Group { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? TagId { get; set; }
    public AssetTag? Tag { get; set; }

    /// <summary>Display unit, e.g. "gram", "unit", "share", "acre".</summary>
    public string Unit { get; set; } = "unit";

    /// <summary>
    /// Persisted status. Auto-set to Inactive when units holding reaches 0,
    /// and Active again when a Buy brings units &gt; 0.
    /// </summary>
    public InvestmentStatus Status { get; set; } = InvestmentStatus.Active;

    public ICollection<InvestmentPriceHistory> PriceHistory { get; set; } = new List<InvestmentPriceHistory>();
    public ICollection<InvestmentTransaction> Transactions { get; set; } = new List<InvestmentTransaction>();
}

public class InvestmentPriceHistory : EntityBase
{
    public Guid InvestmentId { get; set; }
    public Investment? Investment { get; set; }

    public DateOnly AsOf { get; set; }
    public decimal Price { get; set; }
    public string? Note { get; set; }
}

public class InvestmentTransaction : EntityBase
{
    public Guid InvestmentId { get; set; }
    public Investment? Investment { get; set; }

    public DateOnly Date { get; set; }
    public InvestmentTxType Type { get; set; }
    public decimal Units { get; set; }
    public decimal Price { get; set; }
    public string? Note { get; set; }
}
