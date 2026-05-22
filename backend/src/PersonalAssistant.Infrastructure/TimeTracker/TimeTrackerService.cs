using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Application.TimeTracker;
using PersonalAssistant.Domain.TimeTracker;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.TimeTracker;

public class TimeTrackerService : ITimeTrackerService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public TimeTrackerService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId ?? throw new InvalidOperationException("No authenticated user.");

    public async Task<TimePagedResult<TimeEntryDto>> ListAsync(TimeEntryFilters filters, int page, int pageSize, CancellationToken ct)
    {
        var q = Query(filters);
        var total = await q.CountAsync(ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var rows = await q.OrderByDescending(x => x.StartTime)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => Map(x))
            .ToListAsync(ct);
        return new TimePagedResult<TimeEntryDto>(rows, page, pageSize, total);
    }

    public async Task<TimeEntryDto> GetAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.TimeEntries.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Time entry not found.");
        return Map(entity);
    }

    public async Task<TimeEntryDto> CreateAsync(TimeEntryRequest req, CancellationToken ct)
    {
        Validate(req);
        var entity = new TimeEntry
        {
            OwnerUserId = OwnerId,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            Activity = req.Activity.Trim(),
            Note = req.Note?.Trim(),
            Tag = req.Tag?.Trim()
        };
        _db.TimeEntries.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<TimeEntryDto> UpdateAsync(Guid id, TimeEntryRequest req, CancellationToken ct)
    {
        Validate(req);
        var owner = OwnerId;
        var entity = await _db.TimeEntries.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Time entry not found.");
        entity.StartTime = req.StartTime;
        entity.EndTime = req.EndTime;
        entity.Activity = req.Activity.Trim();
        entity.Note = req.Note?.Trim();
        entity.Tag = req.Tag?.Trim();
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.TimeEntries.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Time entry not found.");
        _db.TimeEntries.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<TimeReport> GetReportAsync(TimeEntryFilters filters, CancellationToken ct)
    {
        if (!filters.From.HasValue || !filters.To.HasValue) throw new ArgumentException("From and To are required.");
        if (filters.To < filters.From) throw new ArgumentException("To must be after From.");

        var rows = await Query(filters).OrderBy(x => x.StartTime).ToListAsync(ct);
        var reportRows = rows.Select(x => new TimeReportRow(x.StartTime, x.EndTime, x.Activity, x.Tag, Minutes(x), x.Note)).ToList();
        var activitySummary = rows.GroupBy(x => x.Activity)
            .Select(g => new TimeSummaryRow(g.Key, g.Select(x => DateOnly.FromDateTime(x.StartTime)).Distinct().Count(), g.Sum(Minutes)))
            .OrderByDescending(x => x.NumberOfMinutes).ToList();
        var tagSummary = rows.Where(x => !string.IsNullOrWhiteSpace(x.Tag)).GroupBy(x => x.Tag!)
            .Select(g => new TimeSummaryRow(g.Key, g.Select(x => DateOnly.FromDateTime(x.StartTime)).Distinct().Count(), g.Sum(Minutes)))
            .OrderByDescending(x => x.NumberOfMinutes).ToList();
        return new TimeReport(filters.From.Value, filters.To.Value, reportRows, activitySummary, tagSummary);
    }

    private IQueryable<TimeEntry> Query(TimeEntryFilters f)
    {
        var owner = OwnerId;
        var q = _db.TimeEntries.Where(x => x.OwnerUserId == owner);
        if (f.From.HasValue) q = q.Where(x => x.EndTime >= f.From.Value);
        if (f.To.HasValue) q = q.Where(x => x.StartTime <= f.To.Value);
        if (!string.IsNullOrWhiteSpace(f.Activity))
        {
            var s = f.Activity.Trim();
            q = q.Where(x => EF.Functions.Like(x.Activity, $"%{s}%"));
        }
        if (!string.IsNullOrWhiteSpace(f.Tag))
        {
            var tag = f.Tag.Trim();
            q = q.Where(x => x.Tag != null && EF.Functions.Like(x.Tag, $"%{tag}%"));
        }
        return q;
    }

    private static void Validate(TimeEntryRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Activity)) throw new ArgumentException("Activity is required.");
        if (req.EndTime <= req.StartTime) throw new ArgumentException("End time must be after start time.");
    }

    private static int Minutes(TimeEntry x) => (int)Math.Round((x.EndTime - x.StartTime).TotalMinutes);
    private static TimeEntryDto Map(TimeEntry x) => new(x.Id, x.StartTime, x.EndTime, x.Activity, x.Note, x.Tag, Minutes(x));
}
