using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Application.Goals;
using PersonalAssistant.Domain.Goals;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.Goals;

public class GoalService : IGoalService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public GoalService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId ?? throw new InvalidOperationException("No authenticated user.");

    public async Task<IReadOnlyList<GoalPlanDto>> ListPlansAsync(CancellationToken ct)
    {
        var plans = await BasePlans().OrderBy(x => x.Name).ToListAsync(ct);
        return plans.Select(MapPlan).ToList();
    }

    public async Task<GoalPlanDto> GetPlanAsync(Guid id, CancellationToken ct)
    {
        var plan = await BasePlans().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Goal plan not found.");
        return MapPlan(plan);
    }

    public async Task<GoalPlanDto> CreatePlanAsync(GoalPlanRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Plan name is required.");
        var plan = new GoalPlan { OwnerUserId = OwnerId, Name = req.Name.Trim(), Description = req.Description?.Trim(), Tag = req.Tag?.Trim() };
        _db.GoalPlans.Add(plan);
        await _db.SaveChangesAsync(ct);
        return await GetPlanAsync(plan.Id, ct);
    }

    public async Task<GoalPlanDto> UpdatePlanAsync(Guid id, GoalPlanRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Plan name is required.");
        var plan = await _db.GoalPlans.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Goal plan not found.");
        plan.Name = req.Name.Trim(); plan.Description = req.Description?.Trim(); plan.Tag = req.Tag?.Trim();
        await _db.SaveChangesAsync(ct);
        return await GetPlanAsync(id, ct);
    }

    public async Task DeletePlanAsync(Guid id, CancellationToken ct)
    {
        var plan = await BasePlans().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Goal plan not found.");
        foreach (var goal in plan.Goals) _db.GoalSteps.RemoveRange(goal.Steps);
        _db.Goals.RemoveRange(plan.Goals);
        _db.GoalPlans.Remove(plan);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<GoalDto> CreateGoalAsync(Guid planId, GoalRequest req, CancellationToken ct)
    {
        ValidateGoal(req);
        var exists = await _db.GoalPlans.AnyAsync(x => x.Id == planId && x.OwnerUserId == OwnerId, ct);
        if (!exists) throw new KeyNotFoundException("Goal plan not found.");
        var goal = new Goal
        {
            OwnerUserId = OwnerId,
            GoalPlanId = planId,
            Name = req.Name.Trim(),
            Description = req.Description?.Trim(),
            Tag = req.Tag?.Trim(),
            StartDate = req.StartDate,
            Deadline = req.Deadline,
            AchievedDate = req.AchievedDate,
            Note = req.Note?.Trim()
        };
        _db.Goals.Add(goal);
        await _db.SaveChangesAsync(ct);
        return await GetGoalDtoAsync(goal.Id, ct);
    }

    public async Task<GoalDto> UpdateGoalAsync(Guid goalId, GoalRequest req, CancellationToken ct)
    {
        ValidateGoal(req);
        var goal = await _db.Goals.Include(x => x.Steps).FirstOrDefaultAsync(x => x.Id == goalId && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Goal not found.");
        goal.Name = req.Name.Trim(); goal.Description = req.Description?.Trim(); goal.Tag = req.Tag?.Trim();
        goal.StartDate = req.StartDate; goal.Deadline = req.Deadline; goal.AchievedDate = req.AchievedDate; goal.Note = req.Note?.Trim();
        await _db.SaveChangesAsync(ct);
        return MapGoal(goal);
    }

    public async Task DeleteGoalAsync(Guid goalId, CancellationToken ct)
    {
        var goal = await _db.Goals.Include(x => x.Steps).FirstOrDefaultAsync(x => x.Id == goalId && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Goal not found.");
        _db.GoalSteps.RemoveRange(goal.Steps);
        _db.Goals.Remove(goal);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<GoalStepDto> CreateStepAsync(Guid goalId, GoalStepRequest req, CancellationToken ct)
    {
        ValidateStep(req);
        var exists = await _db.Goals.AnyAsync(x => x.Id == goalId && x.OwnerUserId == OwnerId, ct);
        if (!exists) throw new KeyNotFoundException("Goal not found.");
        var step = new GoalStep { OwnerUserId = OwnerId, GoalId = goalId, Name = req.Name.Trim(), Description = req.Description?.Trim(), StartDate = req.StartDate, Deadline = req.Deadline, AchievedDate = req.AchievedDate, Note = req.Note?.Trim() };
        _db.GoalSteps.Add(step);
        await _db.SaveChangesAsync(ct);
        return MapStep(step);
    }

    public async Task<GoalStepDto> UpdateStepAsync(Guid stepId, GoalStepRequest req, CancellationToken ct)
    {
        ValidateStep(req);
        var step = await _db.GoalSteps.FirstOrDefaultAsync(x => x.Id == stepId && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Goal step not found.");
        step.Name = req.Name.Trim(); step.Description = req.Description?.Trim(); step.StartDate = req.StartDate; step.Deadline = req.Deadline; step.AchievedDate = req.AchievedDate; step.Note = req.Note?.Trim();
        await _db.SaveChangesAsync(ct);
        return MapStep(step);
    }

    public async Task DeleteStepAsync(Guid stepId, CancellationToken ct)
    {
        var step = await _db.GoalSteps.FirstOrDefaultAsync(x => x.Id == stepId && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Goal step not found.");
        _db.GoalSteps.Remove(step);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<GoalReport> GetReportAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (to < from) throw new ArgumentException("To date must be on/after From date.");
        var plans = await BasePlans().ToListAsync(ct);
        var rows = new List<GoalReportRow>();
        foreach (var plan in plans)
        {
            foreach (var goal in plan.Goals.Where(x => x.StartDate <= to && x.Deadline >= from))
            {
                rows.Add(new GoalReportRow(plan.Name, goal.Name, null, goal.StartDate, goal.Deadline, goal.AchievedDate, Status(goal.StartDate, goal.Deadline, goal.AchievedDate)));
                rows.AddRange(goal.Steps.Where(x => x.StartDate <= to && x.Deadline >= from)
                    .Select(step => new GoalReportRow(plan.Name, goal.Name, step.Name, step.StartDate, step.Deadline, step.AchievedDate, Status(step.StartDate, step.Deadline, step.AchievedDate))));
            }
        }
        return new GoalReport(from, to, rows.OrderBy(x => x.PlanName).ThenBy(x => x.GoalName).ThenBy(x => x.StepName).ToList());
    }

    private IQueryable<GoalPlan> BasePlans()
        => _db.GoalPlans.Include(x => x.Goals.OrderBy(g => g.Deadline)).ThenInclude(x => x.Steps.OrderBy(s => s.Deadline))
            .Where(x => x.OwnerUserId == OwnerId);

    private async Task<GoalDto> GetGoalDtoAsync(Guid id, CancellationToken ct)
    {
        var goal = await _db.Goals.Include(x => x.Steps).FirstAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct);
        return MapGoal(goal);
    }

    private static void ValidateGoal(GoalRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Goal name is required.");
        if (req.Deadline < req.StartDate) throw new ArgumentException("Deadline must be on/after start date.");
    }

    private static void ValidateStep(GoalStepRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Step name is required.");
        if (req.Deadline < req.StartDate) throw new ArgumentException("Deadline must be on/after start date.");
    }

    private static GoalPlanDto MapPlan(GoalPlan p)
    {
        var goals = p.Goals.OrderBy(x => x.Deadline).Select(MapGoal).ToList();
        return new GoalPlanDto(p.Id, p.Name, p.Description, p.Tag, goals.Count, goals.Count(x => x.AchievedDate.HasValue), goals);
    }

    private static GoalDto MapGoal(Goal g)
        => new(g.Id, g.Name, g.Description, g.Tag, g.StartDate, g.Deadline, g.AchievedDate, g.Note, Status(g.StartDate, g.Deadline, g.AchievedDate), g.Steps.OrderBy(x => x.Deadline).Select(MapStep).ToList());

    private static GoalStepDto MapStep(GoalStep s)
        => new(s.Id, s.Name, s.Description, s.StartDate, s.Deadline, s.AchievedDate, s.Note, Status(s.StartDate, s.Deadline, s.AchievedDate));

    private static string Status(DateOnly start, DateOnly deadline, DateOnly? achieved)
    {
        if (achieved.HasValue) return "Achieved";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (today < start) return "Not started";
        if (today > deadline) return "Overdue";
        return "Active";
    }
}
