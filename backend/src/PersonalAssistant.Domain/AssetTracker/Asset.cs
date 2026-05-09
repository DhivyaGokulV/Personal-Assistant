using PersonalAssistant.Domain.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Domain.AssetTracker;

public class Asset : EntityBase
{
    public Guid GroupId { get; set; }
    public AssetGroup? Group { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? TagId { get; set; }
    public AssetTag? Tag { get; set; }

    public DateOnly? BuyingDate { get; set; }
    public decimal? BuyingPrice { get; set; }

    public DateOnly? SellingDate { get; set; }
    public decimal? SellingPrice { get; set; }

    public AssetStatus Status { get; set; } = AssetStatus.InPossession;

    public ICollection<AssetPriceHistory> PriceHistory { get; set; } = new List<AssetPriceHistory>();
}

public class AssetPriceHistory : EntityBase
{
    public Guid AssetId { get; set; }
    public Asset? Asset { get; set; }

    public DateOnly AsOf { get; set; }
    public decimal Price { get; set; }
    public string? Note { get; set; }
}
