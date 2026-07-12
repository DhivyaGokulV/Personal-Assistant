using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.AssetTracker.Common;
using PersonalAssistant.Application.AssetTracker.Investments;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Domain.AssetTracker;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.AssetTracker.Investments;

public class InvestmentService : IInvestmentService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public InvestmentService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId ?? throw new InvalidOperationException("No authenticated user.");
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public async Task<InvestmentPage<InvestmentDto>> ListAsync(InvestmentQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var owner = OwnerId;
        var q = _db.Investments
            .AsNoTracking()
            .Include(x => x.Tag)
            .Include(x => x.Transactions)
            .Include(x => x.PriceHistory)
            .Where(x => x.OwnerUserId == owner);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(x => x.Name.Contains(search) || (x.Description != null && x.Description.Contains(search)));
        }
        if (query.Status.HasValue) q = q.Where(x => x.Status == query.Status);
        if (query.Type.HasValue) q = q.Where(x => x.InvestmentType == query.Type);
        if (query.TagId.HasValue) q = q.Where(x => x.TagId == query.TagId);
        if (!string.IsNullOrWhiteSpace(query.Currency))
        {
            var currency = query.Currency.Trim().ToUpperInvariant();
            q = q.Where(x => x.CurrencyCode == currency);
        }

        var rows = (await q.ToListAsync(ct)).Select(Map).ToList();
        rows = (query.SortBy, query.SortDirection) switch
        {
            (InvestmentSort.CreationDate, SortDirection.Asc) => rows.OrderBy(x => x.CreationDate).ThenBy(x => x.Name).ToList(),
            (InvestmentSort.CreationDate, SortDirection.Desc) => rows.OrderByDescending(x => x.CreationDate).ThenBy(x => x.Name).ToList(),
            (InvestmentSort.CurrentValue, SortDirection.Asc) => rows.OrderBy(x => x.CurrentValue).ThenBy(x => x.Name).ToList(),
            (InvestmentSort.CurrentValue, SortDirection.Desc) => rows.OrderByDescending(x => x.CurrentValue).ThenBy(x => x.Name).ToList(),
            (InvestmentSort.ProfitLossPercent, SortDirection.Asc) => rows.OrderBy(x => x.ProfitLossPercent).ThenBy(x => x.Name).ToList(),
            (InvestmentSort.ProfitLossPercent, SortDirection.Desc) => rows.OrderByDescending(x => x.ProfitLossPercent).ThenBy(x => x.Name).ToList(),
            (InvestmentSort.Status, SortDirection.Asc) => rows.OrderBy(x => x.Status).ThenBy(x => x.Name).ToList(),
            (InvestmentSort.Status, SortDirection.Desc) => rows.OrderByDescending(x => x.Status).ThenBy(x => x.Name).ToList(),
            (_, SortDirection.Desc) => rows.OrderByDescending(x => x.Name).ToList(),
            _ => rows.OrderBy(x => x.Name).ToList()
        };

        var total = rows.Count;
        return new InvestmentPage<InvestmentDto>(rows.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, total);
    }

    public async Task<InvestmentDetailDto> GetAsync(Guid id, CancellationToken ct)
    {
        var entity = await InvestmentQuery(id).Include(x => x.StatusHistory).FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Investment not found.");
        return new InvestmentDetailDto(Map(entity), entity.StatusHistory
            .OrderByDescending(x => x.EffectiveDate).ThenByDescending(x => x.CreatedAt)
            .Select(MapStatus).ToList());
    }

    public async Task<InvestmentDto> CreateAsync(CreateInvestmentRequest request, CancellationToken ct)
    {
        ValidateInvestment(request.Name, request.Description, request.CurrencyCode, request.CreationDate);
        if (!Enum.IsDefined(request.InvestmentType)) throw new ArgumentException("Investment type is required.");
        if (request.TagId.HasValue && !string.IsNullOrWhiteSpace(request.NewTagName))
            throw new ArgumentException("Select an existing tag or create a new tag, not both.");

        var owner = OwnerId;
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        Guid? tagId = request.TagId;
        if (tagId.HasValue)
        {
            if (!await _db.AssetTags.AnyAsync(x => x.Id == tagId && x.OwnerUserId == owner, ct))
                throw new KeyNotFoundException("Tag not found.");
        }
        else if (!string.IsNullOrWhiteSpace(request.NewTagName))
        {
            var tagName = request.NewTagName.Trim();
            if (tagName.Length > 50) throw new ArgumentException("Tag must not exceed 50 characters.");
            var existing = await _db.AssetTags.FirstOrDefaultAsync(x => x.OwnerUserId == owner && x.Name == tagName, ct);
            if (existing is not null) tagId = existing.Id;
            else
            {
                var tag = new AssetTag { OwnerUserId = owner, Name = tagName, Color = "#6c5ce7" };
                _db.AssetTags.Add(tag);
                tagId = tag.Id;
            }
        }

        var groupId = await GetLegacyGroupIdAsync(owner, ct);
        var entity = new Investment
        {
            OwnerUserId = owner,
            GroupId = groupId,
            Name = request.Name.Trim(),
            Description = Clean(request.Description),
            TagId = tagId,
            InvestmentType = request.InvestmentType,
            CurrencyCode = "INR",
            CreationDate = request.CreationDate,
            Unit = "unit",
            Status = InvestmentStatus.Active
        };
        var status = new InvestmentStatusHistory
        {
            OwnerUserId = owner,
            InvestmentId = entity.Id,
            Status = InvestmentStatus.Active,
            EffectiveDate = request.CreationDate
        };
        entity.StatusHistory.Add(status);
        _db.Investments.Add(entity);
        AddAudit(entity.Id, "Investment", entity.Id, InvestmentAuditAction.Create, null, Snapshot(entity), null);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (await GetAsync(entity.Id, ct)).Investment;
    }

    public async Task<InvestmentDto> UpdateAsync(Guid id, UpdateInvestmentRequest request, CancellationToken ct)
    {
        ValidateInvestment(request.Name, request.Description, "INR", null);
        var entity = await InvestmentQuery(id).FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Investment not found.");
        await ValidateTagAsync(request.TagId, ct);
        var old = Snapshot(entity);
        var changed = new List<string>();
        if (entity.Name != request.Name.Trim()) changed.Add("name");
        if (entity.Description != Clean(request.Description)) changed.Add("description");
        if (entity.TagId != request.TagId) changed.Add("tagId");
        entity.Name = request.Name.Trim();
        entity.Description = Clean(request.Description);
        entity.TagId = request.TagId;
        if (changed.Count > 0)
        {
            AddAudit(id, "Investment", id, InvestmentAuditAction.Update, old, Snapshot(entity), changed);
            await _db.SaveChangesAsync(ct);
        }
        return (await GetAsync(id, ct)).Investment;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await InvestmentQuery(id).Include(x => x.StatusHistory).FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Investment not found.");
        var snapshot = JsonSerializer.Serialize(new
        {
            Investment = Snapshot(entity),
            Entries = entity.Transactions.Select(Snapshot),
            Prices = entity.PriceHistory.Select(Snapshot),
            Statuses = entity.StatusHistory.Select(Snapshot)
        });
        AddAudit(id, "Investment", id, InvestmentAuditAction.Delete, snapshot, null, null);
        _db.InvestmentTransactions.RemoveRange(entity.Transactions);
        _db.InvestmentPriceHistory.RemoveRange(entity.PriceHistory);
        _db.InvestmentStatusHistory.RemoveRange(entity.StatusHistory);
        _db.Investments.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<InvestmentStatusDto>> GetStatusHistoryAsync(Guid id, CancellationToken ct)
    {
        await EnsureInvestmentAsync(id, ct);
        return await _db.InvestmentStatusHistory.AsNoTracking()
            .Where(x => x.InvestmentId == id && x.OwnerUserId == OwnerId)
            .OrderByDescending(x => x.EffectiveDate).ThenByDescending(x => x.CreatedAt)
            .Select(x => new InvestmentStatusDto(x.Id, x.Status, x.EffectiveDate)).ToListAsync(ct);
    }

    public async Task<InvestmentStatusDto> ChangeStatusAsync(Guid id, ChangeInvestmentStatusRequest request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Status)) throw new ArgumentException("Status is required.");
        var entity = await EnsureInvestmentAsync(id, ct);
        ValidateDate(request.EffectiveDate, entity.CreationDate);
        if (await _db.InvestmentStatusHistory.AnyAsync(x => x.InvestmentId == id && x.EffectiveDate == request.EffectiveDate, ct))
            throw new InvalidOperationException("A status entry already exists for this date.");
        var status = new InvestmentStatusHistory
        {
            OwnerUserId = OwnerId, InvestmentId = id, Status = request.Status, EffectiveDate = request.EffectiveDate
        };
        _db.InvestmentStatusHistory.Add(status);
        AddAudit(id, "Status", status.Id, InvestmentAuditAction.StatusChange, null, Snapshot(status), null);
        var latest = await _db.InvestmentStatusHistory.Where(x => x.InvestmentId == id)
            .OrderByDescending(x => x.EffectiveDate).ThenByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        if (latest is null || request.EffectiveDate >= latest.EffectiveDate) entity.Status = request.Status;
        await _db.SaveChangesAsync(ct);
        return MapStatus(status);
    }

    public async Task<InvestmentPage<InvestmentEntryDto>> GetEntriesAsync(Guid id, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct)
    {
        await EnsureInvestmentAsync(id, ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var q = _db.InvestmentTransactions.AsNoTracking().Where(x => x.InvestmentId == id && x.OwnerUserId == OwnerId);
        if (from.HasValue) q = q.Where(x => x.Date >= from);
        if (to.HasValue) q = q.Where(x => x.Date <= to);
        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(x => x.Date).ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new InvestmentPage<InvestmentEntryDto>(rows.Select(MapEntry).ToList(), page, pageSize, total);
    }

    public async Task<InvestmentEntryDto> AddEntryAsync(Guid id, SaveInvestmentEntryRequest request, CancellationToken ct)
    {
        var investment = await EnsureInvestmentAsync(id, ct);
        ValidateEntry(investment, request);
        var entity = NewEntry(id, request);
        entity.OwnerUserId = OwnerId;
        var ledger = await _db.InvestmentTransactions.Where(x => x.InvestmentId == id).ToListAsync(ct);
        ledger.Add(entity);
        EnsureValidLedger(investment, ledger);
        _db.InvestmentTransactions.Add(entity);
        AddAudit(id, "Entry", entity.Id, InvestmentAuditAction.Create, null, Snapshot(entity), null);
        await _db.SaveChangesAsync(ct);
        return MapEntry(entity);
    }

    public async Task<InvestmentEntryDto> UpdateEntryAsync(Guid id, Guid entryId, SaveInvestmentEntryRequest request, CancellationToken ct)
    {
        var investment = await EnsureInvestmentAsync(id, ct);
        ValidateEntry(investment, request);
        var entity = await _db.InvestmentTransactions.FirstOrDefaultAsync(x => x.Id == entryId && x.InvestmentId == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Investment entry not found.");
        var old = Snapshot(entity);
        ApplyEntry(entity, request);
        var ledger = await _db.InvestmentTransactions.Where(x => x.InvestmentId == id).ToListAsync(ct);
        EnsureValidLedger(investment, ledger);
        AddAudit(id, "Entry", entity.Id, InvestmentAuditAction.Update, old, Snapshot(entity),
            new[] { "type", "date", "note", "quantity", "pricePerUnit", "amount" });
        await _db.SaveChangesAsync(ct);
        return MapEntry(entity);
    }

    public async Task DeleteEntryAsync(Guid id, Guid entryId, CancellationToken ct)
    {
        var investment = await EnsureInvestmentAsync(id, ct);
        var entity = await _db.InvestmentTransactions.FirstOrDefaultAsync(x => x.Id == entryId && x.InvestmentId == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Investment entry not found.");
        var ledger = await _db.InvestmentTransactions.Where(x => x.InvestmentId == id && x.Id != entryId).ToListAsync(ct);
        EnsureValidLedger(investment, ledger);
        AddAudit(id, "Entry", entity.Id, InvestmentAuditAction.Delete, Snapshot(entity), null, null);
        _db.InvestmentTransactions.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<InvestmentPage<InvestmentPriceHistoryDto>> GetPricesAsync(Guid id, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct)
    {
        var investment = await EnsureInvestmentAsync(id, ct);
        EnsureUnitBased(investment);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var q = _db.InvestmentPriceHistory.AsNoTracking().Where(x => x.InvestmentId == id && x.OwnerUserId == OwnerId);
        if (from.HasValue) q = q.Where(x => x.AsOf >= from);
        if (to.HasValue) q = q.Where(x => x.AsOf <= to);
        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(x => x.AsOf).ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new InvestmentPage<InvestmentPriceHistoryDto>(rows.Select(MapPrice).ToList(), page, pageSize, total);
    }

    public async Task<InvestmentPriceHistoryDto> AddPriceAsync(Guid id, SaveInvestmentPriceRequest request, CancellationToken ct)
    {
        var investment = await EnsureInvestmentAsync(id, ct);
        EnsureUnitBased(investment);
        ValidatePrice(request, investment.CreationDate);
        if (await _db.InvestmentPriceHistory.AnyAsync(x => x.InvestmentId == id && x.AsOf == request.Date, ct))
            throw new InvalidOperationException("A price entry already exists for this date.");
        var entity = new InvestmentPriceHistory
        {
            OwnerUserId = OwnerId, InvestmentId = id, AsOf = request.Date, Price = request.PricePerUnit
        };
        _db.InvestmentPriceHistory.Add(entity);
        AddAudit(id, "Price", entity.Id, InvestmentAuditAction.Create, null, Snapshot(entity), null);
        await _db.SaveChangesAsync(ct);
        return MapPrice(entity);
    }

    public async Task<InvestmentPriceHistoryDto> UpdatePriceAsync(Guid id, Guid priceId, SaveInvestmentPriceRequest request, CancellationToken ct)
    {
        var investment = await EnsureInvestmentAsync(id, ct);
        EnsureUnitBased(investment);
        ValidatePrice(request, investment.CreationDate);
        if (await _db.InvestmentPriceHistory.AnyAsync(x => x.InvestmentId == id && x.AsOf == request.Date && x.Id != priceId, ct))
            throw new InvalidOperationException("A price entry already exists for this date.");
        var entity = await _db.InvestmentPriceHistory.FirstOrDefaultAsync(x => x.Id == priceId && x.InvestmentId == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Price entry not found.");
        var old = Snapshot(entity);
        entity.AsOf = request.Date;
        entity.Price = request.PricePerUnit;
        AddAudit(id, "Price", entity.Id, InvestmentAuditAction.Update, old, Snapshot(entity), new[] { "date", "pricePerUnit" });
        await _db.SaveChangesAsync(ct);
        return MapPrice(entity);
    }

    public async Task DeletePriceAsync(Guid id, Guid priceId, CancellationToken ct)
    {
        var investment = await EnsureInvestmentAsync(id, ct);
        EnsureUnitBased(investment);
        var entity = await _db.InvestmentPriceHistory.FirstOrDefaultAsync(x => x.Id == priceId && x.InvestmentId == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Price entry not found.");
        AddAudit(id, "Price", entity.Id, InvestmentAuditAction.Delete, Snapshot(entity), null, null);
        _db.InvestmentPriceHistory.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<InvestmentStatisticsDto> GetStatisticsAsync(Guid id, StatisticsSource source, StatisticsDuration duration, CancellationToken ct)
    {
        var investment = await EnsureInvestmentAsync(id, ct);
        var from = duration switch
        {
            StatisticsDuration.OneMonth => Today.AddMonths(-1),
            StatisticsDuration.ThreeMonths => Today.AddMonths(-3),
            StatisticsDuration.SixMonths => Today.AddMonths(-6),
            StatisticsDuration.OneYear => Today.AddYears(-1),
            StatisticsDuration.ThreeYears => Today.AddYears(-3),
            _ => Today.AddYears(-5)
        };
        if (source == StatisticsSource.Prices)
        {
            EnsureUnitBased(investment);
            var prices = await _db.InvestmentPriceHistory.AsNoTracking()
                .Where(x => x.InvestmentId == id && x.AsOf >= from && x.AsOf <= Today)
                .OrderBy(x => x.AsOf).Select(x => new StatisticsPoint(x.AsOf, x.Price)).ToListAsync(ct);
            return new InvestmentStatisticsDto("Price per unit", investment.CurrencyCode, prices);
        }

        var entries = await _db.InvestmentTransactions.AsNoTracking().Where(x => x.InvestmentId == id && x.Date <= Today)
            .OrderBy(x => x.Date).ThenBy(x => x.CreatedAt).ToListAsync(ct);
        decimal running = 0;
        var points = new List<StatisticsPoint>();
        foreach (var entry in entries)
        {
            running += investment.InvestmentType == InvestmentType.UnitBased
                ? entry.Type == InvestmentTxType.Buy ? entry.Units : -entry.Units
                : entry.Type == InvestmentTxType.Credit ? entry.Amount ?? 0 : -(entry.Amount ?? 0);
            if (entry.Date >= from) points.Add(new StatisticsPoint(entry.Date, running));
        }
        return new InvestmentStatisticsDto(
            investment.InvestmentType == InvestmentType.UnitBased ? "Units held" : "Investment balance",
            investment.CurrencyCode, points);
    }

    public async Task<IReadOnlyList<InvestmentExportRow>> GetExportAsync(InvestmentExportRequest request, CancellationToken ct)
    {
        if (request.InvestmentIds.Count == 0) throw new ArgumentException("Select at least one investment.");
        if (request.To < request.From) throw new ArgumentException("The end date must be on or after the start date.");
        if (request.To > request.From.AddYears(3)) throw new ArgumentException("Export duration cannot exceed three years.");
        var investments = await _db.Investments.AsNoTracking()
            .Include(x => x.Transactions).Include(x => x.PriceHistory).Include(x => x.StatusHistory)
            .Where(x => x.OwnerUserId == OwnerId && request.InvestmentIds.Contains(x.Id)).ToListAsync(ct);
        if (investments.Count != request.InvestmentIds.Distinct().Count())
            throw new KeyNotFoundException("One or more investments were not found.");

        var rows = new List<InvestmentExportRow>();
        foreach (var investment in investments.OrderBy(x => x.Name))
        {
            rows.AddRange(investment.Transactions.Where(x => x.Date >= request.From && x.Date <= request.To)
                .Select(x => new InvestmentExportRow(investment.Name, x.Type.ToString(), x.Date, investment.CurrencyCode,
                    x.Note ?? string.Empty, x.Type is InvestmentTxType.Buy or InvestmentTxType.Sell ? x.Units : null,
                    x.Type is InvestmentTxType.Buy or InvestmentTxType.Sell ? x.Price : null,
                    x.Type is InvestmentTxType.Credit or InvestmentTxType.Debit ? x.Amount : x.Units * x.Price)));
            rows.AddRange(investment.PriceHistory.Where(x => x.AsOf >= request.From && x.AsOf <= request.To)
                .Select(x => new InvestmentExportRow(investment.Name, "Price", x.AsOf, investment.CurrencyCode,
                    "Price per unit", null, x.Price, null)));
            rows.AddRange(investment.StatusHistory.Where(x => x.EffectiveDate >= request.From && x.EffectiveDate <= request.To)
                .Select(x => new InvestmentExportRow(investment.Name, "Status", x.EffectiveDate, investment.CurrencyCode,
                    x.Status.ToString(), null, null, null)));
        }
        return rows.OrderBy(x => x.Investment).ThenBy(x => x.Date).ToList();
    }

    private IQueryable<Investment> InvestmentQuery(Guid id) => _db.Investments
        .Include(x => x.Tag).Include(x => x.Transactions).Include(x => x.PriceHistory)
        .Where(x => x.Id == id && x.OwnerUserId == OwnerId);

    private async Task<Investment> EnsureInvestmentAsync(Guid id, CancellationToken ct) =>
        await _db.Investments.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
        ?? throw new KeyNotFoundException("Investment not found.");

    private async Task<Guid> GetLegacyGroupIdAsync(Guid owner, CancellationToken ct)
    {
        var group = await _db.InvestmentGroups.FirstOrDefaultAsync(x => x.OwnerUserId == owner && x.Name == "Ungrouped", ct);
        if (group is not null) return group.Id;
        group = new InvestmentGroup { OwnerUserId = owner, Name = "Ungrouped", Status = InvestmentStatus.Active };
        _db.InvestmentGroups.Add(group);
        return group.Id;
    }

    private async Task ValidateTagAsync(Guid? tagId, CancellationToken ct)
    {
        if (tagId.HasValue && !await _db.AssetTags.AnyAsync(x => x.Id == tagId && x.OwnerUserId == OwnerId, ct))
            throw new KeyNotFoundException("Tag not found.");
    }

    private static void ValidateInvestment(string name, string? description, string currency, DateOnly? creationDate)
    {
        var n = name?.Trim() ?? string.Empty;
        if (n.Length is < 2 or > 100) throw new ArgumentException("Name must contain between 2 and 100 characters.");
        if ((description?.Trim().Length ?? 0) > 500) throw new ArgumentException("Description must not exceed 500 characters.");
        if (!string.Equals(currency, "INR", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Only INR is currently supported.");
        if (creationDate.HasValue && creationDate.Value > Today) throw new ArgumentException("Creation date cannot be in the future.");
    }

    private static void ValidateDate(DateOnly date, DateOnly creationDate)
    {
        if (date < creationDate) throw new ArgumentException("Date cannot be before the investment creation date.");
        if (date > Today) throw new ArgumentException("Date cannot be in the future.");
    }

    private static void ValidateEntry(Investment investment, SaveInvestmentEntryRequest request)
    {
        ValidateDate(request.Date, investment.CreationDate);
        if ((request.Note?.Trim().Length ?? 0) > 200) throw new ArgumentException("Note must not exceed 200 characters.");
        if (investment.InvestmentType == InvestmentType.UnitBased)
        {
            if (request.Type is not (InvestmentTxType.Buy or InvestmentTxType.Sell))
                throw new ArgumentException("Unit-based investments support only Buy and Sell entries.");
            if (request.Quantity is null or <= 0) throw new ArgumentException("Quantity must be greater than zero.");
            if (request.PricePerUnit is null or <= 0) throw new ArgumentException("Price per unit must be greater than zero.");
        }
        else
        {
            if (request.Type is not (InvestmentTxType.Credit or InvestmentTxType.Debit))
                throw new ArgumentException("Amount-based investments support only Credit and Debit entries.");
            if (request.Amount is null or <= 0) throw new ArgumentException("Amount must be greater than zero.");
        }
    }

    private static void ValidatePrice(SaveInvestmentPriceRequest request, DateOnly creationDate)
    {
        ValidateDate(request.Date, creationDate);
        if (request.PricePerUnit < 0) throw new ArgumentException("Price per unit cannot be negative.");
    }

    private static void EnsureUnitBased(Investment investment)
    {
        if (investment.InvestmentType != InvestmentType.UnitBased)
            throw new InvalidOperationException("Price history is available only for unit-based investments.");
    }

    private static InvestmentTransaction NewEntry(Guid investmentId, SaveInvestmentEntryRequest request)
    {
        var entity = new InvestmentTransaction { InvestmentId = investmentId };
        ApplyEntry(entity, request);
        return entity;
    }

    private static void ApplyEntry(InvestmentTransaction entity, SaveInvestmentEntryRequest request)
    {
        entity.Type = request.Type;
        entity.Date = request.Date;
        entity.Note = Clean(request.Note);
        entity.Units = request.Quantity ?? 0;
        entity.Price = request.PricePerUnit ?? 0;
        entity.Amount = request.Amount;
    }

    private static void EnsureValidLedger(Investment investment, IEnumerable<InvestmentTransaction> entries)
    {
        decimal balance = 0;
        foreach (var entry in entries.OrderBy(x => x.Date).ThenBy(x => x.CreatedAt).ThenBy(x => x.Id))
        {
            balance += investment.InvestmentType == InvestmentType.UnitBased
                ? entry.Type == InvestmentTxType.Buy ? entry.Units : -entry.Units
                : entry.Type == InvestmentTxType.Credit ? entry.Amount ?? 0 : -(entry.Amount ?? 0);
            if (balance < 0) throw new InvalidOperationException(
                investment.InvestmentType == InvestmentType.UnitBased
                    ? $"The entry would make unit holdings negative on {entry.Date:yyyy-MM-dd}."
                    : $"The entry would make the investment balance negative on {entry.Date:yyyy-MM-dd}.");
        }
    }

    private static InvestmentDto Map(Investment entity)
    {
        var metrics = ComputeMetrics(entity);
        return new InvestmentDto(entity.Id, entity.Name, entity.Description,
            entity.Tag is null ? null : new AssetTagBadge(entity.Tag.Id, entity.Tag.Name, entity.Tag.Color),
            entity.InvestmentType, entity.CurrencyCode, entity.CreationDate, entity.Status,
            metrics.Units, metrics.AmountInvested, metrics.CurrentPrice, metrics.CurrentValue,
            metrics.CostBasis, metrics.ProfitLossPercent);
    }

    private static Metrics ComputeMetrics(Investment entity)
    {
        if (entity.InvestmentType == InvestmentType.AmountBased)
        {
            var amount = entity.Transactions.Sum(x => x.Type == InvestmentTxType.Credit ? x.Amount ?? 0 : -(x.Amount ?? 0));
            return new Metrics(0, amount, null, amount, amount, null);
        }
        decimal units = 0;
        decimal basis = 0;
        foreach (var entry in entity.Transactions.OrderBy(x => x.Date).ThenBy(x => x.CreatedAt))
        {
            if (entry.Type == InvestmentTxType.Buy)
            {
                units += entry.Units;
                basis += entry.Units * entry.Price;
            }
            else if (entry.Type == InvestmentTxType.Sell && units > 0)
            {
                var average = basis / units;
                units -= entry.Units;
                basis -= average * entry.Units;
                if (units == 0) basis = 0;
            }
        }
        var latest = entity.PriceHistory.OrderByDescending(x => x.AsOf).ThenByDescending(x => x.CreatedAt).FirstOrDefault();
        var currentPrice = latest?.Price;
        var value = units * (currentPrice ?? 0);
        decimal? percent = basis > 0 ? (value - basis) / basis * 100 : null;
        return new Metrics(units, basis, currentPrice, value, basis, percent);
    }

    private void AddAudit(Guid investmentId, string entityType, Guid entityId, InvestmentAuditAction action,
        string? oldValues, string? newValues, IEnumerable<string>? changedFields)
    {
        _db.InvestmentAuditEntries.Add(new InvestmentAuditEntry
        {
            OwnerUserId = OwnerId,
            InvestmentId = investmentId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            ChangedFieldsJson = changedFields is null ? null : JsonSerializer.Serialize(changedFields)
        });
    }

    private static string Snapshot(Investment x) => JsonSerializer.Serialize(new
        { x.Id, x.Name, x.Description, x.TagId, x.InvestmentType, x.CurrencyCode, x.CreationDate, x.Status });
    private static string Snapshot(InvestmentTransaction x) => JsonSerializer.Serialize(new
        { x.Id, x.InvestmentId, x.Type, x.Date, x.Note, Quantity = x.Units, PricePerUnit = x.Price, x.Amount });
    private static string Snapshot(InvestmentPriceHistory x) => JsonSerializer.Serialize(new
        { x.Id, x.InvestmentId, Date = x.AsOf, PricePerUnit = x.Price });
    private static string Snapshot(InvestmentStatusHistory x) => JsonSerializer.Serialize(new
        { x.Id, x.InvestmentId, x.Status, x.EffectiveDate });
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static InvestmentEntryDto MapEntry(InvestmentTransaction x) => new(x.Id, x.Date, x.Type, x.Note,
        x.Type is InvestmentTxType.Buy or InvestmentTxType.Sell ? x.Units : null,
        x.Type is InvestmentTxType.Buy or InvestmentTxType.Sell ? x.Price : null,
        x.Type is InvestmentTxType.Credit or InvestmentTxType.Debit ? x.Amount ?? 0 : x.Units * x.Price);
    private static InvestmentPriceHistoryDto MapPrice(InvestmentPriceHistory x) => new(x.Id, x.AsOf, x.Price);
    private static InvestmentStatusDto MapStatus(InvestmentStatusHistory x) => new(x.Id, x.Status, x.EffectiveDate);
    private record Metrics(decimal Units, decimal AmountInvested, decimal? CurrentPrice, decimal CurrentValue, decimal CostBasis, decimal? ProfitLossPercent);
}
