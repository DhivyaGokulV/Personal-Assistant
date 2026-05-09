namespace PersonalAssistant.Application.AssetTracker.Dashboard;

public record SliceDto(string Key, decimal Value, decimal PercentOfTotal);

public record TimeSeriesPoint(DateOnly Date, decimal Value);

public record AssetTrackerDashboardView(
    decimal NetWorth,
    decimal TotalAssets,
    decimal TotalInvestments,
    decimal TotalLiabilities,
    IReadOnlyList<SliceDto> AssetsBreakdown,
    IReadOnlyList<SliceDto> InvestmentsBreakdown,
    IReadOnlyList<SliceDto> LiabilitiesBreakdown,
    IReadOnlyList<TimeSeriesPoint> AssetsSeries,
    IReadOnlyList<TimeSeriesPoint> InvestmentsSeries,
    IReadOnlyList<TimeSeriesPoint> LiabilitiesSeries,
    IReadOnlyList<TimeSeriesPoint> NetWorthSeries);
