using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.AssetTracker.PreciousMetals;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Domain.AssetTracker;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.AssetTracker.PreciousMetals;

public class PreciousMetalService : IPreciousMetalService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public PreciousMetalService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId ?? throw new InvalidOperationException("No authenticated user.");
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public async Task<AssetTrackerPage<PreciousMetalDto>> ListAsync(PreciousMetalQuery query, CancellationToken ct)
    {
        await EnsureDefaultsAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var q = _db.PreciousMetals.AsNoTracking()
            .Include(x => x.Transactions)
            .Include(x => x.PriceHistory)
            .Where(x => x.OwnerUserId == OwnerId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(x => x.Name.Contains(search) || (x.Description != null && x.Description.Contains(search)));
        }

        var rows = (await q.ToListAsync(ct)).Select(Map).ToList();
        rows = (query.SortBy, query.SortDirection) switch
        {
            (PreciousMetalSort.CreationDate, AssetSortDirection.Asc) => rows.OrderBy(x => x.CreationDate).ThenBy(x => x.Name).ToList(),
            (PreciousMetalSort.CreationDate, AssetSortDirection.Desc) => rows.OrderByDescending(x => x.CreationDate).ThenBy(x => x.Name).ToList(),
            (PreciousMetalSort.Quantity, AssetSortDirection.Asc) => rows.OrderBy(x => x.Quantity).ThenBy(x => x.Name).ToList(),
            (PreciousMetalSort.Quantity, AssetSortDirection.Desc) => rows.OrderByDescending(x => x.Quantity).ThenBy(x => x.Name).ToList(),
            (PreciousMetalSort.CurrentValue, AssetSortDirection.Asc) => rows.OrderBy(x => x.CurrentValue).ThenBy(x => x.Name).ToList(),
            (PreciousMetalSort.CurrentValue, AssetSortDirection.Desc) => rows.OrderByDescending(x => x.CurrentValue).ThenBy(x => x.Name).ToList(),
            (_, AssetSortDirection.Desc) => rows.OrderByDescending(x => x.Name).ToList(),
            _ => rows.OrderBy(x => x.Name).ToList()
        };
        return new AssetTrackerPage<PreciousMetalDto>(rows.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, rows.Count);
    }

    public async Task<PreciousMetalDto> CreateAsync(SavePreciousMetalRequest request, CancellationToken ct)
    {
        ValidateMetal(request.Name, request.Description, null);
        var entity = new PreciousMetal
        {
            OwnerUserId = OwnerId,
            Name = request.Name.Trim(),
            Description = Clean(request.Description),
            CreationDate = Today,
            CurrencyCode = "INR"
        };
        _db.PreciousMetals.Add(entity);
        AddAudit(entity.Id, "PreciousMetal", entity.Id, InvestmentAuditAction.Create, null, Snapshot(entity), null);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<PreciousMetalDto> UpdateAsync(Guid id, SavePreciousMetalRequest request, CancellationToken ct)
    {
        var entity = await MetalQuery(id).FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Precious metal not found.");
        ValidateMetal(request.Name, request.Description, request.CreationDate);
        var creationDate = request.CreationDate ?? entity.CreationDate;
        var earliestEntry = entity.Transactions.OrderBy(x => x.Date).FirstOrDefault()?.Date;
        if (earliestEntry.HasValue && creationDate > earliestEntry.Value)
            throw new InvalidOperationException("Creation date cannot be after an existing history entry.");

        var old = Snapshot(entity);
        var changed = new List<string>();
        if (entity.Name != request.Name.Trim()) changed.Add("name");
        if (entity.Description != Clean(request.Description)) changed.Add("description");
        if (entity.CreationDate != creationDate) changed.Add("creationDate");
        entity.Name = request.Name.Trim();
        entity.Description = Clean(request.Description);
        entity.CreationDate = creationDate;
        if (changed.Count > 0) AddAudit(id, "PreciousMetal", id, InvestmentAuditAction.Update, old, Snapshot(entity), changed);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await MetalQuery(id).FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Precious metal not found.");
        var snapshot = JsonSerializer.Serialize(new
        {
            Metal = Snapshot(entity),
            Entries = entity.Transactions.Select(Snapshot),
            Prices = entity.PriceHistory.Select(Snapshot)
        });
        AddAudit(id, "PreciousMetal", id, InvestmentAuditAction.Delete, snapshot, null, null);
        _db.PreciousMetalTransactions.RemoveRange(entity.Transactions);
        _db.PreciousMetalPriceHistory.RemoveRange(entity.PriceHistory);
        _db.PreciousMetals.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AssetTrackerPage<PreciousMetalEntryDto>> GetEntriesAsync(Guid id, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct)
    {
        await EnsureMetalAsync(id, ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var q = _db.PreciousMetalTransactions.AsNoTracking().Where(x => x.PreciousMetalId == id && x.OwnerUserId == OwnerId);
        if (from.HasValue) q = q.Where(x => x.Date >= from.Value);
        if (to.HasValue) q = q.Where(x => x.Date <= to.Value);
        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(x => x.Date).ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new AssetTrackerPage<PreciousMetalEntryDto>(rows.Select(MapEntry).ToList(), page, pageSize, total);
    }

    public async Task<PreciousMetalEntryDto> AddEntryAsync(Guid id, SavePreciousMetalEntryRequest request, CancellationToken ct)
    {
        var metal = await EnsureMetalAsync(id, ct);
        ValidateEntry(metal, request);
        var entity = NewEntry(id, request);
        entity.OwnerUserId = OwnerId;
        var ledger = await _db.PreciousMetalTransactions.Where(x => x.PreciousMetalId == id).ToListAsync(ct);
        ledger.Add(entity);
        EnsureValidLedger(ledger);
        _db.PreciousMetalTransactions.Add(entity);
        AddAudit(id, "Entry", entity.Id, InvestmentAuditAction.Create, null, Snapshot(entity), null);
        await _db.SaveChangesAsync(ct);
        return MapEntry(entity);
    }

    public async Task<PreciousMetalEntryDto> UpdateEntryAsync(Guid id, Guid entryId, SavePreciousMetalEntryRequest request, CancellationToken ct)
    {
        var metal = await EnsureMetalAsync(id, ct);
        ValidateEntry(metal, request);
        var entity = await _db.PreciousMetalTransactions.FirstOrDefaultAsync(x => x.Id == entryId && x.PreciousMetalId == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Precious metal entry not found.");
        var old = Snapshot(entity);
        ApplyEntry(entity, request);
        var ledger = await _db.PreciousMetalTransactions.Where(x => x.PreciousMetalId == id).ToListAsync(ct);
        EnsureValidLedger(ledger);
        AddAudit(id, "Entry", entity.Id, InvestmentAuditAction.Update, old, Snapshot(entity), new[] { "type", "date", "note", "quantity", "pricePerUnit" });
        await _db.SaveChangesAsync(ct);
        return MapEntry(entity);
    }

    public async Task DeleteEntryAsync(Guid id, Guid entryId, CancellationToken ct)
    {
        await EnsureMetalAsync(id, ct);
        var entity = await _db.PreciousMetalTransactions.FirstOrDefaultAsync(x => x.Id == entryId && x.PreciousMetalId == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Precious metal entry not found.");
        var ledger = await _db.PreciousMetalTransactions.Where(x => x.PreciousMetalId == id && x.Id != entryId).ToListAsync(ct);
        EnsureValidLedger(ledger);
        AddAudit(id, "Entry", entity.Id, InvestmentAuditAction.Delete, Snapshot(entity), null, null);
        _db.PreciousMetalTransactions.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AssetTrackerPage<PreciousMetalPriceDto>> GetPricesAsync(Guid id, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct)
    {
        await EnsureMetalAsync(id, ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var q = _db.PreciousMetalPriceHistory.AsNoTracking().Where(x => x.PreciousMetalId == id && x.OwnerUserId == OwnerId);
        if (from.HasValue) q = q.Where(x => x.AsOf >= from.Value);
        if (to.HasValue) q = q.Where(x => x.AsOf <= to.Value);
        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(x => x.AsOf).ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new AssetTrackerPage<PreciousMetalPriceDto>(rows.Select(MapPrice).ToList(), page, pageSize, total);
    }

    public async Task<PreciousMetalPriceDto> AddPriceAsync(Guid id, SavePreciousMetalPriceRequest request, CancellationToken ct)
    {
        var metal = await EnsureMetalAsync(id, ct);
        ValidatePrice(request, metal.CreationDate);
        if (await _db.PreciousMetalPriceHistory.AnyAsync(x => x.PreciousMetalId == id && x.AsOf == request.Date, ct))
            throw new InvalidOperationException("A price entry already exists for this date.");
        var entity = new PreciousMetalPriceHistory { OwnerUserId = OwnerId, PreciousMetalId = id, AsOf = request.Date, PricePerUnit = request.PricePerUnit };
        _db.PreciousMetalPriceHistory.Add(entity);
        AddAudit(id, "Price", entity.Id, InvestmentAuditAction.Create, null, Snapshot(entity), null);
        await _db.SaveChangesAsync(ct);
        return MapPrice(entity);
    }

    public async Task DeletePriceAsync(Guid id, Guid priceId, CancellationToken ct)
    {
        await EnsureMetalAsync(id, ct);
        var entity = await _db.PreciousMetalPriceHistory.FirstOrDefaultAsync(x => x.Id == priceId && x.PreciousMetalId == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Price entry not found.");
        AddAudit(id, "Price", entity.Id, InvestmentAuditAction.Delete, Snapshot(entity), null, null);
        _db.PreciousMetalPriceHistory.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PreciousMetalStatisticsDto> GetStatisticsAsync(Guid id, PreciousMetalStatisticsSource source, PreciousMetalStatisticsDuration duration, CancellationToken ct)
    {
        var metal = await EnsureMetalAsync(id, ct);
        var from = duration switch
        {
            PreciousMetalStatisticsDuration.OneMonth => Today.AddMonths(-1),
            PreciousMetalStatisticsDuration.ThreeMonths => Today.AddMonths(-3),
            PreciousMetalStatisticsDuration.SixMonths => Today.AddMonths(-6),
            PreciousMetalStatisticsDuration.OneYear => Today.AddYears(-1),
            PreciousMetalStatisticsDuration.ThreeYears => Today.AddYears(-3),
            _ => Today.AddYears(-5)
        };
        if (source == PreciousMetalStatisticsSource.Prices)
        {
            var prices = await _db.PreciousMetalPriceHistory.AsNoTracking()
                .Where(x => x.PreciousMetalId == id && x.AsOf >= from && x.AsOf <= Today)
                .OrderBy(x => x.AsOf)
                .Select(x => new PreciousMetalStatisticsPoint(x.AsOf, x.PricePerUnit)).ToListAsync(ct);
            return new PreciousMetalStatisticsDto("Price per gram", metal.CurrencyCode, prices);
        }

        var entries = await _db.PreciousMetalTransactions.AsNoTracking().Where(x => x.PreciousMetalId == id && x.Date <= Today)
            .OrderBy(x => x.Date).ThenBy(x => x.CreatedAt).ToListAsync(ct);
        decimal running = 0;
        var points = new List<PreciousMetalStatisticsPoint>();
        foreach (var entry in entries)
        {
            running += entry.Type == PreciousMetalTxType.Buy ? entry.Quantity : -entry.Quantity;
            if (entry.Date >= from) points.Add(new PreciousMetalStatisticsPoint(entry.Date, running));
        }
        return new PreciousMetalStatisticsDto("Quantity held", metal.CurrencyCode, points);
    }

    public async Task<IReadOnlyList<PreciousMetalExportRow>> GetExportAsync(PreciousMetalExportRequest request, CancellationToken ct)
    {
        if (request.PreciousMetalIds.Count == 0) throw new ArgumentException("Select at least one precious metal.");
        if (request.To < request.From) throw new ArgumentException("The end date must be on or after the start date.");
        if (request.To > request.From.AddYears(3)) throw new ArgumentException("Export duration cannot exceed three years.");
        var metals = await _db.PreciousMetals.AsNoTracking().Include(x => x.Transactions).Include(x => x.PriceHistory)
            .Where(x => x.OwnerUserId == OwnerId && request.PreciousMetalIds.Contains(x.Id)).ToListAsync(ct);
        if (metals.Count != request.PreciousMetalIds.Distinct().Count()) throw new KeyNotFoundException("One or more precious metals were not found.");
        var rows = new List<PreciousMetalExportRow>();
        foreach (var metal in metals.OrderBy(x => x.Name))
        {
            rows.AddRange(metal.Transactions.Where(x => x.Date >= request.From && x.Date <= request.To)
                .Select(x => new PreciousMetalExportRow(metal.Name, x.Type.ToString(), x.Date, metal.CurrencyCode,
                    x.Note ?? string.Empty, x.Quantity, x.PricePerUnit, x.Quantity * x.PricePerUnit)));
            rows.AddRange(metal.PriceHistory.Where(x => x.AsOf >= request.From && x.AsOf <= request.To)
                .Select(x => new PreciousMetalExportRow(metal.Name, "Price", x.AsOf, metal.CurrencyCode,
                    "Price per gram", null, x.PricePerUnit, null)));
        }
        return rows.OrderBy(x => x.Metal).ThenBy(x => x.Date).ToList();
    }

    private IQueryable<PreciousMetal> MetalQuery(Guid id) => _db.PreciousMetals.Include(x => x.Transactions).Include(x => x.PriceHistory)
        .Where(x => x.Id == id && x.OwnerUserId == OwnerId);

    private async Task<PreciousMetal> EnsureMetalAsync(Guid id, CancellationToken ct) =>
        await _db.PreciousMetals.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
        ?? throw new KeyNotFoundException("Precious metal not found.");

    private async Task EnsureDefaultsAsync(CancellationToken ct)
    {
        var owner = OwnerId;
        var existing = await _db.PreciousMetals.Where(x => x.OwnerUserId == owner && x.IsDefault).Select(x => x.Name).ToListAsync(ct);
        var defaults = new[] { "Gold 24K", "Silver" };
        foreach (var name in defaults.Where(x => !existing.Contains(x)))
        {
            var entity = new PreciousMetal { OwnerUserId = owner, Name = name, CreationDate = Today, CurrencyCode = "INR", IsDefault = true };
            _db.PreciousMetals.Add(entity);
            AddAudit(entity.Id, "PreciousMetal", entity.Id, InvestmentAuditAction.Create, null, Snapshot(entity), null);
        }
        await _db.SaveChangesAsync(ct);
    }

    private static void ValidateMetal(string name, string? description, DateOnly? creationDate)
    {
        var n = name?.Trim() ?? string.Empty;
        if (n.Length is < 3 or > 30) throw new ArgumentException("Name must contain between 3 and 30 characters.");
        if ((description?.Trim().Length ?? 0) > 200) throw new ArgumentException("Description must not exceed 200 characters.");
        if (creationDate.HasValue && creationDate.Value > Today) throw new ArgumentException("Creation date cannot be in the future.");
    }

    private static void ValidateDate(DateOnly date, DateOnly creationDate)
    {
        if (date < creationDate) throw new ArgumentException("Date cannot be before the precious metal creation date.");
        if (date > Today) throw new ArgumentException("Date cannot be in the future.");
    }

    private static void ValidateEntry(PreciousMetal metal, SavePreciousMetalEntryRequest request)
    {
        if (!Enum.IsDefined(request.Type)) throw new ArgumentException("Type is required.");
        ValidateDate(request.Date, metal.CreationDate);
        if ((request.Note?.Trim().Length ?? 0) > 200) throw new ArgumentException("Note must not exceed 200 characters.");
        if (request.Quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");
        if (request.PricePerUnit <= 0) throw new ArgumentException("Price per unit must be greater than zero.");
    }

    private static void ValidatePrice(SavePreciousMetalPriceRequest request, DateOnly creationDate)
    {
        ValidateDate(request.Date, creationDate);
        if (request.PricePerUnit < 0) throw new ArgumentException("Price per unit cannot be negative.");
    }

    private static void EnsureValidLedger(IEnumerable<PreciousMetalTransaction> entries)
    {
        decimal quantity = 0;
        foreach (var entry in entries.OrderBy(x => x.Date).ThenBy(x => x.CreatedAt).ThenBy(x => x.Id))
        {
            quantity += entry.Type == PreciousMetalTxType.Buy ? entry.Quantity : -entry.Quantity;
            if (quantity < 0) throw new InvalidOperationException($"The entry would make precious metal quantity negative on {entry.Date:yyyy-MM-dd}.");
        }
    }

    private static PreciousMetalTransaction NewEntry(Guid metalId, SavePreciousMetalEntryRequest request)
    {
        var entity = new PreciousMetalTransaction { PreciousMetalId = metalId };
        ApplyEntry(entity, request);
        return entity;
    }

    private static void ApplyEntry(PreciousMetalTransaction entity, SavePreciousMetalEntryRequest request)
    {
        entity.Type = request.Type;
        entity.Date = request.Date;
        entity.Note = Clean(request.Note);
        entity.Quantity = request.Quantity;
        entity.PricePerUnit = request.PricePerUnit;
    }

    private static PreciousMetalDto Map(PreciousMetal entity)
    {
        var quantity = entity.Transactions.Sum(x => x.Type == PreciousMetalTxType.Buy ? x.Quantity : -x.Quantity);
        var latest = entity.PriceHistory.OrderByDescending(x => x.AsOf).ThenByDescending(x => x.CreatedAt).FirstOrDefault();
        var price = latest?.PricePerUnit;
        return new PreciousMetalDto(entity.Id, entity.Name, entity.Description, entity.CreationDate, entity.CurrencyCode,
            entity.IsDefault, quantity, price, quantity * (price ?? 0));
    }

    private void AddAudit(Guid metalId, string entityType, Guid entityId, InvestmentAuditAction action, string? oldValues, string? newValues, IEnumerable<string>? changedFields)
    {
        _db.PreciousMetalAuditEntries.Add(new PreciousMetalAuditEntry
        {
            OwnerUserId = OwnerId,
            PreciousMetalId = metalId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            ChangedFieldsJson = changedFields is null ? null : JsonSerializer.Serialize(changedFields)
        });
    }

    private static string Snapshot(PreciousMetal x) => JsonSerializer.Serialize(new
        { x.Id, x.Name, x.Description, x.CreationDate, x.CurrencyCode, x.IsDefault });
    private static string Snapshot(PreciousMetalTransaction x) => JsonSerializer.Serialize(new
        { x.Id, x.PreciousMetalId, x.Type, x.Date, x.Note, x.Quantity, x.PricePerUnit });
    private static string Snapshot(PreciousMetalPriceHistory x) => JsonSerializer.Serialize(new
        { x.Id, x.PreciousMetalId, Date = x.AsOf, x.PricePerUnit });
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static PreciousMetalEntryDto MapEntry(PreciousMetalTransaction x) => new(x.Id, x.Type, x.Date, x.Note, x.Quantity, x.PricePerUnit, x.Quantity * x.PricePerUnit);
    private static PreciousMetalPriceDto MapPrice(PreciousMetalPriceHistory x) => new(x.Id, x.AsOf, x.PricePerUnit);
}
