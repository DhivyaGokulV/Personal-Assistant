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

    // ===== Groups =====
    public async Task<IReadOnlyList<InvestmentGroupDto>> GetGroupsAsync(InvestmentStatus? status, CancellationToken ct)
    {
        var owner = OwnerId;
        var q = _db.InvestmentGroups.Include(g => g.Tag).Where(g => g.OwnerUserId == owner);
        if (status.HasValue) q = q.Where(g => g.Status == status.Value);

        var groups = await q.OrderBy(g => g.Name).ToListAsync(ct);

        // Compute aggregates per group
        var investmentMetrics = await ComputeAllInvestmentMetricsAsync(owner, ct);
        var byGroup = investmentMetrics
            .GroupBy(m => m.GroupId)
            .ToDictionary(
                g => g.Key,
                g => (Invested: g.Sum(x => x.Invested), Current: g.Sum(x => x.CurrentValue)));

        return groups.Select(g =>
        {
            byGroup.TryGetValue(g.Id, out var stats);
            var pl = stats.Current - stats.Invested;
            return new InvestmentGroupDto(
                g.Id, g.Name, g.Description,
                g.Tag is null ? null : new AssetTagBadge(g.Tag.Id, g.Tag.Name, g.Tag.Color),
                g.Status, stats.Invested, stats.Current, pl);
        }).ToList();
    }

    public async Task<InvestmentGroupDto> CreateGroupAsync(CreateInvestmentGroupRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        await ValidateTagAsync(owner, req.TagId, ct);
        var entity = new InvestmentGroup
        {
            OwnerUserId = owner,
            Name = req.Name.Trim(),
            Description = req.Description?.Trim(),
            TagId = req.TagId,
            Status = req.Status
        };
        _db.InvestmentGroups.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new InvestmentGroupDto(
            entity.Id, entity.Name, entity.Description, null, entity.Status, 0, 0, 0);
    }

    public async Task<InvestmentGroupDto> UpdateGroupAsync(Guid id, UpdateInvestmentGroupRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.InvestmentGroups.FirstOrDefaultAsync(g => g.Id == id && g.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Investment group not found.");
        await ValidateTagAsync(owner, req.TagId, ct);

        entity.Name = req.Name.Trim();
        entity.Description = req.Description?.Trim();
        entity.TagId = req.TagId;
        entity.Status = req.Status;
        await _db.SaveChangesAsync(ct);

        var groups = await GetGroupsAsync(null, ct);
        return groups.First(g => g.Id == id);
    }

    public async Task DeleteGroupAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.InvestmentGroups
            .Include(g => g.Investments).ThenInclude(i => i.PriceHistory)
            .Include(g => g.Investments).ThenInclude(i => i.Transactions)
            .FirstOrDefaultAsync(g => g.Id == id && g.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Investment group not found.");

        foreach (var inv in entity.Investments)
        {
            _db.InvestmentPriceHistory.RemoveRange(inv.PriceHistory);
            _db.InvestmentTransactions.RemoveRange(inv.Transactions);
        }
        _db.Investments.RemoveRange(entity.Investments);
        _db.InvestmentGroups.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    // ===== Investments =====
    public async Task<IReadOnlyList<InvestmentDto>> ListAsync(InvestmentStatus? status, Guid? tagId, Guid? groupId, CancellationToken ct)
    {
        var owner = OwnerId;
        var q = _db.Investments
            .Include(i => i.Group)
            .Include(i => i.Tag)
            .Where(i => i.OwnerUserId == owner);

        if (status.HasValue) q = q.Where(i => i.Status == status.Value);
        if (tagId.HasValue) q = q.Where(i => i.TagId == tagId.Value);
        if (groupId.HasValue) q = q.Where(i => i.GroupId == groupId.Value);

        var investments = await q.OrderBy(i => i.Group!.Name).ThenBy(i => i.Name).ToListAsync(ct);

        var metrics = await ComputeMetricsForAsync(investments.Select(i => i.Id).ToList(), ct);

        return investments.Select(i =>
        {
            metrics.TryGetValue(i.Id, out var m);
            return new InvestmentDto(
                i.Id, i.GroupId, i.Group!.Name, i.Name, i.Description,
                i.Tag is null ? null : new AssetTagBadge(i.Tag.Id, i.Tag.Name, i.Tag.Color),
                i.Unit, i.Status,
                m?.CurrentPrice, m?.LastPriceAsOf,
                m?.UnitsHolding ?? 0,
                m?.AverageBuyPrice ?? 0,
                m?.CurrentValue ?? 0,
                m?.Invested ?? 0,
                m?.ProfitLoss ?? 0);
        }).ToList();
    }

    public async Task<InvestmentDetailDto> GetAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.Investments
            .Include(i => i.Group)
            .Include(i => i.Tag)
            .Include(i => i.PriceHistory.OrderByDescending(p => p.AsOf))
            .Include(i => i.Transactions.OrderByDescending(t => t.Date))
            .FirstOrDefaultAsync(i => i.Id == id && i.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Investment not found.");

        var metrics = (await ComputeMetricsForAsync(new List<Guid> { id }, ct)).GetValueOrDefault(id);

        var dto = new InvestmentDto(
            entity.Id, entity.GroupId, entity.Group!.Name, entity.Name, entity.Description,
            entity.Tag is null ? null : new AssetTagBadge(entity.Tag.Id, entity.Tag.Name, entity.Tag.Color),
            entity.Unit, entity.Status,
            metrics?.CurrentPrice, metrics?.LastPriceAsOf,
            metrics?.UnitsHolding ?? 0,
            metrics?.AverageBuyPrice ?? 0,
            metrics?.CurrentValue ?? 0,
            metrics?.Invested ?? 0,
            metrics?.ProfitLoss ?? 0);

        var prices = entity.PriceHistory
            .OrderByDescending(p => p.AsOf).ThenByDescending(p => p.CreatedAt)
            .Select(p => new InvestmentPriceHistoryDto(p.Id, p.AsOf, p.Price, p.Note))
            .ToList();

        var txs = entity.Transactions
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt)
            .Select(t => new InvestmentTxDto(t.Id, t.Date, t.Type, t.Units, t.Price, t.Units * t.Price, t.Note))
            .ToList();

        return new InvestmentDetailDto(dto, prices, txs);
    }

    public async Task<InvestmentDto> CreateAsync(CreateInvestmentRequest req, CancellationToken ct)
    {
        if (req.CurrentPrice <= 0) throw new ArgumentException("Current price must be positive.");
        if (string.IsNullOrWhiteSpace(req.Unit)) throw new ArgumentException("Unit is required.");

        var owner = OwnerId;
        var groupOk = await _db.InvestmentGroups.AnyAsync(g => g.Id == req.GroupId && g.OwnerUserId == owner, ct);
        if (!groupOk) throw new KeyNotFoundException("Investment group not found.");
        await ValidateTagAsync(owner, req.TagId, ct);

        var entity = new Investment
        {
            OwnerUserId = owner,
            GroupId = req.GroupId,
            Name = req.Name.Trim(),
            Description = req.Description?.Trim(),
            TagId = req.TagId,
            Unit = req.Unit.Trim(),
            Status = InvestmentStatus.Active
        };
        entity.PriceHistory.Add(new InvestmentPriceHistory
        {
            OwnerUserId = owner,
            AsOf = DateOnly.FromDateTime(DateTime.UtcNow),
            Price = req.CurrentPrice,
            Note = "Initial"
        });

        _db.Investments.Add(entity);
        await _db.SaveChangesAsync(ct);

        var list = await ListAsync(null, null, entity.GroupId, ct);
        return list.First(i => i.Id == entity.Id);
    }

    public async Task<InvestmentDto> UpdateAsync(Guid id, UpdateInvestmentRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Unit)) throw new ArgumentException("Unit is required.");

        var owner = OwnerId;
        var entity = await _db.Investments.FirstOrDefaultAsync(i => i.Id == id && i.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Investment not found.");

        if (entity.GroupId != req.GroupId)
        {
            var groupOk = await _db.InvestmentGroups.AnyAsync(g => g.Id == req.GroupId && g.OwnerUserId == owner, ct);
            if (!groupOk) throw new KeyNotFoundException("Target group not found.");
            entity.GroupId = req.GroupId;
        }
        await ValidateTagAsync(owner, req.TagId, ct);

        entity.Name = req.Name.Trim();
        entity.Description = req.Description?.Trim();
        entity.TagId = req.TagId;
        entity.Unit = req.Unit.Trim();

        await _db.SaveChangesAsync(ct);
        var list = await ListAsync(null, null, entity.GroupId, ct);
        return list.First(i => i.Id == id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.Investments
            .Include(i => i.PriceHistory)
            .Include(i => i.Transactions)
            .FirstOrDefaultAsync(i => i.Id == id && i.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Investment not found.");
        _db.InvestmentPriceHistory.RemoveRange(entity.PriceHistory);
        _db.InvestmentTransactions.RemoveRange(entity.Transactions);
        _db.Investments.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    // ===== Price history =====
    public async Task<InvestmentPriceHistoryDto> AddPriceAsync(Guid investmentId, AddInvestmentPriceRequest req, CancellationToken ct)
    {
        if (req.Price <= 0) throw new ArgumentException("Price must be positive.");
        var owner = OwnerId;
        var ok = await _db.Investments.AnyAsync(i => i.Id == investmentId && i.OwnerUserId == owner, ct);
        if (!ok) throw new KeyNotFoundException("Investment not found.");

        var entity = new InvestmentPriceHistory
        {
            OwnerUserId = owner,
            InvestmentId = investmentId,
            AsOf = req.AsOf,
            Price = req.Price,
            Note = req.Note?.Trim()
        };
        _db.InvestmentPriceHistory.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new InvestmentPriceHistoryDto(entity.Id, entity.AsOf, entity.Price, entity.Note);
    }

    public async Task<InvestmentPriceHistoryDto> UpdatePriceAsync(Guid investmentId, Guid priceId, UpdateInvestmentPriceRequest req, CancellationToken ct)
    {
        if (req.Price <= 0) throw new ArgumentException("Price must be positive.");
        var owner = OwnerId;
        var entity = await _db.InvestmentPriceHistory
            .FirstOrDefaultAsync(p => p.Id == priceId && p.InvestmentId == investmentId && p.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Price entry not found.");
        entity.AsOf = req.AsOf;
        entity.Price = req.Price;
        entity.Note = req.Note?.Trim();
        await _db.SaveChangesAsync(ct);
        return new InvestmentPriceHistoryDto(entity.Id, entity.AsOf, entity.Price, entity.Note);
    }

    public async Task DeletePriceAsync(Guid investmentId, Guid priceId, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.InvestmentPriceHistory
            .FirstOrDefaultAsync(p => p.Id == priceId && p.InvestmentId == investmentId && p.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Price entry not found.");

        var count = await _db.InvestmentPriceHistory.CountAsync(p => p.InvestmentId == investmentId, ct);
        if (count <= 1) throw new InvalidOperationException("An investment must have at least one price entry.");

        _db.InvestmentPriceHistory.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    // ===== Buy/sell transactions =====
    public async Task<InvestmentTxDto> AddTxAsync(Guid investmentId, AddInvestmentTxRequest req, CancellationToken ct)
    {
        if (req.Units <= 0) throw new ArgumentException("Units must be positive.");
        var owner = OwnerId;
        var inv = await _db.Investments.FirstOrDefaultAsync(i => i.Id == investmentId && i.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Investment not found.");

        var price = req.Price ?? await ResolvePriceAtAsync(investmentId, req.Date, ct);
        if (price is null || price <= 0) throw new ArgumentException("Could not resolve a price for that date â€” provide one explicitly.");

        if (req.Type == InvestmentTxType.Sell)
        {
            // Validate enough units to sell
            var holding = await ComputeUnitsHoldingAsync(investmentId, ct);
            if (req.Units > holding + 0.0001m) throw new ArgumentException($"Cannot sell {req.Units} units; only {holding} held.");
        }

        var entity = new InvestmentTransaction
        {
            OwnerUserId = owner,
            InvestmentId = investmentId,
            Date = req.Date,
            Type = req.Type,
            Units = req.Units,
            Price = price.Value,
            Note = req.Note?.Trim()
        };
        _db.InvestmentTransactions.Add(entity);

        await _db.SaveChangesAsync(ct);
        await UpdateInvestmentStatusAsync(investmentId, ct);

        return new InvestmentTxDto(entity.Id, entity.Date, entity.Type, entity.Units, entity.Price, entity.Units * entity.Price, entity.Note);
    }

    public async Task<InvestmentTxDto> UpdateTxAsync(Guid investmentId, Guid txId, UpdateInvestmentTxRequest req, CancellationToken ct)
    {
        if (req.Units <= 0) throw new ArgumentException("Units must be positive.");
        var owner = OwnerId;
        var entity = await _db.InvestmentTransactions
            .FirstOrDefaultAsync(t => t.Id == txId && t.InvestmentId == investmentId && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Transaction not found.");

        var price = req.Price ?? await ResolvePriceAtAsync(investmentId, req.Date, ct) ?? entity.Price;

        // Validate sell quantity against holding excluding this row
        if (req.Type == InvestmentTxType.Sell)
        {
            var holdingExcl = await ComputeUnitsHoldingAsync(investmentId, ct, excludingTxId: txId);
            if (req.Units > holdingExcl + 0.0001m) throw new ArgumentException($"Cannot sell {req.Units} units; only {holdingExcl} held.");
        }

        entity.Date = req.Date;
        entity.Type = req.Type;
        entity.Units = req.Units;
        entity.Price = price;
        entity.Note = req.Note?.Trim();
        await _db.SaveChangesAsync(ct);
        await UpdateInvestmentStatusAsync(investmentId, ct);

        return new InvestmentTxDto(entity.Id, entity.Date, entity.Type, entity.Units, entity.Price, entity.Units * entity.Price, entity.Note);
    }

    public async Task DeleteTxAsync(Guid investmentId, Guid txId, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.InvestmentTransactions
            .FirstOrDefaultAsync(t => t.Id == txId && t.InvestmentId == investmentId && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Transaction not found.");
        _db.InvestmentTransactions.Remove(entity);
        await _db.SaveChangesAsync(ct);
        await UpdateInvestmentStatusAsync(investmentId, ct);
    }

    // ===== Reports =====
    public async Task<InvestmentReport> GetReportAsync(InvestmentStatus? status, CancellationToken ct)
    {
        var owner = OwnerId;
        var list = await ListAsync(status, null, null, ct);
        var rows = list.Select(i => new InvestmentReportRow(
            i.GroupName, i.Name, i.Unit, i.Tag?.Name, i.Status.ToString(),
            i.UnitsHolding, i.CurrentPrice ?? 0, i.CurrentHoldingValue, i.Invested, i.ProfitLoss
        )).ToList();
        return new InvestmentReport(rows);
    }

    // ===== Helpers =====
    private async Task ValidateTagAsync(Guid owner, Guid? tagId, CancellationToken ct)
    {
        if (!tagId.HasValue) return;
        var ok = await _db.AssetTags.AnyAsync(t => t.Id == tagId.Value && t.OwnerUserId == owner, ct);
        if (!ok) throw new KeyNotFoundException("Tag not found.");
    }

    /// <summary>Returns the price effective on or before the given date, falling back to the earliest entry.</summary>
    private async Task<decimal?> ResolvePriceAtAsync(Guid investmentId, DateOnly date, CancellationToken ct)
    {
        var owner = OwnerId;
        var atOrBefore = await _db.InvestmentPriceHistory
            .Where(p => p.InvestmentId == investmentId && p.OwnerUserId == owner && p.AsOf <= date)
            .OrderByDescending(p => p.AsOf).ThenByDescending(p => p.CreatedAt)
            .Select(p => (decimal?)p.Price)
            .FirstOrDefaultAsync(ct);
        if (atOrBefore is not null) return atOrBefore;

        // Fall back to earliest if no entry on/before
        return await _db.InvestmentPriceHistory
            .Where(p => p.InvestmentId == investmentId && p.OwnerUserId == owner)
            .OrderBy(p => p.AsOf).ThenBy(p => p.CreatedAt)
            .Select(p => (decimal?)p.Price)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<decimal> ComputeUnitsHoldingAsync(Guid investmentId, CancellationToken ct, Guid? excludingTxId = null)
    {
        var owner = OwnerId;
        var q = _db.InvestmentTransactions.Where(t => t.InvestmentId == investmentId && t.OwnerUserId == owner);
        if (excludingTxId.HasValue) q = q.Where(t => t.Id != excludingTxId.Value);
        var bought = await q.Where(t => t.Type == InvestmentTxType.Buy).SumAsync(t => (decimal?)t.Units, ct) ?? 0m;
        var sold = await q.Where(t => t.Type == InvestmentTxType.Sell).SumAsync(t => (decimal?)t.Units, ct) ?? 0m;
        return bought - sold;
    }

    private async Task UpdateInvestmentStatusAsync(Guid investmentId, CancellationToken ct)
    {
        var inv = await _db.Investments.FirstOrDefaultAsync(i => i.Id == investmentId, ct);
        if (inv is null) return;
        var holding = await ComputeUnitsHoldingAsync(investmentId, ct);
        var desired = holding > 0 ? InvestmentStatus.Active : InvestmentStatus.Inactive;
        if (inv.Status != desired)
        {
            inv.Status = desired;
            await _db.SaveChangesAsync(ct);
        }
    }

    private record Metrics(
        Guid InvestmentId,
        Guid GroupId,
        decimal? CurrentPrice,
        DateOnly? LastPriceAsOf,
        decimal UnitsHolding,
        decimal AverageBuyPrice,
        decimal CurrentValue,
        decimal Invested,
        decimal ProfitLoss);

    private async Task<IReadOnlyList<Metrics>> ComputeAllInvestmentMetricsAsync(Guid owner, CancellationToken ct)
    {
        var ids = await _db.Investments.Where(i => i.OwnerUserId == owner).Select(i => i.Id).ToListAsync(ct);
        var map = await ComputeMetricsForAsync(ids, ct);
        return ids.Select(id => map[id]).ToList();
    }

    private async Task<Dictionary<Guid, Metrics>> ComputeMetricsForAsync(IReadOnlyList<Guid> investmentIds, CancellationToken ct)
    {
        if (investmentIds.Count == 0) return new Dictionary<Guid, Metrics>();
        var owner = OwnerId;

        var investments = await _db.Investments
            .Where(i => investmentIds.Contains(i.Id) && i.OwnerUserId == owner)
            .Select(i => new { i.Id, i.GroupId })
            .ToListAsync(ct);

        var prices = await _db.InvestmentPriceHistory
            .Where(p => investmentIds.Contains(p.InvestmentId) && p.OwnerUserId == owner)
            .Select(p => new { p.InvestmentId, p.AsOf, p.Price, p.CreatedAt })
            .ToListAsync(ct);

        var txs = await _db.InvestmentTransactions
            .Where(t => investmentIds.Contains(t.InvestmentId) && t.OwnerUserId == owner)
            .Select(t => new { t.InvestmentId, t.Type, t.Units, t.Price })
            .ToListAsync(ct);

        var map = new Dictionary<Guid, Metrics>(investmentIds.Count);
        foreach (var inv in investments)
        {
            var latest = prices
                .Where(p => p.InvestmentId == inv.Id)
                .OrderByDescending(p => p.AsOf).ThenByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            var iTxs = txs.Where(t => t.InvestmentId == inv.Id).ToList();
            var totalBuyUnits = iTxs.Where(t => t.Type == InvestmentTxType.Buy).Sum(t => t.Units);
            var totalBuyCost = iTxs.Where(t => t.Type == InvestmentTxType.Buy).Sum(t => t.Units * t.Price);
            var totalSellUnits = iTxs.Where(t => t.Type == InvestmentTxType.Sell).Sum(t => t.Units);
            var avgBuy = totalBuyUnits > 0 ? totalBuyCost / totalBuyUnits : 0m;
            var unitsHolding = totalBuyUnits - totalSellUnits;
            var invested = unitsHolding > 0 ? unitsHolding * avgBuy : 0m;
            var currentPrice = latest?.Price ?? 0m;
            var currentValue = unitsHolding > 0 ? unitsHolding * currentPrice : 0m;
            var profitLoss = currentValue - invested;

            map[inv.Id] = new Metrics(
                inv.Id, inv.GroupId,
                latest is null ? null : (decimal?)latest.Price,
                latest is null ? null : (DateOnly?)latest.AsOf,
                unitsHolding, avgBuy, currentValue, invested, profitLoss);
        }
        return map;
    }
}
