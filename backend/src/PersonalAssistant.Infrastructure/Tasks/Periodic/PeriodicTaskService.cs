using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Application.Tasks.Periodic;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Domain.Tasks.Periodic;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.Tasks.Periodic;

public class PeriodicTaskService : IPeriodicTaskService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public PeriodicTaskService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId
        ?? throw new InvalidOperationException("No authenticated user.");

    public async Task<IReadOnlyList<PeriodicGroupDto>> GetGroupsAsync(CancellationToken ct)
    {
        var owner = OwnerId;
        return await _db.PeriodicTaskGroups
            .Where(g => g.OwnerUserId == owner)
            .OrderBy(g => g.Name)
            .Select(g => new PeriodicGroupDto(
                g.Id, g.Name, g.Description,
                g.Tasks.Count(t => t.Status == TaskActiveStatus.Active)))
            .ToListAsync(ct);
    }

    public async Task<PeriodicGroupDto> CreateGroupAsync(CreatePeriodicGroupRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = new PeriodicTaskGroup
        {
            OwnerUserId = owner,
            Name = req.Name.Trim(),
            Description = req.Description?.Trim()
        };
        _db.PeriodicTaskGroups.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new PeriodicGroupDto(entity.Id, entity.Name, entity.Description, 0);
    }

    public async Task<PeriodicGroupDto> UpdateGroupAsync(Guid id, UpdatePeriodicGroupRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.PeriodicTaskGroups
            .FirstOrDefaultAsync(g => g.Id == id && g.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Group not found.");

        entity.Name = req.Name.Trim();
        entity.Description = req.Description?.Trim();
        await _db.SaveChangesAsync(ct);

        var taskCount = await _db.PeriodicTasks
            .CountAsync(t => t.GroupId == id && t.Status == TaskActiveStatus.Active, ct);
        return new PeriodicGroupDto(entity.Id, entity.Name, entity.Description, taskCount);
    }

    public async Task DeleteGroupAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.PeriodicTaskGroups
            .FirstOrDefaultAsync(g => g.Id == id && g.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Group not found.");

        var tasks = await _db.PeriodicTasks.Where(t => t.GroupId == id).ToListAsync(ct);
        _db.PeriodicTasks.RemoveRange(tasks);
        _db.PeriodicTaskGroups.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PeriodicTaskDto>> GetTasksAsync(Guid? groupId, bool includeInactive, CancellationToken ct)
    {
        var owner = OwnerId;
        var query = _db.PeriodicTasks
            .Include(t => t.Group)
            .Where(t => t.OwnerUserId == owner);
        if (groupId.HasValue) query = query.Where(t => t.GroupId == groupId.Value);
        if (!includeInactive) query = query.Where(t => t.Status == TaskActiveStatus.Active);

        var tasks = await query
            .OrderBy(t => t.Group!.Name).ThenBy(t => t.Title)
            .Select(t => new
            {
                t.Id, t.GroupId, GroupName = t.Group!.Name, t.Title, t.Description,
                t.Status, t.FrequencyValue, t.FrequencyUnit, t.LastDoneOn,
                HistoryCount = t.History.Count()
            })
            .ToListAsync(ct);

        return tasks.Select(t => new PeriodicTaskDto(
            t.Id, t.GroupId, t.GroupName, t.Title, t.Description, t.Status,
            t.FrequencyValue, t.FrequencyUnit, t.LastDoneOn,
            ComputeNextDue(t.LastDoneOn, t.FrequencyValue, t.FrequencyUnit),
            t.HistoryCount)).ToList();
    }

    public async Task<PeriodicTaskWithHistoryDto> GetTaskAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.PeriodicTasks
            .Include(t => t.Group)
            .Include(t => t.History.OrderByDescending(h => h.CompletedOn))
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Task not found.");

        var dto = new PeriodicTaskDto(
            entity.Id, entity.GroupId, entity.Group!.Name, entity.Title, entity.Description,
            entity.Status, entity.FrequencyValue, entity.FrequencyUnit,
            entity.LastDoneOn,
            ComputeNextDue(entity.LastDoneOn, entity.FrequencyValue, entity.FrequencyUnit),
            entity.History.Count);

        var history = entity.History
            .Select(h => new PeriodicHistoryDto(h.Id, h.CompletedOn, h.Note))
            .ToList();

        return new PeriodicTaskWithHistoryDto(dto, history);
    }

    public async Task<PeriodicTaskDto> CreateTaskAsync(CreatePeriodicTaskRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var group = await _db.PeriodicTaskGroups
            .FirstOrDefaultAsync(g => g.Id == req.GroupId && g.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Group not found.");

        if (req.FrequencyValue <= 0) throw new ArgumentException("Frequency must be positive.");

        var entity = new PeriodicTask
        {
            OwnerUserId = owner,
            GroupId = req.GroupId,
            Title = req.Title.Trim(),
            Description = req.Description?.Trim(),
            Status = req.Status,
            FrequencyValue = req.FrequencyValue,
            FrequencyUnit = req.FrequencyUnit
        };
        _db.PeriodicTasks.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new PeriodicTaskDto(
            entity.Id, entity.GroupId, group.Name, entity.Title, entity.Description, entity.Status,
            entity.FrequencyValue, entity.FrequencyUnit, null, null, 0);
    }

    public async Task<PeriodicTaskDto> UpdateTaskAsync(Guid id, UpdatePeriodicTaskRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.PeriodicTasks
            .Include(t => t.Group)
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Task not found.");

        if (req.FrequencyValue <= 0) throw new ArgumentException("Frequency must be positive.");

        if (entity.GroupId != req.GroupId)
        {
            var group = await _db.PeriodicTaskGroups
                .FirstOrDefaultAsync(g => g.Id == req.GroupId && g.OwnerUserId == owner, ct)
                ?? throw new KeyNotFoundException("Group not found.");
            entity.GroupId = req.GroupId;
            entity.Group = group;
        }

        entity.Title = req.Title.Trim();
        entity.Description = req.Description?.Trim();
        entity.Status = req.Status;
        entity.FrequencyValue = req.FrequencyValue;
        entity.FrequencyUnit = req.FrequencyUnit;
        await _db.SaveChangesAsync(ct);

        var historyCount = await _db.PeriodicTaskHistory.CountAsync(h => h.PeriodicTaskId == id, ct);
        return new PeriodicTaskDto(
            entity.Id, entity.GroupId, entity.Group!.Name, entity.Title, entity.Description, entity.Status,
            entity.FrequencyValue, entity.FrequencyUnit, entity.LastDoneOn,
            ComputeNextDue(entity.LastDoneOn, entity.FrequencyValue, entity.FrequencyUnit),
            historyCount);
    }

    public async Task DeleteTaskAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.PeriodicTasks
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Task not found.");
        _db.PeriodicTasks.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PeriodicHistoryDto> AddHistoryAsync(Guid taskId, AddHistoryRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var task = await _db.PeriodicTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Task not found.");

        var entity = new PeriodicTaskHistory
        {
            OwnerUserId = owner,
            PeriodicTaskId = taskId,
            CompletedOn = req.CompletedOn,
            Note = req.Note?.Trim()
        };
        _db.PeriodicTaskHistory.Add(entity);
        await _db.SaveChangesAsync(ct);

        await RecomputeLastDoneOnAsync(taskId, ct);
        return new PeriodicHistoryDto(entity.Id, entity.CompletedOn, entity.Note);
    }

    public async Task<PeriodicHistoryDto> UpdateHistoryAsync(Guid taskId, Guid historyId, UpdateHistoryRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.PeriodicTaskHistory
            .FirstOrDefaultAsync(h => h.Id == historyId && h.PeriodicTaskId == taskId && h.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("History entry not found.");

        entity.CompletedOn = req.CompletedOn;
        entity.Note = req.Note?.Trim();
        await _db.SaveChangesAsync(ct);

        await RecomputeLastDoneOnAsync(taskId, ct);
        return new PeriodicHistoryDto(entity.Id, entity.CompletedOn, entity.Note);
    }

    public async Task DeleteHistoryAsync(Guid taskId, Guid historyId, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.PeriodicTaskHistory
            .FirstOrDefaultAsync(h => h.Id == historyId && h.PeriodicTaskId == taskId && h.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("History entry not found.");

        _db.PeriodicTaskHistory.Remove(entity);
        await _db.SaveChangesAsync(ct);

        await RecomputeLastDoneOnAsync(taskId, ct);
    }

    public async Task<PeriodicReport> GetReportAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (to < from) (from, to) = (to, from);
        var owner = OwnerId;

        var rows = await _db.PeriodicTasks
            .Include(t => t.Group)
            .Where(t => t.OwnerUserId == owner && t.Status == TaskActiveStatus.Active)
            .OrderBy(t => t.Group!.Name).ThenBy(t => t.Title)
            .Select(t => new
            {
                GroupName = t.Group!.Name,
                t.Title,
                TimesDoneInRange = t.History.Count(h => h.CompletedOn >= from && h.CompletedOn <= to),
                t.LastDoneOn,
                t.FrequencyValue,
                t.FrequencyUnit
            })
            .ToListAsync(ct);

        var reportRows = rows.Select(r => new PeriodicReportRow(
            r.GroupName, r.Title, r.TimesDoneInRange, r.LastDoneOn,
            ComputeNextDue(r.LastDoneOn, r.FrequencyValue, r.FrequencyUnit))).ToList();

        return new PeriodicReport(from, to, reportRows);
    }

    private async Task RecomputeLastDoneOnAsync(Guid taskId, CancellationToken ct)
    {
        var task = await _db.PeriodicTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null) return;

        var max = await _db.PeriodicTaskHistory
            .Where(h => h.PeriodicTaskId == taskId)
            .MaxAsync(h => (DateOnly?)h.CompletedOn, ct);

        task.LastDoneOn = max;
        await _db.SaveChangesAsync(ct);
    }

    private static DateOnly? ComputeNextDue(DateOnly? lastDoneOn, int value, FrequencyUnit unit)
    {
        if (lastDoneOn is null) return null;
        var d = lastDoneOn.Value;
        return unit switch
        {
            FrequencyUnit.Days => d.AddDays(value),
            FrequencyUnit.Weeks => d.AddDays(value * 7),
            FrequencyUnit.Months => d.AddMonths(value),
            FrequencyUnit.Years => d.AddYears(value),
            _ => null
        };
    }
}
