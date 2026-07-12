using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Tasks;

public class TaskArchiveEntry : EntityBase
{
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ActionDate { get; set; }
}
