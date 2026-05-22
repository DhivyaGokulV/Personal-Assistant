using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.TimeTracker;

public class TimeEntry : EntityBase
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Activity { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? Tag { get; set; }
}
