using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Application.Finance.Settings;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Domain.Finance;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.Finance.Settings;

public class FinanceSettingsService : IFinanceSettingsService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public FinanceSettingsService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId
        ?? throw new InvalidOperationException("No authenticated user.");

    // ===== Accounts =====
    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync(bool includeInactive, CancellationToken ct)
    {
        var owner = OwnerId;
        var q = _db.Accounts.Where(a => a.OwnerUserId == owner);
        if (!includeInactive) q = q.Where(a => a.Status == AccountStatus.Active);
        return await q
            .OrderBy(a => a.Name)
            .Select(a => new AccountDto(a.Id, a.Name, a.Description, a.OpeningBalance, a.OpeningDate, a.Status))
            .ToListAsync(ct);
    }

    public async Task<AccountDto> CreateAccountAsync(CreateAccountRequest req, CancellationToken ct)
    {
        var entity = new Account
        {
            OwnerUserId = OwnerId,
            Name = req.Name.Trim(),
            Description = req.Description?.Trim(),
            OpeningBalance = req.OpeningBalance,
            OpeningDate = req.OpeningDate,
            Status = req.Status
        };
        _db.Accounts.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new AccountDto(entity.Id, entity.Name, entity.Description, entity.OpeningBalance, entity.OpeningDate, entity.Status);
    }

    public async Task<AccountDto> UpdateAccountAsync(Guid id, UpdateAccountRequest req, CancellationToken ct)
    {
        var entity = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Account not found.");
        entity.Name = req.Name.Trim();
        entity.Description = req.Description?.Trim();
        entity.OpeningBalance = req.OpeningBalance;
        entity.OpeningDate = req.OpeningDate;
        entity.Status = req.Status;
        await _db.SaveChangesAsync(ct);
        return new AccountDto(entity.Id, entity.Name, entity.Description, entity.OpeningBalance, entity.OpeningDate, entity.Status);
    }

    public async Task DeleteAccountAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Account not found.");

        var inUse = await _db.Transactions.AnyAsync(t => t.AccountId == id, ct);
        if (inUse) throw new InvalidOperationException("Cannot delete an account with transactions. Mark it inactive instead.");

        _db.Accounts.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    // ===== Categories =====
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct)
    {
        var owner = OwnerId;
        return await _db.Categories
            .Where(c => c.OwnerUserId == owner)
            .OrderBy(c => c.Type).ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Type))
            .ToListAsync(ct);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest req, CancellationToken ct)
    {
        var entity = new Category { OwnerUserId = OwnerId, Name = req.Name.Trim(), Type = req.Type };
        _db.Categories.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new CategoryDto(entity.Id, entity.Name, entity.Type);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequest req, CancellationToken ct)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Category not found.");
        entity.Name = req.Name.Trim();
        entity.Type = req.Type;
        await _db.SaveChangesAsync(ct);
        return new CategoryDto(entity.Id, entity.Name, entity.Type);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Category not found.");

        var budgetUse = await _db.Budgets.AnyAsync(b => b.CategoryId == id, ct);
        if (budgetUse) throw new InvalidOperationException("Cannot delete a category used by a budget.");

        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    // ===== Payment Types =====
    public async Task<IReadOnlyList<PaymentTypeDto>> GetPaymentTypesAsync(CancellationToken ct)
    {
        var owner = OwnerId;
        return await _db.PaymentTypes
            .Where(p => p.OwnerUserId == owner)
            .OrderBy(p => p.Name)
            .Select(p => new PaymentTypeDto(p.Id, p.Name, p.Description))
            .ToListAsync(ct);
    }

    public async Task<PaymentTypeDto> CreatePaymentTypeAsync(CreatePaymentTypeRequest req, CancellationToken ct)
    {
        var entity = new PaymentType { OwnerUserId = OwnerId, Name = req.Name.Trim(), Description = req.Description?.Trim() };
        _db.PaymentTypes.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new PaymentTypeDto(entity.Id, entity.Name, entity.Description);
    }

    public async Task<PaymentTypeDto> UpdatePaymentTypeAsync(Guid id, UpdatePaymentTypeRequest req, CancellationToken ct)
    {
        var entity = await _db.PaymentTypes.FirstOrDefaultAsync(p => p.Id == id && p.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Payment type not found.");
        entity.Name = req.Name.Trim();
        entity.Description = req.Description?.Trim();
        await _db.SaveChangesAsync(ct);
        return new PaymentTypeDto(entity.Id, entity.Name, entity.Description);
    }

    public async Task DeletePaymentTypeAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.PaymentTypes.FirstOrDefaultAsync(p => p.Id == id && p.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Payment type not found.");
        _db.PaymentTypes.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    // ===== Tags =====
    public async Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken ct)
    {
        var owner = OwnerId;
        return await _db.Tags
            .Where(t => t.OwnerUserId == owner)
            .OrderBy(t => t.Name)
            .Select(t => new TagDto(t.Id, t.Name, t.Description, t.Color))
            .ToListAsync(ct);
    }

    public async Task<TagDto> CreateTagAsync(CreateTagRequest req, CancellationToken ct)
    {
        var entity = new Tag
        {
            OwnerUserId = OwnerId,
            Name = req.Name.Trim(),
            Description = req.Description?.Trim(),
            Color = string.IsNullOrWhiteSpace(req.Color) ? "#6c5ce7" : req.Color.Trim()
        };
        _db.Tags.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new TagDto(entity.Id, entity.Name, entity.Description, entity.Color);
    }

    public async Task<TagDto> UpdateTagAsync(Guid id, UpdateTagRequest req, CancellationToken ct)
    {
        var entity = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Tag not found.");
        entity.Name = req.Name.Trim();
        entity.Description = req.Description?.Trim();
        if (!string.IsNullOrWhiteSpace(req.Color)) entity.Color = req.Color.Trim();
        await _db.SaveChangesAsync(ct);
        return new TagDto(entity.Id, entity.Name, entity.Description, entity.Color);
    }

    public async Task DeleteTagAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Tag not found.");
        _db.Tags.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
