using PersonalAssistant.Application.AssetTracker.PreciousMetals;

namespace PersonalAssistant.Application.AssetTracker.Possessions;

public interface IPersonalAssetService
{
    Task<AssetTrackerPage<PersonalAssetDto>> ListAsync(PossessionQuery query, CancellationToken ct);
    Task<PersonalAssetDto> CreateAsync(SavePersonalAssetRequest request, CancellationToken ct);
    Task<PersonalAssetDto> UpdateAsync(Guid id, SavePersonalAssetRequest request, CancellationToken ct);
    Task<PersonalAssetDto> SellAsync(Guid id, SellPossessionRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<PersonalAssetDto>> GetExportAsync(PossessionExportRequest request, CancellationToken ct);
}
