using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Application.Tasks.Periodic;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Domain.Tasks;
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
            .OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name)
            .Select(g => new PeriodicGroupDto(
                g.Id, g.Name, g.Description,
                g.Tasks.Count(t => t.Status == TaskActiveStatus.Active),
                g.DisplayOrder))
            .ToListAsync(ct);
    }

    public async Task<PeriodicGroupDto> CreateGroupAsync(CreatePeriodicGroupRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = new PeriodicTaskGroup
        {
            OwnerUserId = owner,
            Name = req.Name.Trim(),
            Description = req.Description?.Trim(),
            DisplayOrder = req.DisplayOrder ?? await NextGroupOrderAsync(ct)
        };
        _db.PeriodicTaskGroups.Add(entity);
        await _db.SaveChangesAsync(ct);
        await ArchiveAsync("Periodic", "TaskGroup", entity.Id, "insert", null, Snapshot(entity), ct);
        return new PeriodicGroupDto(entity.Id, entity.Name, entity.Description, 0, entity.DisplayOrder);
    }

    public async Task<PeriodicGroupDto> UpdateGroupAsync(Guid id, UpdatePeriodicGroupRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.PeriodicTaskGroups
            .FirstOrDefaultAsync(g => g.Id == id && g.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Group not found.");

        var old = Snapshot(entity);
        entity.Name = req.Name.Trim();
        entity.Description = req.Description?.Trim();
        if (req.DisplayOrder.HasValue) entity.DisplayOrder = req.DisplayOrder.Value;
        await _db.SaveChangesAsync(ct);
        await ArchiveAsync("Periodic", "TaskGroup", entity.Id, "update", old, Snapshot(entity), ct);

        var taskCount = await _db.PeriodicTasks
            .CountAsync(t => t.GroupId == id && t.Status == TaskActiveStatus.Active, ct);
        return new PeriodicGroupDto(entity.Id, entity.Name, entity.Description, taskCount, entity.DisplayOrder);
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

    public async Task ReorderGroupsAsync(IReadOnlyList<ReorderPeriodicGroupRequest> req, CancellationToken ct)
    {
        var owner = OwnerId;
        var ids = req.Select(x => x.GroupId).ToHashSet();
        var groups = await _db.PeriodicTaskGroups.Where(x => x.OwnerUserId == owner && ids.Contains(x.Id)).ToListAsync(ct);
        foreach (var item in req)
        {
            var group = groups.FirstOrDefault(x => x.Id == item.GroupId);
            if (group is not null) group.DisplayOrder = item.DisplayOrder;
        }
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
            .OrderBy(t => t.Group!.DisplayOrder).ThenBy(t => t.Group!.Name).ThenBy(t => t.DisplayOrder).ThenBy(t => t.Title)
            .Select(t => new
            {
                t.Id, t.GroupId, GroupName = t.Group!.Name, t.Title, t.Description,
                t.Status, t.FrequencyValue, t.FrequencyUnit, t.LastDoneOn, t.DisplayOrder,
                HistoryCount = t.History.Count()
            })
            .ToListAsync(ct);

        return tasks.Select(t => new PeriodicTaskDto(
            t.Id, t.GroupId, t.GroupName, t.Title, t.Description, t.Status,
            t.FrequencyValue, t.FrequencyUnit, t.LastDoneOn,
            ComputeNextDue(t.LastDoneOn, t.FrequencyValue, t.FrequencyUnit),
            t.HistoryCount, t.DisplayOrder,
            DueStatus(t.LastDoneOn, t.FrequencyValue, t.FrequencyUnit, t.HistoryCount))).ToList();
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
            entity.History.Count, entity.DisplayOrder,
            DueStatus(entity.LastDoneOn, entity.FrequencyValue, entity.FrequencyUnit, entity.History.Count));

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
            FrequencyUnit = req.FrequencyUnit,
            LastDoneOn = req.LastDoneOn,
            DisplayOrder = req.DisplayOrder ?? await NextTaskOrderAsync(req.GroupId, ct)
        };
        _db.PeriodicTasks.Add(entity);
        await _db.SaveChangesAsync(ct);
        await ArchiveAsync("Periodic", "Task", entity.Id, "insert", null, Snapshot(entity), ct);

        return new PeriodicTaskDto(
            entity.Id, entity.GroupId, group.Name, entity.Title, entity.Description, entity.Status,
            entity.FrequencyValue, entity.FrequencyUnit, entity.LastDoneOn,
            ComputeNextDue(entity.LastDoneOn, entity.FrequencyValue, entity.FrequencyUnit),
            0, entity.DisplayOrder,
            DueStatus(entity.LastDoneOn, entity.FrequencyValue, entity.FrequencyUnit, 0));
    }

    public async Task<PeriodicTaskDto> UpdateTaskAsync(Guid id, UpdatePeriodicTaskRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.PeriodicTasks
            .Include(t => t.Group)
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Task not found.");

        if (req.FrequencyValue <= 0) throw new ArgumentException("Frequency must be positive.");

        var old = Snapshot(entity);
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
        entity.LastDoneOn = req.LastDoneOn ?? entity.LastDoneOn;
        if (req.DisplayOrder.HasValue) entity.DisplayOrder = req.DisplayOrder.Value;
        await _db.SaveChangesAsync(ct);
        await ArchiveAsync("Periodic", "Task", entity.Id, "update", old, Snapshot(entity), ct);

        var historyCount = await _db.PeriodicTaskHistory.CountAsync(h => h.PeriodicTaskId == id, ct);
        return new PeriodicTaskDto(
            entity.Id, entity.GroupId, entity.Group!.Name, entity.Title, entity.Description, entity.Status,
            entity.FrequencyValue, entity.FrequencyUnit, entity.LastDoneOn,
            ComputeNextDue(entity.LastDoneOn, entity.FrequencyValue, entity.FrequencyUnit),
            historyCount, entity.DisplayOrder,
            DueStatus(entity.LastDoneOn, entity.FrequencyValue, entity.FrequencyUnit, historyCount));
    }

    public async Task DeleteTaskAsync(Guid id, ConfirmDeletePeriodicTaskRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.PeriodicTasks
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Task not found.");
        if (!string.Equals(entity.Title, req.ConfirmationTitle, StringComparison.Ordinal))
            throw new ArgumentException("Type the task name exactly to delete it.");
        var old = Snapshot(entity);
        _db.PeriodicTasks.Remove(entity);
        await _db.SaveChangesAsync(ct);
        await ArchiveAsync("Periodic", "Task", entity.Id, "delete", old, null, ct);
    }

    public async Task ReorderTasksAsync(IReadOnlyList<ReorderPeriodicTaskRequest> req, CancellationToken ct)
    {
        var owner = OwnerId;
        var ids = req.Select(x => x.TaskId).ToHashSet();
        var tasks = await _db.PeriodicTasks.Where(x => x.OwnerUserId == owner && ids.Contains(x.Id)).ToListAsync(ct);
        var groupIds = req.Select(x => x.GroupId).ToHashSet();
        var validGroupIds = await _db.PeriodicTaskGroups.Where(x => x.OwnerUserId == owner && groupIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(ct);
        foreach (var item in req)
        {
            var task = tasks.FirstOrDefault(x => x.Id == item.TaskId);
            if (task is not null && validGroupIds.Contains(item.GroupId))
            {
                task.GroupId = item.GroupId;
                task.DisplayOrder = item.DisplayOrder;
            }
        }
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
            .OrderBy(t => t.Group!.DisplayOrder).ThenBy(t => t.Group!.Name).ThenBy(t => t.DisplayOrder).ThenBy(t => t.Title)
            .Select(t => new
            {
                GroupName = t.Group!.Name,
                t.Title,
                TimesDoneInRange = t.History.Count(h => h.CompletedOn >= from && h.CompletedOn <= to),
                t.LastDoneOn,
                t.FrequencyValue,
                t.FrequencyUnit,
                History = t.History
                    .Where(h => h.CompletedOn >= from && h.CompletedOn <= to)
                    .OrderBy(h => h.CompletedOn)
                    .Select(h => new { h.CompletedOn, h.Note })
                    .ToList()
            })
            .ToListAsync(ct);

        var reportRows = rows.Select(r => new PeriodicReportRow(
            r.GroupName, r.Title, r.TimesDoneInRange, r.LastDoneOn,
            ComputeNextDue(r.LastDoneOn, r.FrequencyValue, r.FrequencyUnit),
            string.Join(", ", r.History.Select(h => $"{h.CompletedOn:dd/MM/yyyy}" + (string.IsNullOrWhiteSpace(h.Note) ? "" : $"({h.Note})"))))).ToList();

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

    private async Task<int> NextGroupOrderAsync(CancellationToken ct)
        => (await _db.PeriodicTaskGroups.Where(x => x.OwnerUserId == OwnerId).MaxAsync(x => (int?)x.DisplayOrder, ct) ?? 0) + 1;

    private async Task<int> NextTaskOrderAsync(Guid groupId, CancellationToken ct)
        => (await _db.PeriodicTasks.Where(x => x.OwnerUserId == OwnerId && x.GroupId == groupId).MaxAsync(x => (int?)x.DisplayOrder, ct) ?? 0) + 1;

    private async Task ArchiveAsync(string module, string entityType, Guid entityId, string activityType, string? oldValue, string? newValue, CancellationToken ct)
    {
        _db.TaskArchiveEntries.Add(new TaskArchiveEntry
        {
            OwnerUserId = OwnerId,
            Module = module,
            EntityType = entityType,
            EntityId = entityId,
            ActivityType = activityType,
            OldValue = oldValue,
            NewValue = newValue,
            ActionDate = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private static string Snapshot(PeriodicTaskGroup g) => $"Name={g.Name}; Description={g.Description}; DisplayOrder={g.DisplayOrder}";
    private static string Snapshot(PeriodicTask t) => $"GroupId={t.GroupId}; Title={t.Title}; Description={t.Description}; Status={t.Status}; Frequency={t.FrequencyValue} {t.FrequencyUnit}; LastDoneOn={t.LastDoneOn}; DisplayOrder={t.DisplayOrder}";

    private static string DueStatus(DateOnly? lastDoneOn, int value, FrequencyUnit unit, int historyCount)
    {
        if (historyCount == 0 && lastDoneOn is null) return "Never done";
        var next = ComputeNextDue(lastDoneOn, value, unit);
        if (next is null) return "Never done";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (next < today) return "Overdue";
        if (next <= today.AddDays(3)) return "Due soon";
        return "Going well";
    }
}
