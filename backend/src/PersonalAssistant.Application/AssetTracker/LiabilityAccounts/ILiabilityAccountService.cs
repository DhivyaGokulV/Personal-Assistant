using PersonalAssistant.Application.AssetTracker.PreciousMetals;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.AssetTracker.LiabilityAccounts;

public interface ILiabilityAccountService
{
    Task<AssetTrackerPage<LiabilityAccountDto>> ListAsync(LiabilityAccountCategory category, LiabilityAccountQuery query, CancellationToken ct);
    Task<LiabilityAccountDto> CreateAsync(LiabilityAccountCategory category, SaveLiabilityAccountRequest request, CancellationToken ct);
    Task<LiabilityAccountDto> UpdateAsync(LiabilityAccountCategory category, Guid id, SaveLiabilityAccountRequest request, CancellationToken ct);
    Task DeleteAsync(LiabilityAccountCategory category, Guid id, CancellationToken ct);

    Task<IReadOnlyList<LiabilityAccountStatusDto>> GetStatusHistoryAsync(LiabilityAccountCategory category, Guid id, CancellationToken ct);
    Task<LiabilityAccountStatusDto> ChangeStatusAsync(LiabilityAccountCategory category, Guid id, ChangeLiabilityAccountStatusRequest request, CancellationToken ct);

    Task<AssetTrackerPage<LiabilityAccountEntryDto>> GetEntriesAsync(LiabilityAccountCategory category, Guid id, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct);
    Task<LiabilityAccountEntryDto> AddEntryAsync(LiabilityAccountCategory category, Guid id, SaveLiabilityAccountEntryRequest request, CancellationToken ct);
    Task<LiabilityAccountEntryDto> UpdateEntryAsync(LiabilityAccountCategory category, Guid id, Guid entryId, SaveLiabilityAccountEntryRequest request, CancellationToken ct);
    Task DeleteEntryAsync(LiabilityAccountCategory category, Guid id, Guid entryId, CancellationToken ct);

    Task<LiabilityStatisticsDto> GetStatisticsAsync(LiabilityAccountCategory category, Guid id, LiabilityStatisticsDuration duration, CancellationToken ct);
    Task<IReadOnlyList<LiabilityAccountExportRow>> GetExportAsync(LiabilityAccountCategory category, LiabilityAccountExportRequest request, CancellationToken ct);
}
