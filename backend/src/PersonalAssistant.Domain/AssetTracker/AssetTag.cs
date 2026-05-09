using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.AssetTracker;

/// <summary>
/// Tag scoped to the Asset Tracker module (separate from Finance Tags).
/// Reused across asset groups, assets, investment groups, investments and liabilities.
/// </summary>
public class AssetTag : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#6c5ce7";
}
