using PersonalAssistant.Domain.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Domain.AssetTracker;

public class Investment : EntityBase
{
    // Retained for compatibility with legacy data. New investments are assigned to
    // a hidden per-user legacy group; groups are no longer exposed by the API/UI.
    public Guid GroupId { get; set; }
    public InvestmentGroup? Group { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? TagId { get; set; }
    public AssetTag? Tag { get; set; }

    public InvestmentType InvestmentType { get; set; } = InvestmentType.UnitBased;
    public string CurrencyCode { get; set; } = "INR";
    public DateOnly CreationDate { get; set; }

    /// <summary>Legacy display unit. New unit-based investments use "unit".</summary>
    public string Unit { get; set; } = "unit";

    /// <summary>
    /// Persisted status. Auto-set to Inactive when units holding reaches 0,
    /// and Active again when a Buy brings units &gt; 0.
    /// </summary>
    public InvestmentStatus Status { get; set; } = InvestmentStatus.Active;

    public ICollection<InvestmentPriceHistory> PriceHistory { get; set; } = new List<InvestmentPriceHistory>();
    public ICollection<InvestmentTransaction> Transactions { get; set; } = new List<InvestmentTransaction>();
    public ICollection<InvestmentStatusHistory> StatusHistory { get; set; } = new List<InvestmentStatusHistory>();
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
    public decimal? Amount { get; set; }
    public string? Note { get; set; }
}

public class InvestmentStatusHistory : EntityBase
{
    public Guid InvestmentId { get; set; }
    public Investment? Investment { get; set; }
    public InvestmentStatus Status { get; set; }
    public DateOnly EffectiveDate { get; set; }
}

public class InvestmentAuditEntry : EntityBase
{
    public Guid InvestmentId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public InvestmentAuditAction Action { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? ChangedFieldsJson { get; set; }
}
