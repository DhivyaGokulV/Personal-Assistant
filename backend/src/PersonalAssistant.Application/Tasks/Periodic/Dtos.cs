using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.Tasks.Periodic;

public record PeriodicGroupDto(Guid Id, string Name, string? Description, int TaskCount);

public record CreatePeriodicGroupRequest(string Name, string? Description);
public record UpdatePeriodicGroupRequest(string Name, string? Description);

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
    int HistoryCount);

public record CreatePeriodicTaskRequest(
    Guid GroupId,
    string Title,
    string? Description,
    TaskActiveStatus Status,
    int FrequencyValue,
    FrequencyUnit FrequencyUnit);

public record UpdatePeriodicTaskRequest(
    Guid GroupId,
    string Title,
    string? Description,
    TaskActiveStatus Status,
    int FrequencyValue,
    FrequencyUnit FrequencyUnit);

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
    DateOnly? NextDueOn);

public record PeriodicReport(DateOnly From, DateOnly To, IReadOnlyList<PeriodicReportRow> Rows);
