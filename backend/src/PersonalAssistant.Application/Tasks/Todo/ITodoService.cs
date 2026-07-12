using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.Tasks.Todo;

public interface ITodoService
{
    Task<IReadOnlyList<TodoDto>> GetAllAsync(TodoStatus? status, TodoSort sortBy, SortOrder order, CancellationToken ct);
    Task<TodoDto> GetAsync(Guid id, CancellationToken ct);
    Task<TodoDto> CreateAsync(CreateTodoRequest req, CancellationToken ct);
    Task<TodoDto> UpdateAsync(Guid id, UpdateTodoRequest req, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<TodoSummary> GetSummaryAsync(CancellationToken ct);
    Task<TodoReport> GetReportAsync(DateOnly asOf, CancellationToken ct);
}
