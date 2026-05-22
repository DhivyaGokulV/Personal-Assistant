using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.Goals;

public class GoalPlan : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Tag { get; set; }
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
}

public class Goal : EntityBase
{
    public Guid GoalPlanId { get; set; }
    public GoalPlan? GoalPlan { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Tag { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly Deadline { get; set; }
    public DateOnly? AchievedDate { get; set; }
    public string? Note { get; set; }
    public ICollection<GoalStep> Steps { get; set; } = new List<GoalStep>();
}

public class GoalStep : EntityBase
{
    public Guid GoalId { get; set; }
    public Goal? Goal { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly Deadline { get; set; }
    public DateOnly? AchievedDate { get; set; }
    public string? Note { get; set; }
}
