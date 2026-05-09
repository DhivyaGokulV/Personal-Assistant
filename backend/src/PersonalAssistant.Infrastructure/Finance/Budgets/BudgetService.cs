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
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Budget not found.");
        return await MapAsync(b, ct);
    }

    public async Task<BudgetDto> CreateAsync(CreateBudgetRequest req, CancellationToken ct)
    {
        if (req.Amount <= 0) throw new ArgumentException("Amount must be positive.");
        if (req.To < req.From) throw new ArgumentException("To date must be on/after From date.");

        var owner = OwnerId;
        var categoryOk = await _db.Categories.AnyAsync(c => c.Id == req.CategoryId && c.OwnerUserId == owner, ct);
        if (!categoryOk) throw new KeyNotFoundException("Category not found.");

        var entity = new Budget
        {
            OwnerUserId = owner,
            Name = req.Name.Trim(),
            CategoryId = req.CategoryId,
            Amount = req.Amount,
            From = req.From,
            To = req.To,
            Note = req.Note?.Trim()
        };
        _db.Budgets.Add(entity);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(entity.Id, ct);
    }

    public async Task<BudgetDto> UpdateAsync(Guid id, UpdateBudgetRequest req, CancellationToken ct)
    {
        if (req.Amount <= 0) throw new ArgumentException("Amount must be positive.");
        if (req.To < req.From) throw new ArgumentException("To date must be on/after From date.");

        var owner = OwnerId;
        var entity = await _db.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Budget not found.");

        var categoryOk = await _db.Categories.AnyAsync(c => c.Id == req.CategoryId && c.OwnerUserId == owner, ct);
        if (!categoryOk) throw new KeyNotFoundException("Category not found.");

        entity.Name = req.Name.Trim();
        entity.CategoryId = req.CategoryId;
        entity.Amount = req.Amount;
        entity.From = req.From;
        entity.To = req.To;
        entity.Note = req.Note?.Trim();

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
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Budget not found.");

        var dto = await MapAsync(b, ct);

        var rows = await _db.Transactions
            .Where(t => t.OwnerUserId == owner
                     && t.CategoryId == b.CategoryId
                     && t.Type == TransactionType.Debit
                     && t.Date >= b.From && t.Date <= b.To
                     && !t.TransferGroupId.HasValue)
            .OrderBy(t => t.Date).ThenBy(t => t.CreatedAt)
            .Select(t => new BudgetTransactionRow(
                t.Date,
                t.Reason,
                t.Account!.Name,
                t.PaymentType != null ? t.PaymentType.Name : null,
                t.Amount))
            .ToListAsync(ct);

        return new BudgetReport(dto, rows);
    }

    private async Task<BudgetDto> MapAsync(Budget b, CancellationToken ct)
    {
        var owner = OwnerId;
        var spent = await _db.Transactions
            .Where(t => t.OwnerUserId == owner
                     && t.CategoryId == b.CategoryId
                     && t.Type == TransactionType.Debit
                     && t.Date >= b.From && t.Date <= b.To
                     && !t.TransferGroupId.HasValue)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;

        var remaining = b.Amount - spent;
        var pct = b.Amount > 0 ? Math.Round(spent / b.Amount * 100m, 1) : 0m;

        return new BudgetDto(
            b.Id, b.Name, b.CategoryId, b.Category?.Name ?? "",
            b.Amount, b.From, b.To, b.Note,
            spent, remaining, pct);
    }
}
