using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.AssetTracker.Dashboard;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.AssetTracker.Dashboard;

public class AssetTrackerDashboardService : IAssetTrackerDashboardService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public AssetTrackerDashboardService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId ?? throw new InvalidOperationException("No authenticated user.");

    private record PricePoint(DateOnly AsOf, decimal Price);
    private record InvTx(DateOnly Date, InvestmentTxType Type, decimal Units, decimal Price);
    private record LiabPoint(DateOnly Date, LiabilityTxType Type, decimal Amount);
    private record AssetData(Guid Id, Guid GroupId, string Name, IReadOnlyList<PricePoint> Prices);
    private record InvestmentData(Guid Id, Guid GroupId, string Name, IReadOnlyList<PricePoint> Prices, IReadOnlyList<InvTx> Txs);
    private record LiabilityData(Guid Id, string Name, IReadOnlyList<LiabPoint> History);

    public async Task<AssetTrackerDashboardView> GetAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (to < from) (from, to) = (to, from);
        var owner = OwnerId;

        var assetGroups = await _db.AssetGroups.Where(g => g.OwnerUserId == owner)
            .Select(g => new { g.Id, g.Name }).ToListAsync(ct);
        var investmentGroups = await _db.InvestmentGroups.Where(g => g.OwnerUserId == owner)
            .Select(g => new { g.Id, g.Name }).ToListAsync(ct);

        // Pull current data
        var assetsRaw = await _db.Assets
            .Where(a => a.OwnerUserId == owner && a.Status == AssetStatus.InPossession)
            .Select(a => new
            {
                a.Id, a.GroupId, a.Name,
                Prices = a.PriceHistory
                    .OrderBy(p => p.AsOf).ThenBy(p => p.CreatedAt)
                    .Select(p => new { p.AsOf, p.Price })
                    .ToList()
            })
            .ToListAsync(ct);
        var assets = assetsRaw.Select(a => new AssetData(
            a.Id, a.GroupId, a.Name,
            a.Prices.Select(p => new PricePoint(p.AsOf, p.Price)).ToList())).ToList();

        var investmentsRaw = await _db.Investments
            .Where(i => i.OwnerUserId == owner && i.Status == InvestmentStatus.Active)
            .Select(i => new
            {
                i.Id, i.GroupId, i.Name,
                Prices = i.PriceHistory
                    .OrderBy(p => p.AsOf).ThenBy(p => p.CreatedAt)
                    .Select(p => new { p.AsOf, p.Price })
                    .ToList(),
                Txs = i.Transactions
                    .OrderBy(t => t.Date).ThenBy(t => t.CreatedAt)
                    .Select(t => new { t.Date, t.Type, t.Units, t.Price })
                    .ToList()
            })
            .ToListAsync(ct);
        var investments = investmentsRaw.Select(i => new InvestmentData(
            i.Id, i.GroupId, i.Name,
            i.Prices.Select(p => new PricePoint(p.AsOf, p.Price)).ToList(),
            i.Txs.Select(t => new InvTx(t.Date, t.Type, t.Units, t.Price)).ToList())).ToList();

        var liabilitiesRaw = await _db.Liabilities
            .Where(l => l.OwnerUserId == owner && l.Status == LiabilityStatus.Active)
            .Select(l => new
            {
                l.Id, l.Name,
                History = l.History
                    .OrderBy(h => h.Date).ThenBy(h => h.CreatedAt)
                    .Select(h => new { h.Date, h.Type, h.Amount })
                    .ToList()
            })
            .ToListAsync(ct);
        var liabilities = liabilitiesRaw.Select(l => new LiabilityData(
            l.Id, l.Name,
            l.History.Select(h => new LiabPoint(h.Date, h.Type, h.Amount)).ToList())).ToList();

        // ---- Snapshot at "to" ----
        decimal totalAssets = 0m;
        var assetsByGroupTotal = new Dictionary<Guid, decimal>();
        foreach (var a in assets)
        {
            var price = LatestPriceOnOrBefore(a.Prices, to);
            if (price is null) continue;
            totalAssets += price.Value;
            assetsByGroupTotal.TryGetValue(a.GroupId, out var t);
            assetsByGroupTotal[a.GroupId] = t + price.Value;
        }
        var assetSlices = assetsByGroupTotal.Select(kv => new SliceDto(
            assetGroups.FirstOrDefault(x => x.Id == kv.Key)?.Name ?? "(unknown)",
            kv.Value,
            totalAssets > 0 ? Math.Round(kv.Value / totalAssets * 100m, 2) : 0m
        )).OrderByDescending(s => s.Value).ToList();

        decimal totalInvestments = 0m;
        var invByGroupTotal = new Dictionary<Guid, decimal>();
        foreach (var i in investments)
        {
            var holding = ComputeHoldingAt(i.Txs, to);
            if (holding <= 0) continue;
            var price = LatestPriceOnOrBefore(i.Prices, to);
            if (price is null) continue;
            var v = holding * price.Value;
            totalInvestments += v;
            invByGroupTotal.TryGetValue(i.GroupId, out var t);
            invByGroupTotal[i.GroupId] = t + v;
        }
        var investmentSlices = invByGroupTotal.Select(kv => new SliceDto(
            investmentGroups.FirstOrDefault(x => x.Id == kv.Key)?.Name ?? "(unknown)",
            kv.Value,
            totalInvestments > 0 ? Math.Round(kv.Value / totalInvestments * 100m, 2) : 0m
        )).OrderByDescending(s => s.Value).ToList();

        decimal totalLiabilities = 0m;
        var liabilityBalances = new List<(string Name, decimal Bal)>();
        foreach (var l in liabilities)
        {
            var bal = ComputeLiabilityBalanceAt(l.History, to);
            if (bal <= 0) continue;
            totalLiabilities += bal;
            liabilityBalances.Add((l.Name, bal));
        }
        var liabilitySlices = liabilityBalances.Select(x => new SliceDto(
            x.Name, x.Bal,
            totalLiabilities > 0 ? Math.Round(x.Bal / totalLiabilities * 100m, 2) : 0m
        )).OrderByDescending(s => s.Value).ToList();

        var netWorth = totalAssets + totalInvestments - totalLiabilities;

        // ---- Time series ----
        var assetsSeries = new List<TimeSeriesPoint>();
        var investmentsSeries = new List<TimeSeriesPoint>();
        var liabilitiesSeries = new List<TimeSeriesPoint>();
        var netWorthSeries = new List<TimeSeriesPoint>();

        int dayCount = to.DayNumber - from.DayNumber + 1;
        int step = Math.Max(1, dayCount / 120);  // cap at ~120 sample points

        DateOnly d = from;
        while (d <= to)
        {
            decimal aSum = assets.Sum(a => LatestPriceOnOrBefore(a.Prices, d) ?? 0m);
            decimal iSum = 0m;
            foreach (var inv in investments)
            {
                var h = ComputeHoldingAt(inv.Txs, d);
                if (h <= 0) continue;
                var p = LatestPriceOnOrBefore(inv.Prices, d);
                if (p is null) continue;
                iSum += h * p.Value;
            }
            decimal lSum = liabilities.Sum(l => Math.Max(0m, ComputeLiabilityBalanceAt(l.History, d)));

            assetsSeries.Add(new TimeSeriesPoint(d, aSum));
            investmentsSeries.Add(new TimeSeriesPoint(d, iSum));
            liabilitiesSeries.Add(new TimeSeriesPoint(d, lSum));
            netWorthSeries.Add(new TimeSeriesPoint(d, aSum + iSum - lSum));

            if (d == to) break;
            var next = d.AddDays(step);
            d = next > to ? to : next;
        }

        return new AssetTrackerDashboardView(
            netWorth, totalAssets, totalInvestments, totalLiabilities,
            assetSlices, investmentSlices, liabilitySlices,
            assetsSeries, investmentsSeries, liabilitiesSeries, netWorthSeries);
    }

    private static decimal? LatestPriceOnOrBefore(IReadOnlyList<PricePoint> prices, DateOnly date)
    {
        decimal? value = null;
        DateOnly best = DateOnly.MinValue;
        foreach (var p in prices)
        {
            if (p.AsOf <= date && (value is null || p.AsOf >= best))
            {
                value = p.Price;
                best = p.AsOf;
            }
        }
        return value;
    }

    private static decimal ComputeHoldingAt(IReadOnlyList<InvTx> txs, DateOnly date)
    {
        decimal total = 0m;
        foreach (var t in txs)
        {
            if (t.Date > date) continue;
            total += t.Type == InvestmentTxType.Buy ? t.Units : -t.Units;
        }
        return total;
    }

    private static decimal ComputeLiabilityBalanceAt(IReadOnlyList<LiabPoint> history, DateOnly date)
    {
        decimal balance = 0m;
        foreach (var h in history)
        {
            if (h.Date > date) continue;
            balance += h.Type == LiabilityTxType.Acquisition ? h.Amount : -h.Amount;
        }
        return balance;
    }
}
