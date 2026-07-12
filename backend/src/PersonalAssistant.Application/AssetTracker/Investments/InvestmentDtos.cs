using PersonalAssistant.Application.AssetTracker.Common;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.AssetTracker.Investments;

public record InvestmentPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public enum InvestmentSort
{
    Name,
    CreationDate,
    CurrentValue,
    ProfitLossPercent,
    Status
}

public enum SortDirection { Asc, Desc }
public enum StatisticsSource { Entries, Prices }
public enum StatisticsDuration { OneMonth, ThreeMonths, SixMonths, OneYear, ThreeYears, FiveYears }

public record InvestmentQuery(
    string? Search,
    InvestmentStatus? Status,
    InvestmentType? Type,
    Guid? TagId,
    string? Currency,
    InvestmentSort SortBy = InvestmentSort.Name,
    SortDirection SortDirection = SortDirection.Asc,
    int Page = 1,
    int PageSize = 25);

public record InvestmentDto(
    Guid Id,
    string Name,
    string? Description,
    AssetTagBadge? Tag,
    InvestmentType InvestmentType,
    string CurrencyCode,
    DateOnly CreationDate,
    InvestmentStatus Status,
    decimal Units,
    decimal AmountInvested,
    decimal? CurrentPrice,
    decimal CurrentValue,
    decimal RemainingCostBasis,
    decimal? ProfitLossPercent);

public record InvestmentDetailDto(
    InvestmentDto Investment,
    IReadOnlyList<InvestmentStatusDto> StatusHistory);

public record CreateInvestmentRequest(
    string Name,
    string? Description,
    InvestmentType InvestmentType,
    string CurrencyCode,
    Guid? TagId,
    string? NewTagName,
    DateOnly CreationDate);

public record UpdateInvestmentRequest(string Name, string? Description, Guid? TagId);

public record InvestmentStatusDto(Guid Id, InvestmentStatus Status, DateOnly EffectiveDate);
public record ChangeInvestmentStatusRequest(InvestmentStatus Status, DateOnly EffectiveDate);

public record InvestmentEntryDto(
    Guid Id,
    DateOnly Date,
    InvestmentTxType Type,
    string? Note,
    decimal? Quantity,
    decimal? PricePerUnit,
    decimal Amount);

public record SaveInvestmentEntryRequest(
    InvestmentTxType Type,
    DateOnly Date,
    string? Note,
    decimal? PricePerUnit,
    decimal? Quantity,
    decimal? Amount);

public record InvestmentPriceHistoryDto(Guid Id, DateOnly Date, decimal PricePerUnit);
public record SaveInvestmentPriceRequest(DateOnly Date, decimal PricePerUnit);

public record StatisticsPoint(DateOnly Date, decimal Value);
public record InvestmentStatisticsDto(string Metric, string CurrencyCode, IReadOnlyList<StatisticsPoint> Points);

public record InvestmentExportRequest(
    IReadOnlyList<Guid> InvestmentIds,
    DateOnly From,
    DateOnly To,
    ReportFormat Format);

public record InvestmentExportRow(
    string Investment,
    string RecordType,
    DateOnly Date,
    string Currency,
    string Details,
    decimal? Quantity,
    decimal? UnitPrice,
    decimal? Amount);
