namespace PersonalAssistant.Application.Tasks.Periodic;

public interface IPeriodicTaskService
{
    Task<IReadOnlyList<PeriodicGroupDto>> GetGroupsAsync(CancellationToken ct);
    Task<PeriodicGroupDto> CreateGroupAsync(CreatePeriodicGroupRequest req, CancellationToken ct);
    Task<PeriodicGroupDto> UpdateGroupAsync(Guid id, UpdatePeriodicGroupRequest req, CancellationToken ct);
    Task DeleteGroupAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<PeriodicTaskDto>> GetTasksAsync(Guid? groupId, bool includeInactive, CancellationToken ct);
    Task<PeriodicTaskWithHistoryDto> GetTaskAsync(Guid id, CancellationToken ct);
    Task<PeriodicTaskDto> CreateTaskAsync(CreatePeriodicTaskRequest req, CancellationToken ct);
    Task<PeriodicTaskDto> UpdateTaskAsync(Guid id, UpdatePeriodicTaskRequest req, CancellationToken ct);
    Task DeleteTaskAsync(Guid id, CancellationToken ct);

    Task<PeriodicHistoryDto> AddHistoryAsync(Guid taskId, AddHistoryRequest req, CancellationToken ct);
    Task<PeriodicHistoryDto> UpdateHistoryAsync(Guid taskId, Guid historyId, UpdateHistoryRequest req, CancellationToken ct);
    Task DeleteHistoryAsync(Guid taskId, Guid historyId, CancellationToken ct);

    Task<PeriodicReport> GetReportAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
