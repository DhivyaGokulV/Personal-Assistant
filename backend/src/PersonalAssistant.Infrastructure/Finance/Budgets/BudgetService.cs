using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Application.Finance.Budgets;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Domain.Finance;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.Finance.Budgets;

public class BudgetService : IBudgetService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public BudgetService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId
        ?? throw new InvalidOperationException("No authenticated user.");

    public async Task<IReadOnlyList<BudgetDto>> ListAsync(CancellationToken ct)
    {
        var owner = OwnerId;
        var budgets = await _db.Budgets
            .Include(b => b.Category)
            .Include(b => b.Entries).ThenInclude(e => e.Category)
            .Where(b => b.OwnerUserId == owner)
            .OrderByDescending(b => b.From)
            .ToListAsync(ct);

        var result = new List<BudgetDto>(budgets.Count);
        foreach (var b in budgets) result.Add(await MapAsync(b, ct));
        return result;
    }

    public async Task<BudgetDto> GetAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var b = await _db.Budgets
            .Include(x => x.Category)
            .Include(x => x.Entries).ThenInclude(e => e.Category)
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Budget not found.");
        return await MapAsync(b, ct);
    }

    public async Task<BudgetDto> CreateAsync(CreateBudgetRequest req, CancellationToken ct)
    {
        var entries = await ValidateEntriesAsync(req.CategoryId, req.Amount, req.Entries, ct);
        if (req.To < req.From) throw new ArgumentException("To date must be on/after From date.");

        var owner = OwnerId;

        var entity = new Budget
        {
            OwnerUserId = owner,
            Name = req.Name.Trim(),
            CategoryId = entries[0].CategoryId,
            Amount = entries.Sum(x => x.Amount),
            From = req.From,
            To = req.To,
            Note = req.Note?.Trim()
        };
        foreach (var entry in entries)
        {
            entity.Entries.Add(new BudgetEntry { OwnerUserId = owner, CategoryId = entry.CategoryId, Amount = entry.Amount });
        }
        _db.Budgets.Add(entity);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(entity.Id, ct);
    }

    public async Task<BudgetDto> UpdateAsync(Guid id, UpdateBudgetRequest req, CancellationToken ct)
    {
        var entries = await ValidateEntriesAsync(req.CategoryId, req.Amount, req.Entries, ct);
        if (req.To < req.From) throw new ArgumentException("To date must be on/after From date.");

        var owner = OwnerId;
        var entity = await _db.Budgets.Include(b => b.Entries).FirstOrDefaultAsync(b => b.Id == id && b.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Budget not found.");

        entity.Name = req.Name.Trim();
        entity.CategoryId = entries[0].CategoryId;
        entity.Amount = entries.Sum(x => x.Amount);
        entity.From = req.From;
        entity.To = req.To;
        entity.Note = req.Note?.Trim();
        _db.BudgetEntries.RemoveRange(entity.Entries);
        entity.Entries.Clear();
        foreach (var entry in entries)
        {
            entity.Entries.Add(new BudgetEntry { OwnerUserId = owner, BudgetId = entity.Id, CategoryId = entry.CategoryId, Amount = entry.Amount });
        }

        await _db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Budget not found.");
        _db.Budgets.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<BudgetReport> GetReportAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var b = await _db.Budgets
            .Include(x => x.Category)
            .Include(x => x.Entries).ThenInclude(e => e.Category)
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Budget not found.");

        var dto = await MapAsync(b, ct);
        var categoryIds = EffectiveEntries(b).Select(e => e.CategoryId).Distinct().ToList();

        var rows = await _db.Transactions
            .Where(t => t.OwnerUserId == owner
                     && t.CategoryId.HasValue
                     && categoryIds.Contains(t.CategoryId.Value)
                     && t.Type == TransactionType.Debit
                     && t.Date >= b.From && t.Date <= b.To
                     && !t.TransferGroupId.HasValue)
            .OrderBy(t => t.Date).ThenBy(t => t.CreatedAt)
            .Select(t => new BudgetTransactionRow(
                t.Date,
                t.Reason,
                t.Category != null ? t.Category.Name : "",
                t.Account!.Name,
                t.PaymentType != null ? t.PaymentType.Name : null,
                t.Amount))
            .ToListAsync(ct);

        return new BudgetReport(dto, rows);
    }

    private async Task<BudgetDto> MapAsync(Budget b, CancellationToken ct)
    {
        var owner = OwnerId;
        var entries = EffectiveEntries(b).ToList();
        var categoryIds = entries.Select(e => e.CategoryId).Distinct().ToList();
        var spentRows = await _db.Transactions
            .Where(t => t.OwnerUserId == owner
                     && t.CategoryId.HasValue
                     && categoryIds.Contains(t.CategoryId.Value)
                     && t.Type == TransactionType.Debit
                     && t.Date >= b.From && t.Date <= b.To
                     && !t.TransferGroupId.HasValue)
            .GroupBy(t => t.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Amount = g.Sum(t => t.Amount) })
            .ToListAsync(ct);
        var spentByCategory = spentRows.ToDictionary(x => x.CategoryId, x => x.Amount);

        var entryDtos = entries.Select(e =>
        {
            var spent = spentByCategory.GetValueOrDefault(e.CategoryId);
            var remaining = e.Amount - spent;
            var pct = e.Amount > 0 ? Math.Round(spent / e.Amount * 100m, 1) : 0m;
            return new BudgetEntryDto(e.Id, e.CategoryId, e.Category?.Name ?? "", e.Amount, spent, remaining, pct);
        }).ToList();

        var amount = entryDtos.Sum(x => x.Amount);
        var totalSpent = entryDtos.Sum(x => x.Spent);
        var totalRemaining = amount - totalSpent;
        var pctTotal = amount > 0 ? Math.Round(totalSpent / amount * 100m, 1) : 0m;
        var categoryName = entryDtos.Count == 1 ? entryDtos[0].CategoryName : $"{entryDtos.Count} categories";

        return new BudgetDto(
            b.Id, b.Name, entryDtos[0].CategoryId, categoryName,
            amount, b.From, b.To, b.Note,
            totalSpent, totalRemaining, pctTotal, entryDtos);
    }

    private async Task<IReadOnlyList<BudgetEntryRequest>> ValidateEntriesAsync(Guid categoryId, decimal amount, IReadOnlyList<BudgetEntryRequest>? requestedEntries, CancellationToken ct)
    {
        var entries = requestedEntries is { Count: > 0 }
            ? requestedEntries
            : new[] { new BudgetEntryRequest(categoryId, amount) };

        if (entries.Any(e => e.Amount <= 0)) throw new ArgumentException("Each budget entry amount must be positive.");
        if (entries.Any(e => e.CategoryId == Guid.Empty)) throw new ArgumentException("Each budget entry requires a category.");
        if (entries.GroupBy(e => e.CategoryId).Any(g => g.Count() > 1)) throw new ArgumentException("A category can be allocated only once per budget.");

        var owner = OwnerId;
        var ids = entries.Select(e => e.CategoryId).Distinct().ToList();
        var found = await _db.Categories.CountAsync(c => c.OwnerUserId == owner && ids.Contains(c.Id), ct);
        if (found != ids.Count) throw new KeyNotFoundException("One or more budget categories were not found.");

        return entries.ToList();
    }

    private static IEnumerable<BudgetEntry> EffectiveEntries(Budget b)
    {
        if (b.Entries.Count > 0) return b.Entries.OrderBy(e => e.Category?.Name ?? "");
        return new[]
        {
            new BudgetEntry
            {
                Id = Guid.Empty,
                OwnerUserId = b.OwnerUserId,
                BudgetId = b.Id,
                CategoryId = b.CategoryId,
                Category = b.Category,
                Amount = b.Amount
            }
        };
    }
}
