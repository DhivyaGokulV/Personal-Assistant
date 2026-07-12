using PersonalAssistant.Application.AssetTracker.PreciousMetals;

namespace PersonalAssistant.Application.AssetTracker.Possessions;

public interface IJewelleryService
{
    Task<AssetTrackerPage<JewelleryDto>> ListAsync(PossessionQuery query, CancellationToken ct);
    Task<JewelleryDto> CreateAsync(SaveJewelleryRequest request, CancellationToken ct);
    Task<JewelleryDto> UpdateAsync(Guid id, SaveJewelleryRequest request, CancellationToken ct);
    Task<JewelleryDto> SellAsync(Guid id, SellPossessionRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<JewelleryDto>> GetExportAsync(PossessionExportRequest request, CancellationToken ct);
}
