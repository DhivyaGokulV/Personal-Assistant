using PersonalAssistant.Application.AssetTracker.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.AssetTracker.Investments;

public record InvestmentGroupDto(
    Guid Id,
    string Name,
    string? Description,
    AssetTagBadge? Tag,
    InvestmentStatus Status,
    decimal TotalInvested,
    decimal TotalCurrentValue,
    decimal ProfitLoss);

public record CreateInvestmentGroupRequest(string Name, string? Description, Guid? TagId, InvestmentStatus Status);
public record UpdateInvestmentGroupRequest(string Name, string? Description, Guid? TagId, InvestmentStatus Status);

public record InvestmentDto(
    Guid Id,
    Guid GroupId,
    string GroupName,
    string Name,
    string? Description,
    AssetTagBadge? Tag,
    string Unit,
    InvestmentStatus Status,
    decimal? CurrentPrice,
    DateOnly? LastPriceAsOf,
    decimal UnitsHolding,
    decimal AverageBuyPrice,
    decimal CurrentHoldingValue,
    decimal Invested,
    decimal ProfitLoss);

public record InvestmentPriceHistoryDto(Guid Id, DateOnly AsOf, decimal Price, string? Note);

public record InvestmentTxDto(Guid Id, DateOnly Date, InvestmentTxType Type, decimal Units, decimal Price, decimal Total, string? Note);

public record InvestmentDetailDto(
    InvestmentDto Investment,
    IReadOnlyList<InvestmentPriceHistoryDto> Prices,
    IReadOnlyList<InvestmentTxDto> Transactions);

public record CreateInvestmentRequest(
    Guid GroupId,
    string Name,
    string? Description,
    Guid? TagId,
    string Unit,
    decimal CurrentPrice);

public record UpdateInvestmentRequest(
    Guid GroupId,
    string Name,
    string? Description,
    Guid? TagId,
    string Unit);

public record AddInvestmentPriceRequest(DateOnly AsOf, decimal Price, string? Note);
public record UpdateInvestmentPriceRequest(DateOnly AsOf, decimal Price, string? Note);

/// <summary>
/// Buy/Sell entry. If <see cref="Price"/> is null, the service will look up the closest
/// entry in price history (on or before <see cref="Date"/>) and use that.
/// </summary>
public record AddInvestmentTxRequest(DateOnly Date, InvestmentTxType Type, decimal Units, decimal? Price, string? Note);
public record UpdateInvestmentTxRequest(DateOnly Date, InvestmentTxType Type, decimal Units, decimal? Price, string? Note);

public record InvestmentReportRow(
    string GroupName,
    string Name,
    string Unit,
    string? TagName,
    string Status,
    decimal UnitsHolding,
    decimal CurrentPrice,
    decimal CurrentValue,
    decimal Invested,
    decimal ProfitLoss);

public record InvestmentReport(IReadOnlyList<InvestmentReportRow> Rows);
