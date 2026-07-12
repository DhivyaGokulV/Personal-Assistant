namespace PersonalAssistant.Application.Tasks.Daily;

public record DailyDayWiseRow(
    DateOnly Date,
    string GroupName,
    string TaskTitle,
    bool IsCompleted,
    string? Note);

public record DailyTaskWiseRow(
    string GroupName,
    string TaskTitle,
    int TimesDone,
    DateOnly? LastDoneOn);

public record DailyDayWiseReport(DateOnly From, DateOnly To, IReadOnlyList<DailyDayWiseRow> Rows);
public record DailyTaskWiseReport(DateOnly From, DateOnly To, IReadOnlyList<DailyTaskWiseRow> Rows);

public record DailyConsolidatedReport(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<DateOnly> Dates,
    IReadOnlyList<DailyConsolidatedTaskRow> Rows,
    IReadOnlyList<DailyConsolidatedTotalRow> Totals);

public record DailyConsolidatedTaskRow(
    string TaskGroup,
    string TaskName,
    int DaysCompleted,
    int DaysInPeriod,
    IReadOnlyDictionary<DateOnly, string> StatusByDate);

public record DailyConsolidatedTotalRow(
    DateOnly Date,
    int TasksCompleted,
    int TasksActive);
