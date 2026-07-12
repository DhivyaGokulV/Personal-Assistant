using PersonalAssistant.Application.AssetTracker.PreciousMetals;
using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.AssetTracker.LiabilityAccounts;

public enum LiabilityAccountSort { Name, CreationDate, StandingAmount, LastEntryDate, Status }
public enum LiabilityStatisticsDuration { OneMonth, ThreeMonths, SixMonths, OneYear, ThreeYears, FiveYears }

public record LiabilityAccountQuery(
    string? Search,
    LiabilityAccountStatus? Status,
    LiabilityAccountSort SortBy = LiabilityAccountSort.Name,
    AssetSortDirection SortDirection = AssetSortDirection.Asc,
    int Page = 1,
    int PageSize = 25);

public record LiabilityAccountDto(
    Guid Id,
    LiabilityAccountCategory Category,
    string Name,
    string? Description,
    DateOnly CreationDate,
    LiabilityAccountStatus Status,
    string CurrencyCode,
    decimal StandingAmount,
    DateOnly? LastEntryDate);

public record SaveLiabilityAccountRequest(string Name, string? Description, DateOnly CreationDate);

public record LiabilityAccountStatusDto(Guid Id, LiabilityAccountStatus Status, DateOnly EffectiveDate);
public record ChangeLiabilityAccountStatusRequest(LiabilityAccountStatus Status, DateOnly EffectiveDate);

public record LiabilityAccountEntryDto(
    Guid Id,
    LiabilityAccountTxType Type,
    DateOnly Date,
    string? Note,
    decimal Amount,
    decimal RunningBalance);

public record SaveLiabilityAccountEntryRequest(
    LiabilityAccountTxType Type,
    DateOnly Date,
    string? Note,
    decimal Amount);

public record LiabilityStatisticsDto(string Metric, string CurrencyCode, IReadOnlyList<LiabilityStatisticsPoint> Points);
public record LiabilityStatisticsPoint(DateOnly Date, decimal Value);

public record LiabilityAccountExportRequest(
    IReadOnlyList<Guid> LiabilityAccountIds,
    DateOnly From,
    DateOnly To,
    ReportFormat Format);

public record LiabilityAccountExportRow(
    string Account,
    string RecordType,
    DateOnly Date,
    string Currency,
    string Details,
    decimal? Amount,
    decimal? RunningBalance);
