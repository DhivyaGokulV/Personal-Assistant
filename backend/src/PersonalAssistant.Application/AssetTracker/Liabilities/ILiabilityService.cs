using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.AssetTracker.Liabilities;

public interface ILiabilityService
{
    Task<IReadOnlyList<LiabilityDto>> ListAsync(LiabilityStatus? status, Guid? tagId, CancellationToken ct);
    Task<LiabilityDetailDto> GetAsync(Guid id, CancellationToken ct);
    Task<LiabilityDto> CreateAsync(CreateLiabilityRequest req, CancellationToken ct);
    Task<LiabilityDto> UpdateAsync(Guid id, UpdateLiabilityRequest req, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);

    Task<LiabilityHistoryDto> AddHistoryAsync(Guid liabilityId, AddLiabilityEntryRequest req, CancellationToken ct);
    Task<LiabilityHistoryDto> UpdateHistoryAsync(Guid liabilityId, Guid historyId, UpdateLiabilityEntryRequest req, CancellationToken ct);
    Task DeleteHistoryAsync(Guid liabilityId, Guid historyId, CancellationToken ct);

    Task<LiabilityReport> GetReportAsync(LiabilityStatus? status, CancellationToken ct);
}
