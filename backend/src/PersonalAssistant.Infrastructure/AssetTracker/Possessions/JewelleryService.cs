using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.AssetTracker.Possessions;
using PersonalAssistant.Application.AssetTracker.PreciousMetals;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Domain.AssetTracker;
using PersonalAssistant.Domain.Enums;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.AssetTracker.Possessions;

public class JewelleryService : IJewelleryService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public JewelleryService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId ?? throw new InvalidOperationException("No authenticated user.");
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public async Task<AssetTrackerPage<JewelleryDto>> ListAsync(PossessionQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var q = _db.JewelleryItems.AsNoTracking().Where(x => x.OwnerUserId == OwnerId);
        if (query.Status.HasValue) q = q.Where(x => x.Status == query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(x => x.Name.Contains(search) || (x.Description != null && x.Description.Contains(search)));
        }

        q = (query.SortBy, query.SortDirection) switch
        {
            (PossessionSort.BuyingDate, AssetSortDirection.Asc) => q.OrderBy(x => x.BuyingDate).ThenBy(x => x.Name),
            (PossessionSort.BuyingDate, AssetSortDirection.Desc) => q.OrderByDescending(x => x.BuyingDate).ThenBy(x => x.Name),
            (PossessionSort.BuyingPrice, AssetSortDirection.Asc) => q.OrderBy(x => x.BuyingPrice).ThenBy(x => x.Name),
            (PossessionSort.BuyingPrice, AssetSortDirection.Desc) => q.OrderByDescending(x => x.BuyingPrice).ThenBy(x => x.Name),
            (PossessionSort.Status, AssetSortDirection.Asc) => q.OrderBy(x => x.Status).ThenBy(x => x.Name),
            (PossessionSort.Status, AssetSortDirection.Desc) => q.OrderByDescending(x => x.Status).ThenBy(x => x.Name),
            (PossessionSort.SellingDate, AssetSortDirection.Asc) => q.OrderBy(x => x.SellingDate).ThenBy(x => x.Name),
            (PossessionSort.SellingDate, AssetSortDirection.Desc) => q.OrderByDescending(x => x.SellingDate).ThenBy(x => x.Name),
            (_, AssetSortDirection.Desc) => q.OrderByDescending(x => x.Name),
            _ => q.OrderBy(x => x.Name)
        };
        var total = await q.CountAsync(ct);
        var entities = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var rows = entities.Select(Map).ToList();
        return new AssetTrackerPage<JewelleryDto>(rows, page, pageSize, total);
    }

    public async Task<JewelleryDto> CreateAsync(SaveJewelleryRequest request, CancellationToken ct)
    {
        Validate(request, isCreate: true);
        var entity = new JewelleryItem
        {
            OwnerUserId = OwnerId,
            Name = request.Name.Trim(),
            Description = Clean(request.Description),
            BuyingDate = request.BuyingDate,
            BuyingPrice = request.BuyingPrice,
            QuantityInGrams = request.QuantityInGrams,
            Status = AssetStatus.InPossession,
            CurrencyCode = "INR"
        };
        _db.JewelleryItems.Add(entity);
        AddAudit(entity.Id, InvestmentAuditAction.Create, null, Snapshot(entity), null);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<JewelleryDto> UpdateAsync(Guid id, SaveJewelleryRequest request, CancellationToken ct)
    {
        var entity = await _db.JewelleryItems.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Jewel not found.");
        Validate(request, isCreate: false);
        var old = Snapshot(entity);
        var changed = new List<string>();
        var requestedStatus = request.Status ?? entity.Status;
        if (entity.Status == AssetStatus.InPossession && requestedStatus == AssetStatus.Sold)
            throw new InvalidOperationException("Use the sell action to mark a jewel as sold.");
        if (entity.Name != request.Name.Trim()) changed.Add("name");
        if (entity.Description != Clean(request.Description)) changed.Add("description");
        if (entity.BuyingDate != request.BuyingDate) changed.Add("buyingDate");
        if (entity.BuyingPrice != request.BuyingPrice) changed.Add("buyingPrice");
        if (entity.QuantityInGrams != request.QuantityInGrams) changed.Add("quantityInGrams");
        if (entity.Status != requestedStatus) changed.Add("status");
        entity.Name = request.Name.Trim();
        entity.Description = Clean(request.Description);
        entity.BuyingDate = request.BuyingDate;
        entity.BuyingPrice = request.BuyingPrice;
        entity.QuantityInGrams = request.QuantityInGrams;
        if (entity.Status == AssetStatus.Sold && requestedStatus == AssetStatus.InPossession)
        {
            entity.Status = AssetStatus.InPossession;
            entity.SellingDate = null;
            entity.SellingPrice = null;
            entity.SellingNote = null;
            changed.AddRange(new[] { "sellingDate", "sellingPrice", "sellingNote" });
        }
        if (changed.Count > 0) AddAudit(id, InvestmentAuditAction.Update, old, Snapshot(entity), changed.Distinct());
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<JewelleryDto> SellAsync(Guid id, SellPossessionRequest request, CancellationToken ct)
    {
        var entity = await _db.JewelleryItems.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Jewel not found.");
        ValidateSell(entity.BuyingDate, request);
        if (entity.Status == AssetStatus.Sold) throw new InvalidOperationException("This jewel is already sold.");
        var old = Snapshot(entity);
        entity.Status = AssetStatus.Sold;
        entity.SellingDate = request.SellingDate;
        entity.SellingPrice = request.SellingPrice;
        entity.SellingNote = Clean(request.SellingNote);
        AddAudit(id, InvestmentAuditAction.Update, old, Snapshot(entity), new[] { "status", "sellingDate", "sellingPrice", "sellingNote" });
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.JewelleryItems.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Jewel not found.");
        AddAudit(id, InvestmentAuditAction.Delete, Snapshot(entity), null, null);
        _db.JewelleryItems.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<JewelleryDto>> GetExportAsync(PossessionExportRequest request, CancellationToken ct)
    {
        ValidateExport(request);
        var rows = await _db.JewelleryItems.AsNoTracking()
            .Where(x => x.OwnerUserId == OwnerId && x.BuyingDate <= request.To && (x.SellingDate ?? x.BuyingDate) >= request.From)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    private static void Validate(SaveJewelleryRequest request, bool isCreate)
    {
        var n = request.Name?.Trim() ?? string.Empty;
        if (n.Length is < 3 or > 30) throw new ArgumentException("Name must contain between 3 and 30 characters.");
        if ((request.Description?.Trim().Length ?? 0) > 200) throw new ArgumentException("Description must not exceed 200 characters.");
        if (request.BuyingDate > Today) throw new ArgumentException("Buying date cannot be in the future.");
        if (request.BuyingPrice <= 0) throw new ArgumentException("Buying price must be greater than zero.");
        if (request.QuantityInGrams <= 0) throw new ArgumentException("Quantity must be greater than zero.");
        if (isCreate && request.Status.HasValue && request.Status != AssetStatus.InPossession)
            throw new ArgumentException("New jewellery must start in possession.");
    }

    private static void ValidateSell(DateOnly buyingDate, SellPossessionRequest request)
    {
        if ((request.SellingNote?.Trim().Length ?? 0) > 200) throw new ArgumentException("Selling note must not exceed 200 characters.");
        if (request.SellingDate < buyingDate) throw new ArgumentException("Selling date cannot be earlier than buying date.");
        if (request.SellingDate > Today) throw new ArgumentException("Selling date cannot be in the future.");
        if (request.SellingPrice <= 0) throw new ArgumentException("Selling price must be greater than zero.");
    }

    private static void ValidateExport(PossessionExportRequest request)
    {
        if (request.To < request.From) throw new ArgumentException("The end date must be on or after the start date.");
        if (request.To > request.From.AddYears(3)) throw new ArgumentException("Export duration cannot exceed three years.");
    }

    private void AddAudit(Guid itemId, InvestmentAuditAction action, string? oldValues, string? newValues, IEnumerable<string>? changedFields)
    {
        _db.JewelleryAuditEntries.Add(new JewelleryAuditEntry
        {
            OwnerUserId = OwnerId,
            JewelleryItemId = itemId,
            Action = action,
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            ChangedFieldsJson = changedFields is null ? null : JsonSerializer.Serialize(changedFields)
        });
    }

    private static JewelleryDto Map(JewelleryItem x) => new(x.Id, x.Name, x.Description, x.BuyingDate, x.BuyingPrice,
        x.QuantityInGrams, x.Status, x.SellingDate, x.SellingPrice, x.SellingNote, x.CurrencyCode);
    private static string Snapshot(JewelleryItem x) => JsonSerializer.Serialize(new
        { x.Id, x.Name, x.Description, x.BuyingDate, x.BuyingPrice, x.QuantityInGrams, x.Status, x.SellingDate, x.SellingPrice, x.SellingNote, x.CurrencyCode });
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
