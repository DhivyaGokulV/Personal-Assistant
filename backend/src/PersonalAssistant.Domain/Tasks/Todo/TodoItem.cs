using PersonalAssistant.Domain.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Domain.Tasks.Todo;

public class TodoItem : EntityBase
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly AddedDate { get; set; }
    public DateOnly? Deadline { get; set; }
    public TodoStatus Status { get; set; } = TodoStatus.NotStartedYet;
    public DateOnly? CompletedOn { get; set; }
    public string? CompletionNote { get; set; }
}
