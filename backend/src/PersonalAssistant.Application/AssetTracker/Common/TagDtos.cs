namespace PersonalAssistant.Application.AssetTracker.Common;

public record AssetTagDto(Guid Id, string Name, string? Description, string Color);
public record CreateAssetTagRequest(string Name, string? Description, string Color);
public record UpdateAssetTagRequest(string Name, string? Description, string Color);

public record AssetTagBadge(Guid Id, string Name, string Color);
