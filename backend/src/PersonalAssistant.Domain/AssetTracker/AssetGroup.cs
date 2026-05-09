using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.AssetTracker;

public class AssetGroup : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? TagId { get; set; }
    public AssetTag? Tag { get; set; }

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
