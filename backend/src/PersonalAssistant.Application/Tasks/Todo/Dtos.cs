using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Application.Tasks.Todo;

public record TodoDto(
    Guid Id,
    string Title,
    string? Description,
    DateOnly AddedDate,
    DateOnly? Deadline,
    int? DaysLeft,
    TodoStatus Status,
    DateOnly? CompletedOn,
    string? CompletionNote);

public record CreateTodoRequest(
    string Title,
    string? Description,
    DateOnly? Deadline,
    TodoStatus Status);

public record UpdateTodoRequest(
    string Title,
    string? Description,
    DateOnly? Deadline,
    TodoStatus Status,
    DateOnly? CompletedOn,
    string? CompletionNote);

public record TodoStatusCount(TodoStatus Status, int Count);
public record TodoSummary(int Total, IReadOnlyList<TodoStatusCount> ByStatus);

public record TodoReportRow(
    string Title,
    DateOnly AddedDate,
    DateOnly? Deadline,
    int? DaysLeft,
    TodoStatus Status,
    DateOnly? CompletedOn,
    string? CompletionNote);

public record TodoReport(DateOnly From, DateOnly To, IReadOnlyList<TodoReportRow> Rows);

public enum TodoSort
{
    AddedDate = 1,
    Deadline = 2,
    Status = 3,
    DaysLeft = 4
}

public enum SortOrder
{
    Asc = 1,
    Desc = 2
}
