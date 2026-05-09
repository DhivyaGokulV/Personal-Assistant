using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Tasks.Daily;

public class DailyTaskCompletion : EntityBase
{
    public Guid DailyTaskId { get; set; }
    public DailyTask? DailyTask { get; set; }

    public DateOnly Date { get; set; }
    public bool IsCompleted { get; set; }
    public string? Note { get; set; }
}
