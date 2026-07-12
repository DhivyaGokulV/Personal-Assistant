using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.Tasks.Daily;

public record DailyTaskGroupDto(Guid Id, string Name, string? Description, int TaskCount, int DisplayOrder = 0);

public record CreateDailyGroupRequest(string Name, string? Description, int? DisplayOrder = null);
public record UpdateDailyGroupRequest(string Name, string? Description, int? DisplayOrder = null);

public record DailyTaskDto(Guid Id, Guid GroupId, string Title, string? Description, TaskActiveStatus Status, int DisplayOrder = 0);

public record CreateDailyTaskRequest(Guid GroupId, string Title, string? Description, TaskActiveStatus Status, int? DisplayOrder = null);
public record UpdateDailyTaskRequest(Guid GroupId, string Title, string? Description, TaskActiveStatus Status, int? DisplayOrder = null);

public record ReorderDailyGroupRequest(Guid GroupId, int DisplayOrder);
public record ReorderDailyTaskRequest(Guid TaskId, Guid GroupId, int DisplayOrder);
public record ConfirmDeleteTaskRequest(string ConfirmationTitle);

public record DailyCompletionDto(bool IsCompleted, string? Note);

public record UpsertCompletionRequest(bool IsCompleted, string? Note);

public record DailyTaskWithCompletionDto(
    Guid Id,
    string Title,
    string? Description,
    TaskActiveStatus Status,
    DailyCompletionDto? Completion,
    int DisplayOrder = 0);

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

public record TaskArchiveDto(
    Guid Id,
    string Module,
    string EntityType,
    Guid EntityId,
    string ActivityType,
    string? OldValue,
    string? NewValue,
    DateTime ActionDate);
