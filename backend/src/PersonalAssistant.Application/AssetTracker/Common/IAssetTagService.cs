namespace PersonalAssistant.Application.AssetTracker.Common;

public interface IAssetTagService
{
    Task<IReadOnlyList<AssetTagDto>> ListAsync(CancellationToken ct);
    Task<AssetTagDto> CreateAsync(CreateAssetTagRequest req, CancellationToken ct);
    Task<AssetTagDto> UpdateAsync(Guid id, UpdateAssetTagRequest req, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
