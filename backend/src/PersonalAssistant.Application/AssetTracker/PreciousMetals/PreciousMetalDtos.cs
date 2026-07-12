using PersonalAssistant.Application.Common.Reports;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.AssetTracker.PreciousMetals;

public record AssetTrackerPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public enum AssetSortDirection { Asc, Desc }
public enum PreciousMetalSort { Name, CreationDate, Quantity, CurrentValue }
public enum PreciousMetalStatisticsSource { Entries, Prices }
public enum PreciousMetalStatisticsDuration { OneMonth, ThreeMonths, SixMonths, OneYear, ThreeYears, FiveYears }

public record PreciousMetalQuery(
    string? Search,
    PreciousMetalSort SortBy = PreciousMetalSort.Name,
    AssetSortDirection SortDirection = AssetSortDirection.Asc,
    int Page = 1,
    int PageSize = 25);

public record PreciousMetalDto(
    Guid Id,
    string Name,
    string? Description,
    DateOnly CreationDate,
    string CurrencyCode,
    bool IsDefault,
    decimal Quantity,
    decimal? CurrentPrice,
    decimal CurrentValue);

public record SavePreciousMetalRequest(string Name, string? Description, DateOnly? CreationDate);

public record PreciousMetalEntryDto(
    Guid Id,
    PreciousMetalTxType Type,
    DateOnly Date,
    string? Note,
    decimal Quantity,
    decimal PricePerUnit,
    decimal Amount);

public record SavePreciousMetalEntryRequest(
    PreciousMetalTxType Type,
    DateOnly Date,
    string? Note,
    decimal Quantity,
    decimal PricePerUnit);

public record PreciousMetalPriceDto(Guid Id, DateOnly Date, decimal PricePerUnit);
public record SavePreciousMetalPriceRequest(DateOnly Date, decimal PricePerUnit);

public record PreciousMetalStatisticsDto(string Metric, string CurrencyCode, IReadOnlyList<PreciousMetalStatisticsPoint> Points);
public record PreciousMetalStatisticsPoint(DateOnly Date, decimal Value);

public record PreciousMetalExportRequest(
    IReadOnlyList<Guid> PreciousMetalIds,
    DateOnly From,
    DateOnly To,
    ReportFormat Format);

public record PreciousMetalExportRow(
    string Metal,
    string RecordType,
    DateOnly Date,
    string Currency,
    string Details,
    decimal? Quantity,
    decimal? UnitPrice,
    decimal? Amount);
