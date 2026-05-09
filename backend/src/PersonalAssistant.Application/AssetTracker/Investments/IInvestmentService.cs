using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.AssetTracker.Investments;

public interface IInvestmentService
{
    // Groups
    Task<IReadOnlyList<InvestmentGroupDto>> GetGroupsAsync(InvestmentStatus? status, CancellationToken ct);
    Task<InvestmentGroupDto> CreateGroupAsync(CreateInvestmentGroupRequest req, CancellationToken ct);
    Task<InvestmentGroupDto> UpdateGroupAsync(Guid id, UpdateInvestmentGroupRequest req, CancellationToken ct);
    Task DeleteGroupAsync(Guid id, CancellationToken ct);

    // Investments
    Task<IReadOnlyList<InvestmentDto>> ListAsync(InvestmentStatus? status, Guid? tagId, Guid? groupId, CancellationToken ct);
    Task<InvestmentDetailDto> GetAsync(Guid id, CancellationToken ct);
    Task<InvestmentDto> CreateAsync(CreateInvestmentRequest req, CancellationToken ct);
    Task<InvestmentDto> UpdateAsync(Guid id, UpdateInvestmentRequest req, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);

    // Price history
    Task<InvestmentPriceHistoryDto> AddPriceAsync(Guid investmentId, AddInvestmentPriceRequest req, CancellationToken ct);
    Task<InvestmentPriceHistoryDto> UpdatePriceAsync(Guid investmentId, Guid priceId, UpdateInvestmentPriceRequest req, CancellationToken ct);
    Task DeletePriceAsync(Guid investmentId, Guid priceId, CancellationToken ct);

    // Buy/sell transactions
    Task<InvestmentTxDto> AddTxAsync(Guid investmentId, AddInvestmentTxRequest req, CancellationToken ct);
    Task<InvestmentTxDto> UpdateTxAsync(Guid investmentId, Guid txId, UpdateInvestmentTxRequest req, CancellationToken ct);
    Task DeleteTxAsync(Guid investmentId, Guid txId, CancellationToken ct);

    // Reports
    Task<InvestmentReport> GetReportAsync(InvestmentStatus? status, CancellationToken ct);
}
