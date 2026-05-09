using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Application.Tasks.Daily;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Domain.Tasks.Daily;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.Tasks.Daily;

public class DailyTaskService : IDailyTaskService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public DailyTaskService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId
        ?? throw new InvalidOperationException("No authenticated user.");

    public async Task<IReadOnlyList<DailyTaskGroupDto>> GetGroupsAsync(CancellationToken ct)
    {
        var owner = OwnerId;
        return await _db.DailyTaskGroups
            .Where(g => g.OwnerUserId == owner)
            .OrderBy(g => g.Name)
            .Select(g => new DailyTaskGroupDto(
                g.Id, g.Name, g.Description,
                g.Tasks.Count(t => t.Status == TaskActiveStatus.Active)))
            .ToListAsync(ct);
    }

    public async Task<DailyTaskGroupDto> CreateGroupAsync(CreateDailyGroupRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = new DailyTaskGroup
        {
            OwnerUserId = owner,
            Name = req.Name.Trim(),
            Description = req.Description?.Trim()
        };
        _db.DailyTaskGroups.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new DailyTaskGroupDto(entity.Id, entity.Name, entity.Description, 0);
    }

    public async Task<DailyTaskGroupDto> UpdateGroupAsync(Guid id, UpdateDailyGroupRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.DailyTaskGroups
            .FirstOrDefaultAsync(g => g.Id == id && g.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Group not found.");

        entity.Name = req.Name.Trim();
        entity.Description = req.Description?.Trim();
        await _db.SaveChangesAsync(ct);

        var taskCount = await _db.DailyTasks
            .CountAsync(t => t.GroupId == id && t.Status == TaskActiveStatus.Active, ct);
        return new DailyTaskGroupDto(entity.Id, entity.Name, entity.Description, taskCount);
    }

    public async Task DeleteGroupAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.DailyTaskGroups
            .FirstOrDefaultAsync(g => g.Id == id && g.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Group not found.");

        var tasks = await _db.DailyTasks.Where(t => t.GroupId == id).ToListAsync(ct);
        _db.DailyTasks.RemoveRange(tasks);
        _db.DailyTaskGroups.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DailyTaskDto>> GetTasksAsync(Guid? groupId, bool includeInactive, CancellationToken ct)
    {
        var owner = OwnerId;
        var query = _db.DailyTasks.Where(t => t.OwnerUserId == owner);
        if (groupId.HasValue) query = query.Where(t => t.GroupId == groupId.Value);
        if (!includeInactive) query = query.Where(t => t.Status == TaskActiveStatus.Active);

        return await query
            .OrderBy(t => t.Title)
            .Select(t => new DailyTaskDto(t.Id, t.GroupId, t.Title, t.Description, t.Status))
            .ToListAsync(ct);
    }

    public async Task<DailyTaskDto> CreateTaskAsync(CreateDailyTaskRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var groupExists = await _db.DailyTaskGroups
            .AnyAsync(g => g.Id == req.GroupId && g.OwnerUserId == owner, ct);
        if (!groupExists) throw new KeyNotFoundException("Group not found.");

        var entity = new DailyTask
        {
            OwnerUserId = owner,
            GroupId = req.GroupId,
            Title = req.Title.Trim(),
            Description = req.Description?.Trim(),
            Status = req.Status
        };
        _db.DailyTasks.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new DailyTaskDto(entity.Id, entity.GroupId, entity.Title, entity.Description, entity.Status);
    }

    public async Task<DailyTaskDto> UpdateTaskAsync(Guid id, UpdateDailyTaskRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.DailyTasks
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Task not found.");

        if (entity.GroupId != req.GroupId)
        {
            var groupExists = await _db.DailyTaskGroups
                .AnyAsync(g => g.Id == req.GroupId && g.OwnerUserId == owner, ct);
            if (!groupExists) throw new KeyNotFoundException("Group not found.");
            entity.GroupId = req.GroupId;
        }

        entity.Title = req.Title.Trim();
        entity.Description = req.Description?.Trim();
        entity.Status = req.Status;
        await _db.SaveChangesAsync(ct);
        return new DailyTaskDto(entity.Id, entity.GroupId, entity.Title, entity.Description, entity.Status);
    }

    public async Task DeleteTaskAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.DailyTasks
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Task not found.");
        _db.DailyTasks.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<DailyByDateView> GetByDateAsync(DateOnly date, CancellationToken ct)
    {
        var owner = OwnerId;
        var groups = await _db.DailyTaskGroups
            .Where(g => g.OwnerUserId == owner)
            .OrderBy(g => g.Name)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                Tasks = g.Tasks
                    .Where(t => t.Status == TaskActiveStatus.Active)
                    .OrderBy(t => t.Title)
                    .Select(t => new
                    {
                        t.Id,
                        t.Title,
                        t.Description,
                        t.Status,
                        Completion = t.Completions
                            .Where(c => c.Date == date)
                            .Select(c => new DailyCompletionDto(c.IsCompleted, c.Note))
                            .FirstOrDefault()
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        var groupViews = groups.Select(g =>
        {
            var taskViews = g.Tasks
                .Select(t => new DailyTaskWithCompletionDto(t.Id, t.Title, t.Description, t.Status, t.Completion))
                .ToList();
            var total = taskViews.Count;
            var completed = taskViews.Count(t => t.Completion?.IsCompleted == true);
            return new DailyGroupView(
                g.Id, g.Name, g.Description,
                new DailyCounts(total, completed, total - completed),
                taskViews);
        }).ToList();

        var totals = new DailyCounts(
            groupViews.Sum(g => g.Counts.Total),
            groupViews.Sum(g => g.Counts.Completed),
            groupViews.Sum(g => g.Counts.NotCompleted));

        return new DailyByDateView(date, totals, groupViews);
    }

    public async Task<DailyDayWiseReport> GetDayWiseReportAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (to < from) (from, to) = (to, from);
        var owner = OwnerId;

        var tasks = await _db.DailyTasks
            .Where(t => t.OwnerUserId == owner && t.Status == TaskActiveStatus.Active)
            .Select(t => new { t.Id, t.Title, GroupName = t.Group!.Name })
            .ToListAsync(ct);

        var taskIds = tasks.Select(t => t.Id).ToHashSet();
        var completions = await _db.DailyTaskCompletions
            .Where(c => c.OwnerUserId == owner && c.Date >= from && c.Date <= to && taskIds.Contains(c.DailyTaskId))
            .Select(c => new { c.DailyTaskId, c.Date, c.IsCompleted, c.Note })
            .ToListAsync(ct);

        var lookup = completions
            .ToDictionary(c => (c.DailyTaskId, c.Date),
                c => (c.IsCompleted, c.Note));

        var rows = new List<DailyDayWiseRow>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            foreach (var task in tasks.OrderBy(t => t.GroupName).ThenBy(t => t.Title))
            {
                lookup.TryGetValue((task.Id, d), out var completion);
                rows.Add(new DailyDayWiseRow(d, task.GroupName, task.Title, completion.IsCompleted, completion.Note));
            }
        }

        return new DailyDayWiseReport(from, to, rows);
    }

    public async Task<DailyTaskWiseReport> GetTaskWiseReportAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (to < from) (from, to) = (to, from);
        var owner = OwnerId;

        var rows = await _db.DailyTasks
            .Where(t => t.OwnerUserId == owner && t.Status == TaskActiveStatus.Active)
            .OrderBy(t => t.Group!.Name).ThenBy(t => t.Title)
            .Select(t => new DailyTaskWiseRow(
                t.Group!.Name,
                t.Title,
                t.Completions.Count(c => c.IsCompleted && c.Date >= from && c.Date <= to),
                t.Completions
                    .Where(c => c.IsCompleted && c.Date >= from && c.Date <= to)
                    .Max(c => (DateOnly?)c.Date)))
            .ToListAsync(ct);

        return new DailyTaskWiseReport(from, to, rows);
    }

    public async Task<DailyCompletionDto> UpsertCompletionAsync(Guid taskId, DateOnly date, UpsertCompletionRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var task = await _db.DailyTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Task not found.");

        var existing = await _db.DailyTaskCompletions
            .FirstOrDefaultAsync(c => c.DailyTaskId == taskId && c.Date == date, ct);

        if (existing is null)
        {
            existing = new DailyTaskCompletion
            {
                OwnerUserId = owner,
                DailyTaskId = taskId,
                Date = date,
                IsCompleted = req.IsCompleted,
                Note = req.Note
            };
            _db.DailyTaskCompletions.Add(existing);
        }
        else
        {
            existing.IsCompleted = req.IsCompleted;
            existing.Note = req.Note;
        }

        await _db.SaveChangesAsync(ct);
        return new DailyCompletionDto(existing.IsCompleted, existing.Note);
    }
}
