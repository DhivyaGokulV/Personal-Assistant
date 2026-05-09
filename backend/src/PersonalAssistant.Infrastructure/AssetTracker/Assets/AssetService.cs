using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.AssetTracker.Assets;
using PersonalAssistant.Application.AssetTracker.Common;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Domain.AssetTracker;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.AssetTracker.Assets;

public class AssetService : IAssetService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public AssetService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId ?? throw new InvalidOperationException("No authenticated user.");

    // ===== Groups =====
    public async Task<IReadOnlyList<AssetGroupDto>> GetGroupsAsync(CancellationToken ct)
    {
        var owner = OwnerId;

        var groups = await _db.AssetGroups
            .Include(g => g.Tag)
            .Where(g => g.OwnerUserId == owner)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);

        // Compute per-group totals: sum of latest price for in-possession assets
        var assets = await _db.Assets
            .Where(a => a.OwnerUserId == owner && a.Status == AssetStatus.InPossession)
            .Select(a => new
            {
                a.Id, a.GroupId,
                LatestPrice = a.PriceHistory
                    .OrderByDescending(p => p.AsOf).ThenByDescending(p => p.CreatedAt)
                    .Select(p => (decimal?)p.Price)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var byGroup = assets.GroupBy(a => a.GroupId).ToDictionary(
            g => g.Key,
            g => (Count: g.Count(), Total: g.Sum(x => x.LatestPrice ?? 0m)));

        return groups.Select(g =>
        {
            byGroup.TryGetValue(g.Id, out var stats);
            return new AssetGroupDto(
                g.Id, g.Name, g.Description,
                g.Tag is null ? null : new AssetTagBadge(g.Tag.Id, g.Tag.Name, g.Tag.Color),
                stats.Count, stats.Total);
        }).ToList();
    }

    public async Task<AssetGroupDto> CreateGroupAsync(CreateAssetGroupRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        await ValidateTagAsync(owner, req.TagId, ct);

        var entity = new AssetGroup
        {
            OwnerUserId = owner,
            Name = req.Name.Trim(),
            Description = req.Description?.Trim(),
            TagId = req.TagId
        };
        _db.AssetGroups.Add(entity);
        await _db.SaveChangesAsync(ct);
        return await GetGroupDtoAsync(entity.Id, ct);
    }

    public async Task<AssetGroupDto> UpdateGroupAsync(Guid id, UpdateAssetGroupRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.AssetGroups.FirstOrDefaultAsync(g => g.Id == id && g.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Asset group not found.");
        await ValidateTagAsync(owner, req.TagId, ct);

        entity.Name = req.Name.Trim();
        entity.Description = req.Description?.Trim();
        entity.TagId = req.TagId;
        await _db.SaveChangesAsync(ct);
        return await GetGroupDtoAsync(id, ct);
    }

    public async Task DeleteGroupAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.AssetGroups
            .Include(g => g.Assets)
            .FirstOrDefaultAsync(g => g.Id == id && g.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Asset group not found.");

        // Cascade soft-delete all assets and their price history
        var assetIds = entity.Assets.Select(a => a.Id).ToList();
        if (assetIds.Count > 0)
        {
            var prices = await _db.AssetPriceHistory.Where(p => assetIds.Contains(p.AssetId)).ToListAsync(ct);
            _db.AssetPriceHistory.RemoveRange(prices);
            _db.Assets.RemoveRange(entity.Assets);
        }
        _db.AssetGroups.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<AssetGroupDto> GetGroupDtoAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var g = await _db.AssetGroups.Include(x => x.Tag).FirstAsync(x => x.Id == id && x.OwnerUserId == owner, ct);
        var stats = await _db.Assets
            .Where(a => a.GroupId == id && a.Status == AssetStatus.InPossession)
            .Select(a => new
            {
                Latest = a.PriceHistory
                    .OrderByDescending(p => p.AsOf).ThenByDescending(p => p.CreatedAt)
                    .Select(p => (decimal?)p.Price).FirstOrDefault()
            })
            .ToListAsync(ct);

        return new AssetGroupDto(
            g.Id, g.Name, g.Description,
            g.Tag is null ? null : new AssetTagBadge(g.Tag.Id, g.Tag.Name, g.Tag.Color),
            stats.Count, stats.Sum(x => x.Latest ?? 0m));
    }

    // ===== Assets =====
    public async Task<IReadOnlyList<AssetDto>> ListAsync(AssetStatus? status, Guid? tagId, Guid? groupId, CancellationToken ct)
    {
        var owner = OwnerId;
        var q = _db.Assets
            .Include(a => a.Tag)
            .Include(a => a.Group)
            .Where(a => a.OwnerUserId == owner);

        if (status.HasValue) q = q.Where(a => a.Status == status.Value);
        if (tagId.HasValue) q = q.Where(a => a.TagId == tagId.Value);
        if (groupId.HasValue) q = q.Where(a => a.GroupId == groupId.Value);

        var rows = await q
            .OrderBy(a => a.Group!.Name).ThenBy(a => a.Name)
            .Select(a => new
            {
                a.Id, a.GroupId, GroupName = a.Group!.Name, a.Name, a.Description,
                Tag = a.Tag,
                a.BuyingDate, a.BuyingPrice, a.SellingDate, a.SellingPrice, a.Status,
                Latest = a.PriceHistory
                    .OrderByDescending(p => p.AsOf).ThenByDescending(p => p.CreatedAt)
                    .Select(p => new { p.AsOf, p.Price })
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        return rows.Select(r => new AssetDto(
            r.Id, r.GroupId, r.GroupName, r.Name, r.Description,
            r.Tag is null ? null : new AssetTagBadge(r.Tag.Id, r.Tag.Name, r.Tag.Color),
            r.BuyingDate, r.BuyingPrice, r.SellingDate, r.SellingPrice, r.Status,
            r.Latest is null ? null : r.Latest.Price,
            r.Latest is null ? null : (DateOnly?)r.Latest.AsOf
        )).ToList();
    }

    public async Task<AssetWithHistoryDto> GetAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.Assets
            .Include(a => a.Tag)
            .Include(a => a.Group)
            .Include(a => a.PriceHistory.OrderByDescending(p => p.AsOf))
            .FirstOrDefaultAsync(a => a.Id == id && a.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Asset not found.");

        var latest = entity.PriceHistory
            .OrderByDescending(p => p.AsOf).ThenByDescending(p => p.CreatedAt)
            .FirstOrDefault();

        var dto = new AssetDto(
            entity.Id, entity.GroupId, entity.Group!.Name, entity.Name, entity.Description,
            entity.Tag is null ? null : new AssetTagBadge(entity.Tag.Id, entity.Tag.Name, entity.Tag.Color),
            entity.BuyingDate, entity.BuyingPrice, entity.SellingDate, entity.SellingPrice, entity.Status,
            latest?.Price, latest?.AsOf);

        var history = entity.PriceHistory
            .OrderByDescending(p => p.AsOf).ThenByDescending(p => p.CreatedAt)
            .Select(p => new AssetPriceHistoryDto(p.Id, p.AsOf, p.Price, p.Note))
            .ToList();

        return new AssetWithHistoryDto(dto, history);
    }

    public async Task<AssetDto> CreateAsync(CreateAssetRequest req, CancellationToken ct)
    {
        if (req.CurrentPrice <= 0) throw new ArgumentException("Current price must be positive.");
        var owner = OwnerId;

        var groupOk = await _db.AssetGroups.AnyAsync(g => g.Id == req.GroupId && g.OwnerUserId == owner, ct);
        if (!groupOk) throw new KeyNotFoundException("Asset group not found.");
        await ValidateTagAsync(owner, req.TagId, ct);

        var asset = new Asset
        {
            OwnerUserId = owner,
            GroupId = req.GroupId,
            Name = req.Name.Trim(),
            Description = req.Description?.Trim(),
            TagId = req.TagId,
            BuyingDate = req.BuyingDate,
            BuyingPrice = req.BuyingPrice,
            SellingDate = req.SellingDate,
            SellingPrice = req.SellingPrice,
            Status = req.Status
        };

        var initialDate = req.BuyingDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        asset.PriceHistory.Add(new AssetPriceHistory
        {
            OwnerUserId = owner,
            AsOf = initialDate,
            Price = req.CurrentPrice,
            Note = "Initial"
        });

        _db.Assets.Add(asset);
        await _db.SaveChangesAsync(ct);

        return (await GetAsync(asset.Id, ct)).Asset;
    }

    public async Task<AssetDto> UpdateAsync(Guid id, UpdateAssetRequest req, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.Assets.FirstOrDefaultAsync(a => a.Id == id && a.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Asset not found.");

        if (entity.GroupId != req.GroupId)
        {
            var groupOk = await _db.AssetGroups.AnyAsync(g => g.Id == req.GroupId && g.OwnerUserId == owner, ct);
            if (!groupOk) throw new KeyNotFoundException("Target group not found.");
            entity.GroupId = req.GroupId;
        }
        await ValidateTagAsync(owner, req.TagId, ct);

        entity.Name = req.Name.Trim();
        entity.Description = req.Description?.Trim();
        entity.TagId = req.TagId;
        entity.BuyingDate = req.BuyingDate;
        entity.BuyingPrice = req.BuyingPrice;
        entity.SellingDate = req.SellingDate;
        entity.SellingPrice = req.SellingPrice;
        entity.Status = req.Status;

        await _db.SaveChangesAsync(ct);
        return (await GetAsync(id, ct)).Asset;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.Assets
            .Include(a => a.PriceHistory)
            .FirstOrDefaultAsync(a => a.Id == id && a.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Asset not found.");
        _db.AssetPriceHistory.RemoveRange(entity.PriceHistory);
        _db.Assets.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    // ===== Price history =====
    public async Task<AssetPriceHistoryDto> AddPriceAsync(Guid assetId, AddAssetPriceRequest req, CancellationToken ct)
    {
        if (req.Price <= 0) throw new ArgumentException("Price must be positive.");
        var owner = OwnerId;
        var ok = await _db.Assets.AnyAsync(a => a.Id == assetId && a.OwnerUserId == owner, ct);
        if (!ok) throw new KeyNotFoundException("Asset not found.");

        var entity = new AssetPriceHistory
        {
            OwnerUserId = owner,
            AssetId = assetId,
            AsOf = req.AsOf,
            Price = req.Price,
            Note = req.Note?.Trim()
        };
        _db.AssetPriceHistory.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new AssetPriceHistoryDto(entity.Id, entity.AsOf, entity.Price, entity.Note);
    }

    public async Task<AssetPriceHistoryDto> UpdatePriceAsync(Guid assetId, Guid priceId, UpdateAssetPriceRequest req, CancellationToken ct)
    {
        if (req.Price <= 0) throw new ArgumentException("Price must be positive.");
        var owner = OwnerId;
        var entity = await _db.AssetPriceHistory
            .FirstOrDefaultAsync(p => p.Id == priceId && p.AssetId == assetId && p.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Price entry not found.");

        entity.AsOf = req.AsOf;
        entity.Price = req.Price;
        entity.Note = req.Note?.Trim();
        await _db.SaveChangesAsync(ct);
        return new AssetPriceHistoryDto(entity.Id, entity.AsOf, entity.Price, entity.Note);
    }

    public async Task DeletePriceAsync(Guid assetId, Guid priceId, CancellationToken ct)
    {
        var owner = OwnerId;
        var entity = await _db.AssetPriceHistory
            .FirstOrDefaultAsync(p => p.Id == priceId && p.AssetId == assetId && p.OwnerUserId == owner, ct)
            ?? throw new KeyNotFoundException("Price entry not found.");

        // Don't allow deleting the only history entry â€” current price is mandatory.
        var count = await _db.AssetPriceHistory.CountAsync(p => p.AssetId == assetId, ct);
        if (count <= 1) throw new InvalidOperationException("An asset must have at least one current price entry.");

        _db.AssetPriceHistory.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    // ===== Reports =====
    public async Task<AssetReport> GetReportAsync(AssetStatus? status, CancellationToken ct)
    {
        var owner = OwnerId;
        var q = _db.Assets
            .Include(a => a.Group)
            .Include(a => a.Tag)
            .Where(a => a.OwnerUserId == owner);
        if (status.HasValue) q = q.Where(a => a.Status == status.Value);

        var rows = await q
            .OrderBy(a => a.Group!.Name).ThenBy(a => a.Name)
            .Select(a => new
            {
                GroupName = a.Group!.Name,
                a.Name,
                TagName = a.Tag != null ? a.Tag.Name : null,
                a.Status,
                a.BuyingDate, a.BuyingPrice, a.SellingDate, a.SellingPrice,
                Latest = a.PriceHistory
                    .OrderByDescending(p => p.AsOf).ThenByDescending(p => p.CreatedAt)
                    .Select(p => (decimal?)p.Price)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var dtoRows = rows.Select(r => new AssetReportRow(
            r.GroupName, r.Name, r.TagName, r.Status.ToString(),
            r.BuyingDate, r.BuyingPrice, r.SellingDate, r.SellingPrice, r.Latest)).ToList();
        return new AssetReport(dtoRows);
    }

    private async Task ValidateTagAsync(Guid owner, Guid? tagId, CancellationToken ct)
    {
        if (!tagId.HasValue) return;
        var ok = await _db.AssetTags.AnyAsync(t => t.Id == tagId.Value && t.OwnerUserId == owner, ct);
        if (!ok) throw new KeyNotFoundException("Tag not found.");
    }
}
