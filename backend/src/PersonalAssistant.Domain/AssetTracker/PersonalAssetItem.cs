using PersonalAssistant.Domain.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Domain.AssetTracker;

public class PersonalAssetItem : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly BuyingDate { get; set; }
    public decimal BuyingPrice { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.InPossession;
    public DateOnly? SellingDate { get; set; }
    public decimal? SellingPrice { get; set; }
    public string? SellingNote { get; set; }
    public string CurrencyCode { get; set; } = "INR";
}

public class PersonalAssetAuditEntry : EntityBase
{
    public Guid PersonalAssetItemId { get; set; }
    public InvestmentAuditAction Action { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? ChangedFieldsJson { get; set; }
}
