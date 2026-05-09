using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Finance;

public class Budget : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public decimal Amount { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string? Note { get; set; }
}
