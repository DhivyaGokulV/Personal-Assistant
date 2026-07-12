using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Application.Finance.Dashboard;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.Finance.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public DashboardService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId
        ?? throw new InvalidOperationException("No authenticated user.");

    public async Task<DashboardView> GetAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (to < from) (from, to) = (to, from);
        var owner = OwnerId;

        var accounts = await _db.Accounts
            .Where(a => a.OwnerUserId == owner)
            .Select(a => new { a.Id, a.Name, a.OpeningBalance, a.OpeningDate, a.Status })
            .ToListAsync(ct);

        // Pull all transactions once and aggregate in memory to keep this readable.
        var allTx = await _db.Transactions
            .Where(t => t.OwnerUserId == owner && t.Date <= to)
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.PaymentType)
            .Include(t => t.TransactionTags).ThenInclude(tt => tt.Tag)
            .ToListAsync(ct);

        var startingThreshold = from;  // include everything strictly before `from`
        var inRange = allTx.Where(t => t.Date >= from && t.Date <= to).ToList();
        var beforeRange = allTx.Where(t => t.Date < startingThreshold).ToList();

        // Per-account standing
        var accountDtos = new List<AccountStandingDto>(accounts.Count);
        foreach (var a in accounts)
        {
            decimal start = a.OpeningBalance;
            foreach (var t in beforeRange.Where(x => x.AccountId == a.Id))
                start += t.Type == TransactionType.Credit ? t.Amount : -t.Amount;

            decimal current = start;
            foreach (var t in inRange.Where(x => x.AccountId == a.Id))
                current += t.Type == TransactionType.Credit ? t.Amount : -t.Amount;

            accountDtos.Add(new AccountStandingDto(a.Id, a.Name, start, current, current - start));
        }

        // Aggregate transactions in [from, to] excluding self-transfer legs (they net to zero across own accounts).
        var spendingTx = inRange.Where(t => !t.TransferGroupId.HasValue).ToList();

        decimal totalDebits = spendingTx.Where(t => t.Type == TransactionType.Debit).Sum(t => t.Amount);
        decimal totalCredits = spendingTx.Where(t => t.Type == TransactionType.Credit).Sum(t => t.Amount);

        var byCategory = spendingTx
            .GroupBy(t => t.Category?.Name ?? "(none)")
            .Select(g => Stat(g.Key, g))
            .OrderByDescending(g => g.Debits)
            .ToList();

        var byAccount = inRange  // include transfers here so users see the full account flow
            .GroupBy(t => t.Account?.Name ?? "(unknown)")
            .Select(g => Stat(g.Key, g))
            .OrderByDescending(g => g.Debits + g.Credits)
            .ToList();

        var byPaymentType = spendingTx
            .GroupBy(t => t.PaymentType?.Name ?? "(none)")
            .Select(g => Stat(g.Key, g))
            .OrderByDescending(g => g.Debits)
            .ToList();

        var byTag = spendingTx
            .SelectMany(t => t.TransactionTags
                .Where(tt => tt.Tag != null)
                .Select(tt => new { TagName = tt.Tag!.Name, Tx = t }))
            .GroupBy(x => x.TagName)
            .Select(g => new GroupStat(
                g.Key,
                g.Where(x => x.Tx.Type == TransactionType.Debit).Sum(x => x.Tx.Amount),
                g.Where(x => x.Tx.Type == TransactionType.Credit).Sum(x => x.Tx.Amount),
                g.Where(x => x.Tx.Type == TransactionType.Credit).Sum(x => x.Tx.Amount)
                  - g.Where(x => x.Tx.Type == TransactionType.Debit).Sum(x => x.Tx.Amount)))
            .OrderByDescending(g => g.Debits)
            .ToList();

        return new DashboardView(
            from, to,
            accountDtos.Sum(a => a.StartingStanding),
            accountDtos.Sum(a => a.CurrentStanding),
            totalDebits, totalCredits,
            accountDtos, byCategory, byAccount, byPaymentType, byTag);
    }

    private static GroupStat Stat(string key, IEnumerable<Domain.Finance.Transaction> g)
    {
        var debits = g.Where(t => t.Type == TransactionType.Debit).Sum(t => t.Amount);
        var credits = g.Where(t => t.Type == TransactionType.Credit).Sum(t => t.Amount);
        return new GroupStat(key, debits, credits, credits - debits);
    }
}
