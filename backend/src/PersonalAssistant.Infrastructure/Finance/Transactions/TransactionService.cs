using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Application.Finance.Transactions;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Domain.Finance;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.Finance.Transactions;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public TransactionService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId
        ?? throw new InvalidOperationException("No authenticated user.");

    public async Task<PagedResult<TransactionDto>> ListAsync(TransactionFilters f, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var owner = OwnerId;

        var q = _db.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.PaymentType)
            .Include(t => t.TransactionTags).ThenInclude(tt => tt.Tag)
            .Where(t => t.OwnerUserId == owner);

        if (f.From.HasValue) q = q.Where(t => t.Date >= f.From.Value);
        if (f.To.HasValue) q = q.Where(t => t.Date <= f.To.Value);
        if (f.AccountId.HasValue) q = q.Where(t => t.AccountId == f.AccountId.Value);
        if (f.CategoryId.HasValue) q = q.Where(t => t.CategoryId == f.CategoryId.Value);
        if (f.PaymentTypeId.HasValue) q = q.Where(t => t.PaymentTypeId == f.PaymentTypeId.Value);
        if (f.Type.HasValue) q = q.Where(t => t.Type == f.Type.Value);
        if (f.TagId.HasValue) q = q.Where(t => t.TransactionTags.Any(tt => tt.TagId == f.TagId.Value));
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim();
            q = q.Where(t => EF.Functions.Like(t.Reason, $"%{s}%")
                          || (t.Note != null && EF.Functions.Like(t.Note, $"%{s}%")));
        }

        var total = await q.CountAsync(ct);

        var pageEntities = await q
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // If filtered by a single account, compute account standing per row.
        Dictionary<Guid, decimal>? standingByTxId = null;
        if (f.AccountId.HasValue)
        {
            standingByTxId = await ComputeAccountStandingForTransactionsAsync(f.AccountId.Value, pageEntities, ct);
        }

        var items = pageEntities.Select(t => Map(t, standingByTxId)).ToList();
        return new PagedResult<TransactionDto>(items, page, pageSize, total);
    }

    public async Task<TransactionDto> GetAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.PaymentType)
            .Include(t => t.TransactionTags).ThenInclude(tt => tt.Tag)
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Transaction not found.");
        return Map(entity, null);
    }

    public async Task<TransactionDto> CreateAsync(CreateTransactionRequest req, CancellationToken ct)
    {
        if (req.Amount <= 0) throw new ArgumentException("Amount must be positive.");

        var owner = OwnerId;
        await ValidateAccountAsync(owner, req.AccountId, ct);
        await ValidateOptionalRefsAsync(owner, req.CategoryId, req.PaymentTypeId, ct);
        var tagIds = await ValidateTagIdsAsync(owner, req.TagIds, ct);

        var entity = new Transaction
        {
            OwnerUserId = owner,
            Date = req.Date,
            Type = req.Type,
            AccountId = req.AccountId,
            Amount = req.Amount,
            Reason = req.Reason.Trim(),
            Note = req.Note?.Trim(),
            CategoryId = req.CategoryId,
            PaymentTypeId = req.PaymentTypeId
        };
        foreach (var tagId in tagIds) entity.TransactionTags.Add(new TransactionTag { TagId = tagId });

        _db.Transactions.Add(entity);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(entity.Id, ct);
    }

    public async Task<TransactionDto> UpdateAsync(Guid id, UpdateTransactionRequest req, CancellationToken ct)
    {
        if (req.Amount <= 0) throw new ArgumentException("Amount must be positive.");

        var owner = OwnerId;
        var entity = await _db.Transactions
            .Include(t => t.TransactionTags)
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Transaction not found.");

        await ValidateAccountAsync(owner, req.AccountId, ct);
        await ValidateOptionalRefsAsync(owner, req.CategoryId, req.PaymentTypeId, ct);
        var tagIds = await ValidateTagIdsAsync(owner, req.TagIds, ct);

        entity.Date = req.Date;
        entity.Type = req.Type;
        entity.AccountId = req.AccountId;
        entity.Amount = req.Amount;
        entity.Reason = req.Reason.Trim();
        entity.Note = req.Note?.Trim();
        entity.CategoryId = req.CategoryId;
        entity.PaymentTypeId = req.PaymentTypeId;

        // Replace tags
        _db.TransactionTags.RemoveRange(entity.TransactionTags);
        entity.TransactionTags.Clear();
        foreach (var tagId in tagIds) entity.TransactionTags.Add(new TransactionTag { TransactionId = entity.Id, TagId = tagId });

        await _db.SaveChangesAsync(ct);
        return await GetAsync(entity.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Transaction not found.");

        // Self-transfers come in pairs — delete both legs together.
        if (entity.TransferGroupId.HasValue)
        {
            var pair = await _db.Transactions
                .Where(t => t.TransferGroupId == entity.TransferGroupId && t.OwnerUserId == owner)
                .ToListAsync(ct);
            _db.Transactions.RemoveRange(pair);
        }
        else
        {
            _db.Transactions.Remove(entity);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TransactionDto>> CreateSelfTransferAsync(CreateSelfTransferRequest req, CancellationToken ct)
    {
        if (req.Amount <= 0) throw new ArgumentException("Amount must be positive.");
        if (req.SourceAccountId == req.DestinationAccountId) throw new ArgumentException("Source and destination must differ.");

        var owner = OwnerId;
        await ValidateAccountAsync(owner, req.SourceAccountId, ct);
        await ValidateAccountAsync(owner, req.DestinationAccountId, ct);
        await ValidateOptionalRefsAsync(owner, null, req.PaymentTypeId, ct);
        var tagIds = await ValidateTagIdsAsync(owner, req.TagIds, ct);

        var groupId = Guid.NewGuid();
        var reason = string.IsNullOrWhiteSpace(req.Reason) ? "Self transfer" : req.Reason.Trim();

        var debitLeg = new Transaction
        {
            OwnerUserId = owner,
            Date = req.Date,
            Type = TransactionType.Debit,
            AccountId = req.SourceAccountId,
            Amount = req.Amount,
            Reason = reason,
            Note = req.Note?.Trim(),
            PaymentTypeId = req.PaymentTypeId,
            TransferGroupId = groupId
        };
        var creditLeg = new Transaction
        {
            OwnerUserId = owner,
            Date = req.Date,
            Type = TransactionType.Credit,
            AccountId = req.DestinationAccountId,
            Amount = req.Amount,
            Reason = reason,
            Note = req.Note?.Trim(),
            PaymentTypeId = req.PaymentTypeId,
            TransferGroupId = groupId
        };
        foreach (var tagId in tagIds)
        {
            debitLeg.TransactionTags.Add(new TransactionTag { TagId = tagId });
            creditLeg.TransactionTags.Add(new TransactionTag { TagId = tagId });
        }

        _db.Transactions.AddRange(debitLeg, creditLeg);
        await _db.SaveChangesAsync(ct);

        var debitDto = await GetAsync(debitLeg.Id, ct);
        var creditDto = await GetAsync(creditLeg.Id, ct);
        return new[] { debitDto, creditDto };
    }

    public async Task<TransactionReport> GetReportAsync(DateOnly from, DateOnly to, Guid? accountId, CancellationToken ct)
    {
        if (to < from) (from, to) = (to, from);
        var owner = OwnerId;

        var q = _db.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.PaymentType)
            .Include(t => t.TransactionTags).ThenInclude(tt => tt.Tag)
            .Where(t => t.OwnerUserId == owner && t.Date >= from && t.Date <= to);

        if (accountId.HasValue) q = q.Where(t => t.AccountId == accountId.Value);

        var entities = await q.OrderBy(t => t.Date).ThenBy(t => t.CreatedAt).ToListAsync(ct);

        var rows = entities.Select(t => new TransactionReportRow(
            t.Date,
            t.Type.ToString(),
            t.Account?.Name ?? "",
            t.Amount,
            t.Reason,
            t.Note,
            t.Category?.Name,
            t.PaymentType?.Name,
            string.Join(", ", t.TransactionTags.Select(tt => tt.Tag?.Name ?? "").Where(n => n.Length > 0))
        )).ToList();

        return new TransactionReport(from, to, rows);
    }

    // ===== Helpers =====
    private async Task ValidateAccountAsync(Guid owner, Guid accountId, CancellationToken ct)
    {
        var ok = await _db.Accounts.AnyAsync(a => a.Id == accountId && a.OwnerUserId == owner, ct);
        if (!ok) throw new KeyNotFoundException("Account not found.");
    }

    private async Task ValidateOptionalRefsAsync(Guid owner, Guid? categoryId, Guid? paymentTypeId, CancellationToken ct)
    {
        if (categoryId.HasValue)
        {
            var ok = await _db.Categories.AnyAsync(c => c.Id == categoryId.Value && c.OwnerUserId == owner, ct);
            if (!ok) throw new KeyNotFoundException("Category not found.");
        }
        if (paymentTypeId.HasValue)
        {
            var ok = await _db.PaymentTypes.AnyAsync(p => p.Id == paymentTypeId.Value && p.OwnerUserId == owner, ct);
            if (!ok) throw new KeyNotFoundException("Payment type not found.");
        }
    }

    private async Task<List<Guid>> ValidateTagIdsAsync(Guid owner, IReadOnlyList<Guid>? tagIds, CancellationToken ct)
    {
        if (tagIds is null || tagIds.Count == 0) return new List<Guid>();
        var distinct = tagIds.Distinct().ToList();
        var found = await _db.Tags
            .Where(t => t.OwnerUserId == owner && distinct.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync(ct);
        if (found.Count != distinct.Count) throw new KeyNotFoundException("One or more tags not found.");
        return found;
    }

    private async Task<Dictionary<Guid, decimal>> ComputeAccountStandingForTransactionsAsync(
        Guid accountId, List<Transaction> page, CancellationToken ct)
    {
        var owner = OwnerId;
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId && a.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Account not found.");

        // Pull all transactions for this account in chronological order, compute running balance, then map.
        var ordered = await _db.Transactions
            .Where(t => t.OwnerUserId == owner && t.AccountId == accountId)
            .OrderBy(t => t.Date).ThenBy(t => t.CreatedAt)
            .Select(t => new { t.Id, t.Type, t.Amount })
            .ToListAsync(ct);

        var running = account.OpeningBalance;
        var map = new Dictionary<Guid, decimal>(ordered.Count);
        foreach (var r in ordered)
        {
            running += r.Type == TransactionType.Credit ? r.Amount : -r.Amount;
            map[r.Id] = running;
        }
        return map;
    }

    private static TransactionDto Map(Transaction t, Dictionary<Guid, decimal>? standingMap)
    {
        var tags = t.TransactionTags
            .Where(tt => tt.Tag != null)
            .OrderBy(tt => tt.Tag!.Name)
            .Select(tt => new TagBadge(tt.TagId, tt.Tag!.Name, tt.Tag!.Color))
            .ToList();

        decimal? standing = standingMap is not null && standingMap.TryGetValue(t.Id, out var s) ? s : null;

        return new TransactionDto(
            t.Id, t.Date, t.Type, t.AccountId, t.Account?.Name ?? "",
            t.Amount, t.Reason, t.Note,
            t.CategoryId, t.Category?.Name,
            t.PaymentTypeId, t.PaymentType?.Name,
            t.TransferGroupId,
            tags,
            standing);
    }
}
