using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.Tasks.Periodic;

public record PeriodicGroupDto(Guid Id, string Name, string? Description, int TaskCount, int DisplayOrder = 0);

public record CreatePeriodicGroupRequest(string Name, string? Description, int? DisplayOrder = null);
public record UpdatePeriodicGroupRequest(string Name, string? Description, int? DisplayOrder = null);

public record PeriodicTaskDto(
    Guid Id,
    Guid GroupId,
    string GroupName,
    string Title,
    string? Description,
    TaskActiveStatus Status,
    int FrequencyValue,
    FrequencyUnit FrequencyUnit,
    DateOnly? LastDoneOn,
    DateOnly? NextDueOn,
    int HistoryCount,
    int DisplayOrder = 0,
    string DueStatus = "");

public record CreatePeriodicTaskRequest(
    Guid GroupId,
    string Title,
    string? Description,
    TaskActiveStatus Status,
    int FrequencyValue,
    FrequencyUnit FrequencyUnit,
    DateOnly? LastDoneOn = null,
    int? DisplayOrder = null);

public record UpdatePeriodicTaskRequest(
    Guid GroupId,
    string Title,
    string? Description,
    TaskActiveStatus Status,
    int FrequencyValue,
    FrequencyUnit FrequencyUnit,
    DateOnly? LastDoneOn = null,
    int? DisplayOrder = null);

public record ReorderPeriodicGroupRequest(Guid GroupId, int DisplayOrder);
public record ReorderPeriodicTaskRequest(Guid TaskId, Guid GroupId, int DisplayOrder);
public record ConfirmDeletePeriodicTaskRequest(string ConfirmationTitle);

public record PeriodicHistoryDto(Guid Id, DateOnly CompletedOn, string? Note);

public record AddHistoryRequest(DateOnly CompletedOn, string? Note);
public record UpdateHistoryRequest(DateOnly CompletedOn, string? Note);

public record PeriodicTaskWithHistoryDto(
    PeriodicTaskDto Task,
    IReadOnlyList<PeriodicHistoryDto> History);

public record PeriodicReportRow(
    string GroupName,
    string TaskTitle,
    int TimesDoneInRange,
    DateOnly? LastDoneOn,
    DateOnly? NextDueOn,
    string TaskHistory);

public record PeriodicReport(DateOnly From, DateOnly To, IReadOnlyList<PeriodicReportRow> Rows);
