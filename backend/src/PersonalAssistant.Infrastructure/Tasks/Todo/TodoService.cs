using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Application.Tasks.Todo;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Domain.Tasks.Todo;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.Tasks.Todo;

public class TodoService : ITodoService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public TodoService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId
        ?? throw new InvalidOperationException("No authenticated user.");

    public async Task<IReadOnlyList<TodoDto>> GetAllAsync(TodoStatus? status, TodoSort sortBy, SortOrder order, CancellationToken ct)
    {
        var owner = OwnerId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var query = _db.Todos.Where(t => t.OwnerUserId == owner);
        if (status.HasValue) query = query.Where(t => t.Status == status.Value);

        IOrderedQueryable<TodoItem> ordered = sortBy switch
        {
            TodoSort.AddedDate => order == SortOrder.Asc ? query.OrderBy(t => t.AddedDate) : query.OrderByDescending(t => t.AddedDate),
            TodoSort.Status   => order == SortOrder.Asc ? query.OrderBy(t => t.Status)    : query.OrderByDescending(t => t.Status),
            TodoSort.Deadline => order == SortOrder.Asc
                ? query.OrderBy(t => t.Deadline ?? DateOnly.MaxValue)
                : query.OrderByDescending(t => t.Deadline ?? DateOnly.MinValue),
            TodoSort.DaysLeft => order == SortOrder.Asc
                ? query.OrderBy(t => t.Deadline ?? DateOnly.MaxValue)
                : query.OrderByDescending(t => t.Deadline ?? DateOnly.MinValue),
            _ => query.OrderByDescending(t => t.AddedDate)
        };

        var items = await ordered.ThenByDescending(t => t.UpdatedAt).ToListAsync(ct);
        return items.Select(t => Map(t, today)).ToList();
    }

    public async Task<TodoDto> GetAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Todo not found.");
        return Map(entity, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public async Task<TodoDto> CreateAsync(CreateTodoRequest req, CancellationToken ct)
    {
        if (req.Status is TodoStatus.Completed or TodoStatus.Cancelled)
            throw new ArgumentException("New tasks cannot be created as completed or cancelled.");

        var owner = OwnerId;
        var entity = new TodoItem
        {
            OwnerUserId = owner,
            Title = req.Title.Trim(),
            Description = req.Description?.Trim(),
            AddedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Deadline = req.Deadline,
            Status = req.Status
        };
        _db.Todos.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Map(entity, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public async Task<TodoDto> UpdateAsync(Guid id, UpdateTodoRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Todo not found.");

        entity.Title = req.Title.Trim();
        entity.Description = req.Description?.Trim();
        entity.Deadline = req.Deadline;
        entity.Status = req.Status;

        if (req.Status == TodoStatus.Completed)
        {
            entity.CompletedOn = req.CompletedOn ?? entity.CompletedOn ?? DateOnly.FromDateTime(DateTime.UtcNow);
            entity.CompletionNote = req.StatusNote?.Trim();
        }
        else
        {
            entity.CompletedOn = req.CompletedOn;
            entity.CompletionNote = req.StatusNote?.Trim();
        }

        await _db.SaveChangesAsync(ct);
        return Map(entity, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Todo not found.");
        _db.Todos.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<TodoSummary> GetSummaryAsync(CancellationToken ct)
    {
        var owner = OwnerId;
        var grouped = await _db.Todos
            .Where(t => t.OwnerUserId == owner)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var total = grouped.Sum(g => g.Count);
        var by = grouped
            .OrderBy(g => g.Status)
            .Select(g => new TodoStatusCount(g.Status, g.Count))
            .ToList();

        return new TodoSummary(total, by);
    }

    public async Task<TodoReport> GetReportAsync(DateOnly asOf, CancellationToken ct)
    {
        var owner = OwnerId;

        var items = await _db.Todos
            .Where(t => t.OwnerUserId == owner && t.AddedDate <= asOf)
            .OrderBy(t => t.Status).ThenBy(t => t.Deadline ?? DateOnly.MaxValue)
            .ToListAsync(ct);

        var rows = items.Select(t => new TodoReportRow(
            t.Title, t.AddedDate, t.Deadline,
            ComputeDaysLeft(t.Deadline, asOf),
            t.Status, t.CompletedOn, t.CompletionNote)).ToList();

        return new TodoReport(asOf, rows);
    }

    private static TodoDto Map(TodoItem t, DateOnly today) => new(
        t.Id, t.Title, t.Description, t.AddedDate, t.Deadline,
        ComputeDaysLeft(t.Deadline, today),
        t.Status, t.CompletedOn, t.CompletionNote);

    private static int? ComputeDaysLeft(DateOnly? deadline, DateOnly today)
    {
        if (deadline is null) return null;
        return deadline.Value.DayNumber - today.DayNumber;
    }
}
