using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Tasks.Periodic;

public class PeriodicTaskHistory : EntityBase
{
    public Guid PeriodicTaskId { get; set; }
    public PeriodicTask? PeriodicTask { get; set; }

    public DateOnly CompletedOn { get; set; }
    public string? Note { get; set; }
}
