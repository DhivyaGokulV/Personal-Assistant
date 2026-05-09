using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Tasks.Periodic;

public class PeriodicTaskGroup : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<PeriodicTask> Tasks { get; set; } = new List<PeriodicTask>();
}
