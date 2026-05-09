using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.AssetTracker.Common;
using PersonalAssistant.Application.AssetTracker.Liabilities;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Domain.AssetTracker;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.AssetTracker.Liabilities;

public class LiabilityService : ILiabilityService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public LiabilityService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId ?? throw new InvalidOperationException("No authenticated user.");

    public async Task<IReadOnlyList<LiabilityDto>> ListAsync(LiabilityStatus? status, Guid? tagId, CancellationToken ct)
    {
        var owner = OwnerId;
        var q = _db.Liabilities.Include(l => l.Tag).Where(l => l.OwnerUserId == owner);
        if (status.HasValue) q = q.Where(l => l.Status == status.Value);
        if (tagId.HasValue) q = q.Where(l => l.TagId == tagId.Value);

        var entities = await q.OrderByDescending(l => l.UpdatedAt).ToListAsync(ct);

        var ids = entities.Select(e => e.Id).ToList();
        var balances = await ComputeBalancesAsync(ids, ct);

        return entities.Select(l =>
        {
            balances.TryGetValue(l.Id, out var b);
            return new LiabilityDto(
                l.Id, l.Name, l.Description,
                l.Tag is null ? null : new AssetTagBadge(l.Tag.Id, l.Tag.Name, l.Tag.Color),
                l.Status, b.Balance, b.LastDate);
        }).ToList();
    }

    public async Task<LiabilityDetailDto> GetAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.Liabilities
            .Include(l => l.Tag)
            .Include(l => l.History.OrderBy(h => h.Date))
            .FirstOrDefaultAsync(l => l.Id == id && l.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Liability not found.");

        var balances = await ComputeBalancesAsync(new List<Guid> { id }, ct);
        balances.TryGetValue(id, out var b);

        var dto = new LiabilityDto(
            entity.Id, entity.Name, entity.Description,
            entity.Tag is null ? null : new AssetTagBadge(entity.Tag.Id, entity.Tag.Name, entity.Tag.Color),
            entity.Status, b.Balance, b.LastDate);

        // Compute running balance per history row (chronological)
        var runningBalance = 0m;
        var ordered = entity.History.OrderBy(h => h.Date).ThenBy(h => h.CreatedAt).ToList();
        var historyDtos = new List<LiabilityHistoryDto>(ordered.Count);
        foreach (var h in ordered)
        {
            runningBalance += h.Type == LiabilityTxType.Acquisition ? h.Amount : -h.Amount;
            historyDtos.Add(new LiabilityHistoryDto(h.Id, h.Date, h.Type, h.Amount, runningBalance, h.Note));
        }
        // Reverse so most recent is first
        historyDtos.Reverse();
        return new LiabilityDetailDto(dto, historyDtos);
    }

    public async Task<LiabilityDto> CreateAsync(CreateLiabilityRequest req, CancellationToken ct)
    {
        if (req.InitialAmount <= 0) throw new ArgumentException("Initial amount must be positive.");
        var owner = OwnerId;
        await ValidateTagAsync(owner, req.TagId, ct);

        var entity = new Liability
        {
            OwnerUserId = owner,
            Name = req.Name.Trim(),
            Description = req.Description?.Trim(),
            TagId = req.TagId,
            Status = LiabilityStatus.Active
        };
        entity.History.Add(new LiabilityHistory
        {
            OwnerUserId = owner,
            Date = req.Date ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Type = LiabilityTxType.Acquisition,
            Amount = req.InitialAmount,
            Note = req.Note?.Trim() ?? "Initial"
        });
        _db.Liabilities.Add(entity);
        await _db.SaveChangesAsync(ct);

        var list = await ListAsync(null, null, ct);
        return list.First(l => l.Id == entity.Id);
    }

    public async Task<LiabilityDto> UpdateAsync(Guid id, UpdateLiabilityRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.Liabilities.FirstOrDefaultAsync(l => l.Id == id && l.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Liability not found.");
        await ValidateTagAsync(owner, req.TagId, ct);

        entity.Name = req.Name.Trim();
        entity.Description = req.Description?.Trim();
        entity.TagId = req.TagId;
        await _db.SaveChangesAsync(ct);

        var list = await ListAsync(null, null, ct);
        return list.First(l => l.Id == id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.Liabilities
            .Include(l => l.History)
            .FirstOrDefaultAsync(l => l.Id == id && l.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Liability not found.");
        _db.LiabilityHistory.RemoveRange(entity.History);
        _db.Liabilities.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<LiabilityHistoryDto> AddHistoryAsync(Guid liabilityId, AddLiabilityEntryRequest req, CancellationToken ct)
    {
        if (req.Amount <= 0) throw new ArgumentException("Amount must be positive.");
        var owner = OwnerId;
        var liability = await _db.Liabilities.FirstOrDefaultAsync(l => l.Id == liabilityId && l.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Liability not found.");

        if (req.Type == LiabilityTxType.Repayment)
        {
            var bal = await ComputeBalanceAsync(liabilityId, ct);
            if (req.Amount > bal + 0.0001m) throw new ArgumentException($"Repayment {req.Amount:0.00} exceeds outstanding balance {bal:0.00}.");
        }

        var entity = new LiabilityHistory
        {
            OwnerUserId = owner,
            LiabilityId = liabilityId,
            Date = req.Date,
            Type = req.Type,
            Amount = req.Amount,
            Note = req.Note?.Trim()
        };
        _db.LiabilityHistory.Add(entity);
        await _db.SaveChangesAsync(ct);
        await UpdateLiabilityStatusAsync(liabilityId, ct);

        // Compute running balance up to this entry
        var detail = await GetAsync(liabilityId, ct);
        var hist = detail.History.First(h => h.Id == entity.Id);
        return hist;
    }

    public async Task<LiabilityHistoryDto> UpdateHistoryAsync(Guid liabilityId, Guid historyId, UpdateLiabilityEntryRequest req, CancellationToken ct)
    {
        if (req.Amount <= 0) throw new ArgumentException("Amount must be positive.");
        var owner = OwnerId;
        var entity = await _db.LiabilityHistory
            .FirstOrDefaultAsync(h => h.Id == historyId && h.LiabilityId == liabilityId && h.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("History entry not found.");

        // Validate repayment doesn't drive balance negative
        if (req.Type == LiabilityTxType.Repayment)
        {
            var balExcl = await ComputeBalanceAsync(liabilityId, ct, excludingHistoryId: historyId);
            if (req.Amount > balExcl + 0.0001m) throw new ArgumentException($"Repayment {req.Amount:0.00} exceeds balance excluding this row ({balExcl:0.00}).");
        }

        entity.Date = req.Date;
        entity.Type = req.Type;
        entity.Amount = req.Amount;
        entity.Note = req.Note?.Trim();
        await _db.SaveChangesAsync(ct);
        await UpdateLiabilityStatusAsync(liabilityId, ct);

        var detail = await GetAsync(liabilityId, ct);
        return detail.History.First(h => h.Id == historyId);
    }

    public async Task DeleteHistoryAsync(Guid liabilityId, Guid historyId, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.LiabilityHistory
            .FirstOrDefaultAsync(h => h.Id == historyId && h.LiabilityId == liabilityId && h.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("History entry not found.");

        var count = await _db.LiabilityHistory.CountAsync(h => h.LiabilityId == liabilityId, ct);
        if (count <= 1) throw new InvalidOperationException("A liability must have at least one history entry.");

        _db.LiabilityHistory.Remove(entity);
        await _db.SaveChangesAsync(ct);
        await UpdateLiabilityStatusAsync(liabilityId, ct);
    }

    public async Task<LiabilityReport> GetReportAsync(LiabilityStatus? status, CancellationToken ct)
    {
        var list = await ListAsync(status, null, ct);
        var rows = list.Select(l => new LiabilityReportRow(
            l.Name, l.Tag?.Name, l.Status.ToString(), l.CurrentAmount, l.LastUpdate)).ToList();
        return new LiabilityReport(rows);
    }

    // ===== Helpers =====
    private async Task ValidateTagAsync(Guid owner, Guid? tagId, CancellationToken ct)
    {
        if (!tagId.HasValue) return;
        var ok = await _db.AssetTags.AnyAsync(t => t.Id == tagId.Value && t.OwnerUserId == owner, ct);
        if (!ok) throw new KeyNotFoundException("Tag not found.");
    }

    private async Task<Dictionary<Guid, (decimal Balance, DateOnly? LastDate)>> ComputeBalancesAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return new Dictionary<Guid, (decimal, DateOnly?)>();
        var owner = OwnerId;
        var rows = await _db.LiabilityHistory
            .Where(h => ids.Contains(h.LiabilityId) && h.OwnerUserId == owner)
            .Select(h => new { h.LiabilityId, h.Date, h.Type, h.Amount })
            .ToListAsync(ct);

        var map = new Dictionary<Guid, (decimal, DateOnly?)>();
        foreach (var id in ids)
        {
            var rs = rows.Where(r => r.LiabilityId == id).ToList();
            var balance = rs.Sum(r => r.Type == LiabilityTxType.Acquisition ? r.Amount : -r.Amount);
            DateOnly? lastDate = rs.Count == 0 ? null : rs.Max(r => r.Date);
            map[id] = (balance, lastDate);
        }
        return map;
    }

    private async Task<decimal> ComputeBalanceAsync(Guid liabilityId, CancellationToken ct, Guid? excludingHistoryId = null)
    {
        var owner = OwnerId;
        var q = _db.LiabilityHistory.Where(h => h.LiabilityId == liabilityId && h.OwnerUserId == owner);
        if (excludingHistoryId.HasValue) q = q.Where(h => h.Id != excludingHistoryId.Value);
        var acq = await q.Where(h => h.Type == LiabilityTxType.Acquisition).SumAsync(h => (decimal?)h.Amount, ct) ?? 0m;
        var rep = await q.Where(h => h.Type == LiabilityTxType.Repayment).SumAsync(h => (decimal?)h.Amount, ct) ?? 0m;
        return acq - rep;
    }

    private async Task UpdateLiabilityStatusAsync(Guid liabilityId, CancellationToken ct)
    {
        var liab = await _db.Liabilities.FirstOrDefaultAsync(l => l.Id == liabilityId, ct);
        if (liab is null) return;
        var bal = await ComputeBalanceAsync(liabilityId, ct);
        var desired = bal > 0 ? LiabilityStatus.Active : LiabilityStatus.Past;
        if (liab.Status != desired)
        {
            liab.Status = desired;
            await _db.SaveChangesAsync(ct);
        }
    }
}
