namespace PersonalAssistant.Application.Tasks.Daily;

public interface IDailyTaskService
{
    Task<IReadOnlyList<DailyTaskGroupDto>> GetGroupsAsync(CancellationToken ct);
    Task<DailyTaskGroupDto> CreateGroupAsync(CreateDailyGroupRequest req, CancellationToken ct);
    Task<DailyTaskGroupDto> UpdateGroupAsync(Guid id, UpdateDailyGroupRequest req, CancellationToken ct);
    Task DeleteGroupAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<DailyTaskDto>> GetTasksAsync(Guid? groupId, bool includeInactive, CancellationToken ct);
    Task<DailyTaskDto> CreateTaskAsync(CreateDailyTaskRequest req, CancellationToken ct);
    Task<DailyTaskDto> UpdateTaskAsync(Guid id, UpdateDailyTaskRequest req, CancellationToken ct);
    Task DeleteTaskAsync(Guid id, CancellationToken ct);

    Task<DailyByDateView> GetByDateAsync(DateOnly date, CancellationToken ct);
    Task<DailyCompletionDto> UpsertCompletionAsync(Guid taskId, DateOnly date, UpsertCompletionRequest req, CancellationToken ct);

    Task<DailyDayWiseReport> GetDayWiseReportAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<DailyTaskWiseReport> GetTaskWiseReportAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
