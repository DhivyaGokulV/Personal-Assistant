using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Application.Common.Interfaces;
using PersonalAssistant.Application.PasswordVault;
using PersonalAssistant.Domain.PasswordVault;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Infrastructure.PasswordVault;

public class PasswordVaultService : IPasswordVaultService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _user;

    public PasswordVaultService(AppDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    private Guid OwnerId => _user.UserId ?? throw new InvalidOperationException("No authenticated user.");

    public async Task<PasswordVaultStatusDto> GetStatusAsync(CancellationToken ct)
    {
        var setting = await _db.PasswordVaultSettings.FirstOrDefaultAsync(x => x.OwnerUserId == OwnerId, ct);
        return setting is null
            ? new PasswordVaultStatusDto(false, null, null, null, null)
            : MapStatus(setting);
    }

    public async Task<PasswordVaultStatusDto> InitializeAsync(InitializeVaultRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Salt) || string.IsNullOrWhiteSpace(req.VerifierCipherText) || string.IsNullOrWhiteSpace(req.VerifierIv))
            throw new ArgumentException("Vault salt and verifier are required.");
        if (req.KdfIterations < 100_000) throw new ArgumentException("KDF iterations must be at least 100000.");
        ValidateWrappedKey(req.MasterWrappedKeyCipherText, req.MasterWrappedKeyIv, "Master wrapped key");
        ValidateRecoveryMetadata(req);

        var owner = OwnerId;
        var existing = await _db.PasswordVaultSettings.FirstOrDefaultAsync(x => x.OwnerUserId == owner, ct);
        if (existing is not null) throw new InvalidOperationException("Password vault is already initialized.");

        var setting = new PasswordVaultSetting
        {
            OwnerUserId = owner,
            Salt = req.Salt,
            VerifierCipherText = req.VerifierCipherText,
            VerifierIv = req.VerifierIv,
            KdfIterations = req.KdfIterations,
            MasterWrappedKeyCipherText = req.MasterWrappedKeyCipherText,
            MasterWrappedKeyIv = req.MasterWrappedKeyIv,
            RecoverySalt = req.RecoverySalt,
            RecoveryVerifierCipherText = req.RecoveryVerifierCipherText,
            RecoveryVerifierIv = req.RecoveryVerifierIv,
            RecoveryWrappedKeyCipherText = req.RecoveryWrappedKeyCipherText,
            RecoveryWrappedKeyIv = req.RecoveryWrappedKeyIv,
            RecoveryKdfIterations = req.RecoveryKdfIterations
        };
        _db.PasswordVaultSettings.Add(setting);
        await _db.SaveChangesAsync(ct);
        return MapStatus(setting);
    }

    public async Task<PasswordVaultStatusDto> ResetMasterPasswordAsync(ResetMasterPasswordRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Salt) || string.IsNullOrWhiteSpace(req.VerifierCipherText) || string.IsNullOrWhiteSpace(req.VerifierIv))
            throw new ArgumentException("Vault salt and verifier are required.");
        if (string.IsNullOrWhiteSpace(req.MasterWrappedKeyCipherText) || string.IsNullOrWhiteSpace(req.MasterWrappedKeyIv))
            throw new ArgumentException("Wrapped vault key is required.");
        if (req.KdfIterations < 100_000) throw new ArgumentException("KDF iterations must be at least 100000.");

        var setting = await _db.PasswordVaultSettings.FirstOrDefaultAsync(x => x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Password vault is not initialized.");
        setting.Salt = req.Salt;
        setting.VerifierCipherText = req.VerifierCipherText;
        setting.VerifierIv = req.VerifierIv;
        setting.KdfIterations = req.KdfIterations;
        setting.MasterWrappedKeyCipherText = req.MasterWrappedKeyCipherText;
        setting.MasterWrappedKeyIv = req.MasterWrappedKeyIv;
        await _db.SaveChangesAsync(ct);
        return MapStatus(setting);
    }

    public async Task<IReadOnlyList<PasswordGroupDto>> ListGroupsAsync(CancellationToken ct)
    {
        var groups = await _db.PasswordGroups.Include(x => x.Entries).Where(x => x.OwnerUserId == OwnerId).OrderBy(x => x.Name).ToListAsync(ct);
        return groups.Select(MapGroup).ToList();
    }

    public async Task<PasswordGroupDto> CreateGroupAsync(PasswordGroupRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Group name is required.");
        var group = new PasswordGroup { OwnerUserId = OwnerId, Name = req.Name.Trim(), Description = req.Description?.Trim() };
        _db.PasswordGroups.Add(group);
        await _db.SaveChangesAsync(ct);
        return MapGroup(group);
    }

    public async Task<PasswordGroupDto> UpdateGroupAsync(Guid id, PasswordGroupRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Group name is required.");
        var group = await _db.PasswordGroups.Include(x => x.Entries).FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Password group not found.");
        group.Name = req.Name.Trim(); group.Description = req.Description?.Trim();
        await _db.SaveChangesAsync(ct);
        return MapGroup(group);
    }

    public async Task DeleteGroupAsync(Guid id, CancellationToken ct)
    {
        var group = await _db.PasswordGroups.Include(x => x.Entries).ThenInclude(x => x.History).FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Password group not found.");
        foreach (var entry in group.Entries) _db.PasswordHistory.RemoveRange(entry.History);
        _db.PasswordEntries.RemoveRange(group.Entries);
        _db.PasswordGroups.Remove(group);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PasswordEntryDto>> ListEntriesAsync(Guid? groupId, string? search, CancellationToken ct)
    {
        var q = _db.PasswordEntries.Include(x => x.Group).Include(x => x.History).Where(x => x.OwnerUserId == OwnerId);
        if (groupId.HasValue) q = q.Where(x => x.GroupId == groupId.Value);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => EF.Functions.Like(x.Name, $"%{search.Trim()}%"));
        var entries = await q.OrderBy(x => x.Name).ToListAsync(ct);
        return entries.Select(MapEntry).ToList();
    }

    public async Task<PasswordEntryDto> GetEntryAsync(Guid id, CancellationToken ct)
    {
        var entry = await _db.PasswordEntries.Include(x => x.Group).Include(x => x.History)
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Password entry not found.");
        return MapEntry(entry);
    }

    public async Task<PasswordEntryDto> CreateEntryAsync(PasswordEntryRequest req, CancellationToken ct)
    {
        ValidateEntry(req);
        var groupOk = await _db.PasswordGroups.AnyAsync(x => x.Id == req.GroupId && x.OwnerUserId == OwnerId, ct);
        if (!groupOk) throw new KeyNotFoundException("Password group not found.");
        var entry = new PasswordEntry { OwnerUserId = OwnerId };
        ApplyEntry(entry, req);
        _db.PasswordEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
        return await GetEntryAsync(entry.Id, ct);
    }

    public async Task<PasswordEntryDto> UpdateEntryAsync(Guid id, PasswordEntryRequest req, CancellationToken ct)
    {
        ValidateEntry(req);
        var groupOk = await _db.PasswordGroups.AnyAsync(x => x.Id == req.GroupId && x.OwnerUserId == OwnerId, ct);
        if (!groupOk) throw new KeyNotFoundException("Password group not found.");
        var entry = await _db.PasswordEntries.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Password entry not found.");
        ApplyEntry(entry, req);
        await _db.SaveChangesAsync(ct);
        return await GetEntryAsync(id, ct);
    }

    public async Task DeleteEntryAsync(Guid id, CancellationToken ct)
    {
        var entry = await _db.PasswordEntries.Include(x => x.History).FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Password entry not found.");
        _db.PasswordHistory.RemoveRange(entry.History);
        _db.PasswordEntries.Remove(entry);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PasswordHistoryDto> AddHistoryAsync(Guid entryId, PasswordHistoryRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.PreviousPassword.CipherText) || string.IsNullOrWhiteSpace(req.PreviousPassword.Iv))
            throw new ArgumentException("Encrypted previous password is required.");
        var exists = await _db.PasswordEntries.AnyAsync(x => x.Id == entryId && x.OwnerUserId == OwnerId, ct);
        if (!exists) throw new KeyNotFoundException("Password entry not found.");
        var history = new PasswordHistory { OwnerUserId = OwnerId, PasswordEntryId = entryId, ChangeDate = req.ChangeDate, PreviousPasswordCipherText = req.PreviousPassword.CipherText, PreviousPasswordIv = req.PreviousPassword.Iv };
        _db.PasswordHistory.Add(history);
        await _db.SaveChangesAsync(ct);
        return MapHistory(history);
    }

    public async Task DeleteHistoryAsync(Guid entryId, Guid historyId, CancellationToken ct)
    {
        var history = await _db.PasswordHistory.FirstOrDefaultAsync(x => x.Id == historyId && x.PasswordEntryId == entryId && x.OwnerUserId == OwnerId, ct)
            ?? throw new KeyNotFoundException("Password history entry not found.");
        _db.PasswordHistory.Remove(history);
        await _db.SaveChangesAsync(ct);
    }

    private static void ValidateEntry(PasswordEntryRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Entry name is required.");
        if (!req.HasUsername && !req.HasEmail) throw new ArgumentException("Username or email is required.");
        if (req.HasUsername && (req.Username is null || string.IsNullOrWhiteSpace(req.Username.CipherText) || string.IsNullOrWhiteSpace(req.Username.Iv))) throw new ArgumentException("Encrypted username is required.");
        if (req.HasEmail && (req.Email is null || string.IsNullOrWhiteSpace(req.Email.CipherText) || string.IsNullOrWhiteSpace(req.Email.Iv))) throw new ArgumentException("Encrypted email is required.");
    }

    private static void ValidateWrappedKey(string? cipherText, string? iv, string label)
    {
        if (string.IsNullOrWhiteSpace(cipherText) != string.IsNullOrWhiteSpace(iv))
            throw new ArgumentException($"{label} ciphertext and IV must be provided together.");
    }

    private static void ValidateRecoveryMetadata(InitializeVaultRequest req)
    {
        var values = new[]
        {
            req.RecoverySalt,
            req.RecoveryVerifierCipherText,
            req.RecoveryVerifierIv,
            req.RecoveryWrappedKeyCipherText,
            req.RecoveryWrappedKeyIv
        };
        var any = values.Any(x => !string.IsNullOrWhiteSpace(x)) || req.RecoveryKdfIterations.HasValue;
        if (!any) return;

        if (values.Any(string.IsNullOrWhiteSpace) || !req.RecoveryKdfIterations.HasValue)
            throw new ArgumentException("Recovery salt, verifier, wrapped key, IVs, and KDF iterations are required together.");
        if (req.RecoveryKdfIterations.Value < 100_000)
            throw new ArgumentException("Recovery KDF iterations must be at least 100000.");
    }

    private static void ApplyEntry(PasswordEntry e, PasswordEntryRequest r)
    {
        e.GroupId = r.GroupId; e.Name = r.Name.Trim(); e.HasUsername = r.HasUsername; e.HasEmail = r.HasEmail;
        e.UsernameCipherText = r.Username?.CipherText; e.UsernameIv = r.Username?.Iv;
        e.EmailCipherText = r.Email?.CipherText; e.EmailIv = r.Email?.Iv;
        e.PasswordCipherText = r.Password?.CipherText; e.PasswordIv = r.Password?.Iv;
        e.CreatedDate = r.CreatedDate; e.UpdatedDate = r.UpdatedDate;
    }

    private static PasswordGroupDto MapGroup(PasswordGroup g) => new(g.Id, g.Name, g.Description, g.Entries.Count);
    private static PasswordVaultStatusDto MapStatus(PasswordVaultSetting setting) => new(
        true,
        setting.Salt,
        setting.VerifierCipherText,
        setting.VerifierIv,
        setting.KdfIterations,
        setting.MasterWrappedKeyCipherText,
        setting.MasterWrappedKeyIv,
        setting.RecoverySalt,
        setting.RecoveryVerifierCipherText,
        setting.RecoveryVerifierIv,
        setting.RecoveryWrappedKeyCipherText,
        setting.RecoveryWrappedKeyIv,
        setting.RecoveryKdfIterations);
    private static PasswordEntryDto MapEntry(PasswordEntry e) => new(
        e.Id, e.GroupId, e.Group?.Name ?? "", e.Name, e.HasUsername, e.HasEmail,
        Field(e.UsernameCipherText, e.UsernameIv), Field(e.EmailCipherText, e.EmailIv), Field(e.PasswordCipherText, e.PasswordIv),
        e.CreatedDate, e.UpdatedDate, e.History.OrderByDescending(x => x.ChangeDate).Select(MapHistory).ToList());
    private static PasswordHistoryDto MapHistory(PasswordHistory h) => new(h.Id, h.ChangeDate, new EncryptedFieldDto(h.PreviousPasswordCipherText, h.PreviousPasswordIv));
    private static EncryptedFieldDto? Field(string? cipher, string? iv) => string.IsNullOrWhiteSpace(cipher) || string.IsNullOrWhiteSpace(iv) ? null : new EncryptedFieldDto(cipher, iv);
}
