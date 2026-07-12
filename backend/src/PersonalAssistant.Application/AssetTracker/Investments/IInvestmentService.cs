namespace PersonalAssistant.Application.AssetTracker.Investments;

public interface IInvestmentService
{
    Task<InvestmentPage<InvestmentDto>> ListAsync(InvestmentQuery query, CancellationToken ct);
    Task<InvestmentDetailDto> GetAsync(Guid id, CancellationToken ct);
    Task<InvestmentDto> CreateAsync(CreateInvestmentRequest request, CancellationToken ct);
    Task<InvestmentDto> UpdateAsync(Guid id, UpdateInvestmentRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<InvestmentStatusDto>> GetStatusHistoryAsync(Guid id, CancellationToken ct);
    Task<InvestmentStatusDto> ChangeStatusAsync(Guid id, ChangeInvestmentStatusRequest request, CancellationToken ct);

    Task<InvestmentPage<InvestmentEntryDto>> GetEntriesAsync(Guid id, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct);
    Task<InvestmentEntryDto> AddEntryAsync(Guid id, SaveInvestmentEntryRequest request, CancellationToken ct);
    Task<InvestmentEntryDto> UpdateEntryAsync(Guid id, Guid entryId, SaveInvestmentEntryRequest request, CancellationToken ct);
    Task DeleteEntryAsync(Guid id, Guid entryId, CancellationToken ct);

    Task<InvestmentPage<InvestmentPriceHistoryDto>> GetPricesAsync(Guid id, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct);
    Task<InvestmentPriceHistoryDto> AddPriceAsync(Guid id, SaveInvestmentPriceRequest request, CancellationToken ct);
    Task<InvestmentPriceHistoryDto> UpdatePriceAsync(Guid id, Guid priceId, SaveInvestmentPriceRequest request, CancellationToken ct);
    Task DeletePriceAsync(Guid id, Guid priceId, CancellationToken ct);

    Task<InvestmentStatisticsDto> GetStatisticsAsync(Guid id, StatisticsSource source, StatisticsDuration duration, CancellationToken ct);
    Task<IReadOnlyList<InvestmentExportRow>> GetExportAsync(InvestmentExportRequest request, CancellationToken ct);
}
