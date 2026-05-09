using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.AssetTracker.Common;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Domain.AssetTracker;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.AssetTracker.Common;

public class AssetTagService : IAssetTagService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public AssetTagService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId
        ?? throw new InvalidOperationException("No authenticated user.");

    public async Task<IReadOnlyList<AssetTagDto>> ListAsync(CancellationToken ct)
    {
        var owner = OwnerId;
        return await _db.AssetTags
            .Where(t => t.OwnerUserId == owner)
            .OrderBy(t => t.Name)
            .Select(t => new AssetTagDto(t.Id, t.Name, t.Description, t.Color))
            .ToListAsync(ct);
    }

    public async Task<AssetTagDto> CreateAsync(CreateAssetTagRequest req, CancellationToken ct)
    {
        var entity = new AssetTag
        {
            OwnerUserId = OwnerId,
            Name = req.Name.Trim(),
            Description = req.Description?.Trim(),
            Color = string.IsNullOrWhiteSpace(req.Color) ? "#6c5ce7" : req.Color.Trim()
        };
        _db.AssetTags.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new AssetTagDto(entity.Id, entity.Name, entity.Description, entity.Color);
    }

    public async Task<AssetTagDto> UpdateAsync(Guid id, UpdateAssetTagRequest req, CancellationToken ct)
    {
        var entity = await _db.AssetTags.FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Tag not found.");
        entity.Name = req.Name.Trim();
        entity.Description = req.Description?.Trim();
        if (!string.IsNullOrWhiteSpace(req.Color)) entity.Color = req.Color.Trim();
        await _db.SaveChangesAsync(ct);
        return new AssetTagDto(entity.Id, entity.Name, entity.Description, entity.Color);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.AssetTags.FirstOrDefaultAsync(t => t.Id == id && t.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Tag not found.");
        _db.AssetTags.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
