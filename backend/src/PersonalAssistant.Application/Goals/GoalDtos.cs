namespace PersonalAssistant.Application.Goals;

public record GoalStepDto(Guid Id, string Name, string? Description, DateOnly StartDate, DateOnly Deadline, DateOnly? AchievedDate, string? Note, string Status);
public record GoalStepRequest(string Name, string? Description, DateOnly StartDate, DateOnly Deadline, DateOnly? AchievedDate, string? Note);

public record GoalDto(Guid Id, string Name, string? Description, string? Tag, DateOnly StartDate, DateOnly Deadline, DateOnly? AchievedDate, string? Note, string Status, IReadOnlyList<GoalStepDto> Steps);
public record GoalRequest(string Name, string? Description, string? Tag, DateOnly StartDate, DateOnly Deadline, DateOnly? AchievedDate, string? Note);

public record GoalPlanDto(Guid Id, string Name, string? Description, string? Tag, int GoalCount, int AchievedGoalCount, IReadOnlyList<GoalDto> Goals);
public record GoalPlanRequest(string Name, string? Description, string? Tag);

public record GoalReportRow(string PlanName, string GoalName, string? StepName, DateOnly StartDate, DateOnly Deadline, DateOnly? AchievedDate, string Status);
public record GoalReport(DateOnly From, DateOnly To, IReadOnlyList<GoalReportRow> Rows);

public interface IGoalService
{
    Task<IReadOnlyList<GoalPlanDto>> ListPlansAsync(CancellationToken ct);
    Task<GoalPlanDto> GetPlanAsync(Guid id, CancellationToken ct);
    Task<GoalPlanDto> CreatePlanAsync(GoalPlanRequest req, CancellationToken ct);
    Task<GoalPlanDto> UpdatePlanAsync(Guid id, GoalPlanRequest req, CancellationToken ct);
    Task DeletePlanAsync(Guid id, CancellationToken ct);

    Task<GoalDto> CreateGoalAsync(Guid planId, GoalRequest req, CancellationToken ct);
    Task<GoalDto> UpdateGoalAsync(Guid goalId, GoalRequest req, CancellationToken ct);
    Task DeleteGoalAsync(Guid goalId, CancellationToken ct);

    Task<GoalStepDto> CreateStepAsync(Guid goalId, GoalStepRequest req, CancellationToken ct);
    Task<GoalStepDto> UpdateStepAsync(Guid stepId, GoalStepRequest req, CancellationToken ct);
    Task DeleteStepAsync(Guid stepId, CancellationToken ct);

    Task<GoalReport> GetReportAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
