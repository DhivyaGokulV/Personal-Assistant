namespace PersonalAssistant.Application.TimeTracker;

public record TimeEntryDto(Guid Id, DateTime StartTime, DateTime EndTime, string Activity, string? Note, string? Tag, int Minutes);

public record TimeEntryRequest(DateTime StartTime, DateTime EndTime, string Activity, string? Note, string? Tag);

public record TimeEntryFilters(DateTime? From, DateTime? To, string? Activity, string? Tag);

public record TimePagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public record TimeReportRow(DateTime StartTime, DateTime EndTime, string Activity, string? Tag, int Minutes, string? Note);

public record TimeSummaryRow(string Key, int NumberOfDays, int NumberOfMinutes);

public record TimeReport(DateTime From, DateTime To, IReadOnlyList<TimeReportRow> Rows, IReadOnlyList<TimeSummaryRow> ActivitySummary, IReadOnlyList<TimeSummaryRow> TagSummary);

public interface ITimeTrackerService
{
    Task<TimePagedResult<TimeEntryDto>> ListAsync(TimeEntryFilters filters, int page, int pageSize, CancellationToken ct);
    Task<TimeEntryDto> GetAsync(Guid id, CancellationToken ct);
    Task<TimeEntryDto> CreateAsync(TimeEntryRequest req, CancellationToken ct);
    Task<TimeEntryDto> UpdateAsync(Guid id, TimeEntryRequest req, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<TimeReport> GetReportAsync(TimeEntryFilters filters, CancellationToken ct);
}
