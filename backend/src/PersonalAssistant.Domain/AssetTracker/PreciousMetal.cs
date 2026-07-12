using PersonalAssistant.Domain.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Domain.AssetTracker;

public class PreciousMetal : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly CreationDate { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public bool IsDefault { get; set; }

    public ICollection<PreciousMetalTransaction> Transactions { get; set; } = new List<PreciousMetalTransaction>();
    public ICollection<PreciousMetalPriceHistory> PriceHistory { get; set; } = new List<PreciousMetalPriceHistory>();
}

public class PreciousMetalTransaction : EntityBase
{
    public Guid PreciousMetalId { get; set; }
    public PreciousMetal? PreciousMetal { get; set; }

    public PreciousMetalTxType Type { get; set; }
    public DateOnly Date { get; set; }
    public string? Note { get; set; }
    public decimal Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
}

public class PreciousMetalPriceHistory : EntityBase
{
    public Guid PreciousMetalId { get; set; }
    public PreciousMetal? PreciousMetal { get; set; }

    public DateOnly AsOf { get; set; }
    public decimal PricePerUnit { get; set; }
}

public class PreciousMetalAuditEntry : EntityBase
{
    public Guid PreciousMetalId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public InvestmentAuditAction Action { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? ChangedFieldsJson { get; set; }
}
