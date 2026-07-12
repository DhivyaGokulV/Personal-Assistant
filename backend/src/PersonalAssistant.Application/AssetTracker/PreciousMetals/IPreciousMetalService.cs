namespace PersonalAssistant.Application.AssetTracker.PreciousMetals;

public interface IPreciousMetalService
{
    Task<AssetTrackerPage<PreciousMetalDto>> ListAsync(PreciousMetalQuery query, CancellationToken ct);
    Task<PreciousMetalDto> CreateAsync(SavePreciousMetalRequest request, CancellationToken ct);
    Task<PreciousMetalDto> UpdateAsync(Guid id, SavePreciousMetalRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);

    Task<AssetTrackerPage<PreciousMetalEntryDto>> GetEntriesAsync(Guid id, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct);
    Task<PreciousMetalEntryDto> AddEntryAsync(Guid id, SavePreciousMetalEntryRequest request, CancellationToken ct);
    Task<PreciousMetalEntryDto> UpdateEntryAsync(Guid id, Guid entryId, SavePreciousMetalEntryRequest request, CancellationToken ct);
    Task DeleteEntryAsync(Guid id, Guid entryId, CancellationToken ct);

    Task<AssetTrackerPage<PreciousMetalPriceDto>> GetPricesAsync(Guid id, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct);
    Task<PreciousMetalPriceDto> AddPriceAsync(Guid id, SavePreciousMetalPriceRequest request, CancellationToken ct);
    Task DeletePriceAsync(Guid id, Guid priceId, CancellationToken ct);

    Task<PreciousMetalStatisticsDto> GetStatisticsAsync(Guid id, PreciousMetalStatisticsSource source, PreciousMetalStatisticsDuration duration, CancellationToken ct);
    Task<IReadOnlyList<PreciousMetalExportRow>> GetExportAsync(PreciousMetalExportRequest request, CancellationToken ct);
}
