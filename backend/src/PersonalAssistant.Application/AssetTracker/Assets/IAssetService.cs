using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.AssetTracker.Assets;

public interface IAssetService
{
    // Groups
    Task<IReadOnlyList<AssetGroupDto>> GetGroupsAsync(CancellationToken ct);
    Task<AssetGroupDto> CreateGroupAsync(CreateAssetGroupRequest req, CancellationToken ct);
    Task<AssetGroupDto> UpdateGroupAsync(Guid id, UpdateAssetGroupRequest req, CancellationToken ct);
    Task DeleteGroupAsync(Guid id, CancellationToken ct);

    // Assets
    Task<IReadOnlyList<AssetDto>> ListAsync(AssetStatus? status, Guid? tagId, Guid? groupId, CancellationToken ct);
    Task<AssetWithHistoryDto> GetAsync(Guid id, CancellationToken ct);
    Task<AssetDto> CreateAsync(CreateAssetRequest req, CancellationToken ct);
    Task<AssetDto> UpdateAsync(Guid id, UpdateAssetRequest req, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);

    // Price history
    Task<AssetPriceHistoryDto> AddPriceAsync(Guid assetId, AddAssetPriceRequest req, CancellationToken ct);
    Task<AssetPriceHistoryDto> UpdatePriceAsync(Guid assetId, Guid priceId, UpdateAssetPriceRequest req, CancellationToken ct);
    Task DeletePriceAsync(Guid assetId, Guid priceId, CancellationToken ct);

    // Reports
    Task<AssetReport> GetReportAsync(AssetStatus? status, CancellationToken ct);
}
