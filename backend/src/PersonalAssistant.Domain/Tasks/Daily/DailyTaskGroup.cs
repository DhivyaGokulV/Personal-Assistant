using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Tasks.Daily;

public class DailyTaskGroup : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<DailyTask> Tasks { get; set; } = new List<DailyTask>();
}
