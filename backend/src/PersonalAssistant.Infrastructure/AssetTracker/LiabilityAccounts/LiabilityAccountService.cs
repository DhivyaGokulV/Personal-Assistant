using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.AssetTracker.LiabilityAccounts;
using PersonalAssistant.Application.AssetTracker.PreciousMetals;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Domain.AssetTracker;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.AssetTracker.LiabilityAccounts;

public class LiabilityAccountService : ILiabilityAccountService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public LiabilityAccountService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId ?? throw new InvalidOperationException("No authenticated user.");
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public async Task<AssetTrackerPage<LiabilityAccountDto>> ListAsync(LiabilityAccountCategory category, LiabilityAccountQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var q = _db.LiabilityAccounts.AsNoTracking()
            .Include(x => x.Entries)
            .Where(x => x.OwnerUserId == OwnerId && x.Category == category);

        if (query.Status.HasValue) q = q.Where(x => x.Status == query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(x => x.Name.Contains(search) || (x.Description != null && x.Description.Contains(search)));
        }

        var rows = (await q.ToListAsync(ct)).Select(Map).ToList();
        rows = (query.SortBy, query.SortDirection) switch
        {
            (LiabilityAccountSort.CreationDate, AssetSortDirection.Asc) => rows.OrderBy(x => x.CreationDate).ThenBy(x => x.Name).ToList(),
            (LiabilityAccountSort.CreationDate, AssetSortDirection.Desc) => rows.OrderByDescending(x => x.CreationDate).ThenBy(x => x.Name).ToList(),
            (LiabilityAccountSort.StandingAmount, AssetSortDirection.Asc) => rows.OrderBy(x => x.StandingAmount).ThenBy(x => x.Name).ToList(),
            (LiabilityAccountSort.StandingAmount, AssetSortDirection.Desc) => rows.OrderByDescending(x => x.StandingAmount).ThenBy(x => x.Name).ToList(),
            (LiabilityAccountSort.LastEntryDate, AssetSortDirection.Asc) => rows.OrderBy(x => x.LastEntryDate).ThenBy(x => x.Name).ToList(),
            (LiabilityAccountSort.LastEntryDate, AssetSortDirection.Desc) => rows.OrderByDescending(x => x.LastEntryDate).ThenBy(x => x.Name).ToList(),
            (LiabilityAccountSort.Status, AssetSortDirection.Asc) => rows.OrderBy(x => x.Status).ThenBy(x => x.Name).ToList(),
            (LiabilityAccountSort.Status, AssetSortDirection.Desc) => rows.OrderByDescending(x => x.Status).ThenBy(x => x.Name).ToList(),
            (_, AssetSortDirection.Desc) => rows.OrderByDescending(x => x.Name).ToList(),
            _ => rows.OrderBy(x => x.Name).ToList()
        };
        return new AssetTrackerPage<LiabilityAccountDto>(rows.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, rows.Count);
    }

    public async Task<LiabilityAccountDto> CreateAsync(LiabilityAccountCategory category, SaveLiabilityAccountRequest request, CancellationToken ct)
    {
        ValidateAccount(category, request, null);
        var entity = new LiabilityAccount
        {
            OwnerUserId = OwnerId,
            Category = category,
            Name = request.Name.Trim(),
            Description = Clean(request.Description),
            CreationDate = request.CreationDate,
            Status = LiabilityAccountStatus.Active,
            CurrencyCode = "INR"
        };
        entity.StatusHistory.Add(new LiabilityAccountStatusHistory
        {
            OwnerUserId = OwnerId,
            LiabilityAccountId = entity.Id,
            Status = LiabilityAccountStatus.Active,
            EffectiveDate = request.CreationDate
        });
        _db.LiabilityAccounts.Add(entity);
        AddAudit(entity.Id, "Account", entity.Id, InvestmentAuditAction.Create, null, Snapshot(entity), null);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<LiabilityAccountDto> UpdateAsync(LiabilityAccountCategory category, Guid id, SaveLiabilityAccountRequest request, CancellationToken ct)
    {
        var entity = await AccountQuery(category, id).FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"{Label(category)} not found.");
        ValidateAccount(category, request, entity);
        var old = Snapshot(entity);
        var changed = new List<string>();
        if (entity.Name != request.Name.Trim()) changed.Add("name");
        if (entity.Description != Clean(request.Description)) changed.Add("description");
        if (entity.CreationDate != request.CreationDate) changed.Add("creationDate");
        entity.Name = request.Name.Trim();
        entity.Description = Clean(request.Description);
        entity.CreationDate = request.CreationDate;
        if (changed.Count > 0) AddAudit(id, "Account", id, InvestmentAuditAction.Update, old, Snapshot(entity), changed);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task DeleteAsync(LiabilityAccountCategory category, Guid id, CancellationToken ct)
    {
        var entity = await AccountQuery(category, id).Include(x => x.StatusHistory).FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"{Label(category)} not found.");
        var snapshot = JsonSerializer.Serialize(new
        {
            Account = Snapshot(entity),
            Entries = entity.Entries.Select(Snapshot),
            Statuses = entity.StatusHistory.Select(Snapshot)
        });
        AddAudit(id, "Account", id, InvestmentAuditAction.Delete, snapshot, null, null);
        _db.LiabilityAccountEntries.RemoveRange(entity.Entries);
        _db.LiabilityAccountStatusHistory.RemoveRange(entity.StatusHistory);
        _db.LiabilityAccounts.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<LiabilityAccountStatusDto>> GetStatusHistoryAsync(LiabilityAccountCategory category, Guid id, CancellationToken ct)
    {
        await EnsureAccountAsync(category, id, ct);
        return await _db.LiabilityAccountStatusHistory.AsNoTracking()
            .Where(x => x.OwnerUserId == OwnerId && x.LiabilityAccountId == id)
            .OrderByDescending(x => x.EffectiveDate).ThenByDescending(x => x.CreatedAt)
            .Select(x => new LiabilityAccountStatusDto(x.Id, x.Status, x.EffectiveDate)).ToListAsync(ct);
    }

    public async Task<LiabilityAccountStatusDto> ChangeStatusAsync(LiabilityAccountCategory category, Guid id, ChangeLiabilityAccountStatusRequest request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Status)) throw new ArgumentException("Status is required.");
        var entity = await EnsureAccountAsync(category, id, ct);
        ValidateDate(request.EffectiveDate, entity.CreationDate, Label(category));
        if (request.Status == LiabilityAccountStatus.Inactive && ComputeBalance(entity.Entries) != 0)
            throw new InvalidOperationException($"{Label(category)} can be made inactive only when standing amount is zero.");
        if (await _db.LiabilityAccountStatusHistory.AnyAsync(x => x.LiabilityAccountId == id && x.EffectiveDate == request.EffectiveDate, ct))
            throw new InvalidOperationException("A status entry already exists for this date.");
        var status = new LiabilityAccountStatusHistory
        {
            OwnerUserId = OwnerId,
            LiabilityAccountId = id,
            Status = request.Status,
            EffectiveDate = request.EffectiveDate
        };
        _db.LiabilityAccountStatusHistory.Add(status);
        AddAudit(id, "Status", status.Id, InvestmentAuditAction.StatusChange, null, Snapshot(status), null);
        var latest = await _db.LiabilityAccountStatusHistory.Where(x => x.LiabilityAccountId == id)
            .OrderByDescending(x => x.EffectiveDate).ThenByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        if (latest is null || request.EffectiveDate >= latest.EffectiveDate) entity.Status = request.Status;
        await _db.SaveChangesAsync(ct);
        return MapStatus(status);
    }

    public async Task<AssetTrackerPage<LiabilityAccountEntryDto>> GetEntriesAsync(LiabilityAccountCategory category, Guid id, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct)
    {
        await EnsureAccountAsync(category, id, ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var q = _db.LiabilityAccountEntries.AsNoTracking().Where(x => x.OwnerUserId == OwnerId && x.LiabilityAccountId == id);
        if (from.HasValue) q = q.Where(x => x.Date >= from.Value);
        if (to.HasValue) q = q.Where(x => x.Date <= to.Value);
        var total = await q.CountAsync(ct);
        var all = await _db.LiabilityAccountEntries.AsNoTracking()
            .Where(x => x.OwnerUserId == OwnerId && x.LiabilityAccountId == id)
            .OrderBy(x => x.Date).ThenBy(x => x.CreatedAt).ToListAsync(ct);
        var running = BuildRunningBalanceMap(all);
        var rows = await q.OrderByDescending(x => x.Date).ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new AssetTrackerPage<LiabilityAccountEntryDto>(rows.Select(x => MapEntry(x, running[x.Id])).ToList(), page, pageSize, total);
    }

    public async Task<LiabilityAccountEntryDto> AddEntryAsync(LiabilityAccountCategory category, Guid id, SaveLiabilityAccountEntryRequest request, CancellationToken ct)
    {
        var account = await EnsureAccountAsync(category, id, ct);
        ValidateEntry(account, request);
        var entity = NewEntry(id, request);
        entity.OwnerUserId = OwnerId;
        var ledger = await _db.LiabilityAccountEntries.Where(x => x.LiabilityAccountId == id).ToListAsync(ct);
        ledger.Add(entity);
        EnsureValidLedger(ledger, category);
        _db.LiabilityAccountEntries.Add(entity);
        AddAudit(id, "Entry", entity.Id, InvestmentAuditAction.Create, null, Snapshot(entity), null);
        await _db.SaveChangesAsync(ct);
        return MapEntry(entity, BuildRunningBalanceMap(ledger)[entity.Id]);
    }

    public async Task<LiabilityAccountEntryDto> UpdateEntryAsync(LiabilityAccountCategory category, Guid id, Guid entryId, SaveLiabilityAccountEntryRequest request, CancellationToken ct)
    {
        var account = await EnsureAccountAsync(category, id, ct);
        ValidateEntry(account, request);
        var entity = await _db.LiabilityAccountEntries.FirstOrDefaultAsync(x => x.Id == entryId && x.LiabilityAccountId == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Liability entry not found.");
        var old = Snapshot(entity);
        ApplyEntry(entity, request);
        var ledger = await _db.LiabilityAccountEntries.Where(x => x.LiabilityAccountId == id).ToListAsync(ct);
        EnsureValidLedger(ledger, category);
        AddAudit(id, "Entry", entity.Id, InvestmentAuditAction.Update, old, Snapshot(entity), new[] { "type", "date", "note", "amount" });
        await _db.SaveChangesAsync(ct);
        return MapEntry(entity, BuildRunningBalanceMap(ledger)[entity.Id]);
    }

    public async Task DeleteEntryAsync(LiabilityAccountCategory category, Guid id, Guid entryId, CancellationToken ct)
    {
        await EnsureAccountAsync(category, id, ct);
        var entity = await _db.LiabilityAccountEntries.FirstOrDefaultAsync(x => x.Id == entryId && x.LiabilityAccountId == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Liability entry not found.");
        var ledger = await _db.LiabilityAccountEntries.Where(x => x.LiabilityAccountId == id && x.Id != entryId).ToListAsync(ct);
        EnsureValidLedger(ledger, category);
        AddAudit(id, "Entry", entity.Id, InvestmentAuditAction.Delete, Snapshot(entity), null, null);
        _db.LiabilityAccountEntries.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<LiabilityStatisticsDto> GetStatisticsAsync(LiabilityAccountCategory category, Guid id, LiabilityStatisticsDuration duration, CancellationToken ct)
    {
        var account = await EnsureAccountAsync(category, id, ct);
        var from = duration switch
        {
            LiabilityStatisticsDuration.OneMonth => Today.AddMonths(-1),
            LiabilityStatisticsDuration.ThreeMonths => Today.AddMonths(-3),
            LiabilityStatisticsDuration.SixMonths => Today.AddMonths(-6),
            LiabilityStatisticsDuration.OneYear => Today.AddYears(-1),
            LiabilityStatisticsDuration.ThreeYears => Today.AddYears(-3),
            _ => Today.AddYears(-5)
        };
        var entries = account.Entries.OrderBy(x => x.Date).ThenBy(x => x.CreatedAt).ToList();
        decimal running = 0;
        var points = new List<LiabilityStatisticsPoint>();
        foreach (var entry in entries)
        {
            running += entry.Type == LiabilityAccountTxType.Credit ? entry.Amount : -entry.Amount;
            if (entry.Date >= from && entry.Date <= Today) points.Add(new LiabilityStatisticsPoint(entry.Date, running));
        }
        return new LiabilityStatisticsDto("Standing amount", account.CurrencyCode, points);
    }

    public async Task<IReadOnlyList<LiabilityAccountExportRow>> GetExportAsync(LiabilityAccountCategory category, LiabilityAccountExportRequest request, CancellationToken ct)
    {
        if (request.LiabilityAccountIds.Count == 0) throw new ArgumentException($"Select at least one {Label(category).ToLowerInvariant()}.");
        if (request.To < request.From) throw new ArgumentException("The end date must be on or after the start date.");
        if (request.To > request.From.AddYears(3)) throw new ArgumentException("Export duration cannot exceed three years.");
        var accounts = await _db.LiabilityAccounts.AsNoTracking().Include(x => x.Entries).Include(x => x.StatusHistory)
            .Where(x => x.OwnerUserId == OwnerId && x.Category == category && request.LiabilityAccountIds.Contains(x.Id)).ToListAsync(ct);
        if (accounts.Count != request.LiabilityAccountIds.Distinct().Count())
            throw new KeyNotFoundException($"One or more {Label(category).ToLowerInvariant()} records were not found.");
        var rows = new List<LiabilityAccountExportRow>();
        foreach (var account in accounts.OrderBy(x => x.Name))
        {
            var running = BuildRunningBalanceMap(account.Entries.OrderBy(x => x.Date).ThenBy(x => x.CreatedAt));
            rows.AddRange(account.Entries.Where(x => x.Date >= request.From && x.Date <= request.To)
                .Select(x => new LiabilityAccountExportRow(account.Name, x.Type.ToString(), x.Date, account.CurrencyCode, x.Note ?? string.Empty, x.Amount, running[x.Id])));
            rows.AddRange(account.StatusHistory.Where(x => x.EffectiveDate >= request.From && x.EffectiveDate <= request.To)
                .Select(x => new LiabilityAccountExportRow(account.Name, "Status", x.EffectiveDate, account.CurrencyCode, x.Status.ToString(), null, null)));
        }
        return rows.OrderBy(x => x.Account).ThenBy(x => x.Date).ToList();
    }

    private IQueryable<LiabilityAccount> AccountQuery(LiabilityAccountCategory category, Guid id) => _db.LiabilityAccounts
        .Include(x => x.Entries).Include(x => x.StatusHistory)
        .Where(x => x.Id == id && x.OwnerUserId == OwnerId && x.Category == category);

    private async Task<LiabilityAccount> EnsureAccountAsync(LiabilityAccountCategory category, Guid id, CancellationToken ct) =>
        await AccountQuery(category, id).FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException($"{Label(category)} not found.");

    private static void ValidateAccount(LiabilityAccountCategory category, SaveLiabilityAccountRequest request, LiabilityAccount? existing)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var min = category == LiabilityAccountCategory.Loan ? 3 : 2;
        if (name.Length < min || name.Length > 100) throw new ArgumentException($"Name must contain between {min} and 100 characters.");
        if ((request.Description?.Trim().Length ?? 0) > 500) throw new ArgumentException("Description must not exceed 500 characters.");
        if (request.CreationDate > Today) throw new ArgumentException("Creation date cannot be in the future.");
        if (existing is not null)
        {
            var earliestEntry = existing.Entries.OrderBy(x => x.Date).FirstOrDefault()?.Date;
            if (earliestEntry.HasValue && request.CreationDate > earliestEntry.Value)
                throw new InvalidOperationException("Creation date cannot be after an existing history entry.");
        }
    }

    private static void ValidateEntry(LiabilityAccount account, SaveLiabilityAccountEntryRequest request)
    {
        if (!Enum.IsDefined(request.Type)) throw new ArgumentException("Type is required.");
        ValidateDate(request.Date, account.CreationDate, Label(account.Category));
        if ((request.Note?.Trim().Length ?? 0) > 200) throw new ArgumentException("Note must not exceed 200 characters.");
        if (request.Amount <= 0) throw new ArgumentException("Amount must be greater than zero.");
    }

    private static void ValidateDate(DateOnly date, DateOnly creationDate, string label)
    {
        if (date < creationDate) throw new ArgumentException($"Date cannot be before the {label.ToLowerInvariant()} creation date.");
        if (date > Today) throw new ArgumentException("Date cannot be in the future.");
    }

    private static void EnsureValidLedger(IEnumerable<LiabilityAccountEntry> entries, LiabilityAccountCategory category)
    {
        decimal balance = 0;
        foreach (var entry in entries.OrderBy(x => x.Date).ThenBy(x => x.CreatedAt).ThenBy(x => x.Id))
        {
            balance += entry.Type == LiabilityAccountTxType.Credit ? entry.Amount : -entry.Amount;
            if (balance < 0) throw new InvalidOperationException($"The entry would make {Label(category).ToLowerInvariant()} standing amount negative on {entry.Date:yyyy-MM-dd}.");
        }
    }

    private static decimal ComputeBalance(IEnumerable<LiabilityAccountEntry> entries) =>
        entries.Sum(x => x.Type == LiabilityAccountTxType.Credit ? x.Amount : -x.Amount);

    private static Dictionary<Guid, decimal> BuildRunningBalanceMap(IEnumerable<LiabilityAccountEntry> entries)
    {
        decimal running = 0;
        var map = new Dictionary<Guid, decimal>();
        foreach (var entry in entries.OrderBy(x => x.Date).ThenBy(x => x.CreatedAt).ThenBy(x => x.Id))
        {
            running += entry.Type == LiabilityAccountTxType.Credit ? entry.Amount : -entry.Amount;
            map[entry.Id] = running;
        }
        return map;
    }

    private static LiabilityAccountEntry NewEntry(Guid accountId, SaveLiabilityAccountEntryRequest request)
    {
        var entity = new LiabilityAccountEntry { LiabilityAccountId = accountId };
        ApplyEntry(entity, request);
        return entity;
    }

    private static void ApplyEntry(LiabilityAccountEntry entity, SaveLiabilityAccountEntryRequest request)
    {
        entity.Type = request.Type;
        entity.Date = request.Date;
        entity.Note = Clean(request.Note);
        entity.Amount = request.Amount;
    }

    private static LiabilityAccountDto Map(LiabilityAccount x)
    {
        var amount = ComputeBalance(x.Entries);
        var last = x.Entries.Count == 0 ? (DateOnly?)null : x.Entries.Max(e => e.Date);
        return new LiabilityAccountDto(x.Id, x.Category, x.Name, x.Description, x.CreationDate, x.Status, x.CurrencyCode, amount, last);
    }

    private void AddAudit(Guid accountId, string entityType, Guid entityId, InvestmentAuditAction action, string? oldValues, string? newValues, IEnumerable<string>? changedFields)
    {
        _db.LiabilityAccountAuditEntries.Add(new LiabilityAccountAuditEntry
        {
            OwnerUserId = OwnerId,
            LiabilityAccountId = accountId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            ChangedFieldsJson = changedFields is null ? null : JsonSerializer.Serialize(changedFields)
        });
    }

    private static string Label(LiabilityAccountCategory category) => category switch
    {
        LiabilityAccountCategory.Loan => "Loan",
        LiabilityAccountCategory.Debt => "Debt",
        _ => "Credit card"
    };

    private static string Snapshot(LiabilityAccount x) => JsonSerializer.Serialize(new
        { x.Id, x.Category, x.Name, x.Description, x.CreationDate, x.Status, x.CurrencyCode });
    private static string Snapshot(LiabilityAccountEntry x) => JsonSerializer.Serialize(new
        { x.Id, x.LiabilityAccountId, x.Type, x.Date, x.Note, x.Amount });
    private static string Snapshot(LiabilityAccountStatusHistory x) => JsonSerializer.Serialize(new
        { x.Id, x.LiabilityAccountId, x.Status, x.EffectiveDate });
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static LiabilityAccountEntryDto MapEntry(LiabilityAccountEntry x, decimal runningBalance) =>
        new(x.Id, x.Type, x.Date, x.Note, x.Amount, runningBalance);
    private static LiabilityAccountStatusDto MapStatus(LiabilityAccountStatusHistory x) => new(x.Id, x.Status, x.EffectiveDate);
}
