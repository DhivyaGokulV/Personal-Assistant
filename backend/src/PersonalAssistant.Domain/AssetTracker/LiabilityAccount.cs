using PersonalAssistant.Domain.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Domain.AssetTracker;

public class LiabilityAccount : EntityBase
{
    public LiabilityAccountCategory Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly CreationDate { get; set; }
    public LiabilityAccountStatus Status { get; set; } = LiabilityAccountStatus.Active;
    public string CurrencyCode { get; set; } = "INR";

    public ICollection<LiabilityAccountEntry> Entries { get; set; } = new List<LiabilityAccountEntry>();
    public ICollection<LiabilityAccountStatusHistory> StatusHistory { get; set; } = new List<LiabilityAccountStatusHistory>();
}

public class LiabilityAccountEntry : EntityBase
{
    public Guid LiabilityAccountId { get; set; }
    public LiabilityAccount? LiabilityAccount { get; set; }

    public LiabilityAccountTxType Type { get; set; }
    public DateOnly Date { get; set; }
    public string? Note { get; set; }
    public decimal Amount { get; set; }
}

public class LiabilityAccountStatusHistory : EntityBase
{
    public Guid LiabilityAccountId { get; set; }
    public LiabilityAccount? LiabilityAccount { get; set; }

    public LiabilityAccountStatus Status { get; set; }
    public DateOnly EffectiveDate { get; set; }
}

public class LiabilityAccountAuditEntry : EntityBase
{
    public Guid LiabilityAccountId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public InvestmentAuditAction Action { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? ChangedFieldsJson { get; set; }
}
