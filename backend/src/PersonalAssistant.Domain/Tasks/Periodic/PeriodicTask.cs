using PersonalAssistant.Domain.Common;
using PersonalAssistant.Domain.Enums;

namespace PersonalAssistant.Domain.Tasks.Periodic;

public class PeriodicTask : EntityBase
{
    public Guid GroupId { get; set; }
    public PeriodicTaskGroup? Group { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskActiveStatus Status { get; set; } = TaskActiveStatus.Active;
    public int DisplayOrder { get; set; }

    public int FrequencyValue { get; set; } = 1;
    public FrequencyUnit FrequencyUnit { get; set; } = FrequencyUnit.Days;

    public DateOnly? LastDoneOn { get; set; }

    public ICollection<PeriodicTaskHistory> History { get; set; } = new List<PeriodicTaskHistory>();
}
