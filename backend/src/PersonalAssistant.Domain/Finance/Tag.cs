using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Finance;

public class Tag : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#6c5ce7";
}
