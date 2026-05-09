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
