using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.Tasks.Daily;

public record DailyTaskGroupDto(Guid Id, string Name, string? Description, int TaskCount);

public record CreateDailyGroupRequest(string Name, string? Description);
public record UpdateDailyGroupRequest(string Name, string? Description);

public record DailyTaskDto(Guid Id, Guid GroupId, string Title, string? Description, TaskActiveStatus Status);

public record CreateDailyTaskRequest(Guid GroupId, string Title, string? Description, TaskActiveStatus Status);
public record UpdateDailyTaskRequest(Guid GroupId, string Title, string? Description, TaskActiveStatus Status);

public record DailyCompletionDto(bool IsCompleted, string? Note);

public record UpsertCompletionRequest(bool IsCompleted, string? Note);

public record DailyTaskWithCompletionDto(
    Guid Id,
    string Title,
    string? Description,
    TaskActiveStatus Status,
    DailyCompletionDto? Completion);

public record DailyCounts(int Total, int Completed, int NotCompleted);

public record DailyGroupView(
    Guid Id,
    string Name,
    string? Description,
    DailyCounts Counts,
    IReadOnlyList<DailyTaskWithCompletionDto> Tasks);

public record DailyByDateView(
    DateOnly Date,
    DailyCounts Totals,
    IReadOnlyList<DailyGroupView> Groups);
