using PersonalAssistant.Domain.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Domain.Tasks.Daily;

public class DailyTask : EntityBase
{
    public Guid GroupId { get; set; }
    public DailyTaskGroup? Group { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskActiveStatus Status { get; set; } = TaskActiveStatus.Active;

    public ICollection<DailyTaskCompletion> Completions { get; set; } = new List<DailyTaskCompletion>();
}
