namespace PersonalAssistant.Application.PasswordVault;

public record PasswordVaultStatusDto(
    bool IsInitialized,
    string? Salt,
    string? VerifierCipherText,
    string? VerifierIv,
    int? KdfIterations,
    string? MasterWrappedKeyCipherText = null,
    string? MasterWrappedKeyIv = null,
    string? RecoverySalt = null,
    string? RecoveryVerifierCipherText = null,
    string? RecoveryVerifierIv = null,
    string? RecoveryWrappedKeyCipherText = null,
    string? RecoveryWrappedKeyIv = null,
    int? RecoveryKdfIterations = null);

public record InitializeVaultRequest(
    string Salt,
    string VerifierCipherText,
    string VerifierIv,
    int KdfIterations,
    string? MasterWrappedKeyCipherText = null,
    string? MasterWrappedKeyIv = null,
    string? RecoverySalt = null,
    string? RecoveryVerifierCipherText = null,
    string? RecoveryVerifierIv = null,
    string? RecoveryWrappedKeyCipherText = null,
    string? RecoveryWrappedKeyIv = null,
    int? RecoveryKdfIterations = null);

public record ResetMasterPasswordRequest(
    string Salt,
    string VerifierCipherText,
    string VerifierIv,
    int KdfIterations,
    string MasterWrappedKeyCipherText,
    string MasterWrappedKeyIv);

public record EncryptedFieldDto(string CipherText, string Iv);

public record PasswordGroupDto(Guid Id, string Name, string? Description, int EntryCount);
public record PasswordGroupRequest(string Name, string? Description);

public record PasswordEntryDto(
    Guid Id, Guid GroupId, string GroupName, string Name, bool HasUsername, bool HasEmail,
    EncryptedFieldDto? Username, EncryptedFieldDto? Email, EncryptedFieldDto? Password,
    DateOnly CreatedDate, DateOnly? UpdatedDate, IReadOnlyList<PasswordHistoryDto> History);

public record PasswordEntryRequest(
    Guid GroupId, string Name, bool HasUsername, bool HasEmail,
    EncryptedFieldDto? Username, EncryptedFieldDto? Email, EncryptedFieldDto? Password,
    DateOnly CreatedDate, DateOnly? UpdatedDate);

public record PasswordHistoryDto(Guid Id, DateOnly ChangeDate, EncryptedFieldDto PreviousPassword);
public record PasswordHistoryRequest(DateOnly ChangeDate, EncryptedFieldDto PreviousPassword);

public interface IPasswordVaultService
{
    Task<PasswordVaultStatusDto> GetStatusAsync(CancellationToken ct);
    Task<PasswordVaultStatusDto> InitializeAsync(InitializeVaultRequest req, CancellationToken ct);
    Task<PasswordVaultStatusDto> ResetMasterPasswordAsync(ResetMasterPasswordRequest req, CancellationToken ct);
    Task<IReadOnlyList<PasswordGroupDto>> ListGroupsAsync(CancellationToken ct);
    Task<PasswordGroupDto> CreateGroupAsync(PasswordGroupRequest req, CancellationToken ct);
    Task<PasswordGroupDto> UpdateGroupAsync(Guid id, PasswordGroupRequest req, CancellationToken ct);
    Task DeleteGroupAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<PasswordEntryDto>> ListEntriesAsync(Guid? groupId, string? search, CancellationToken ct);
    Task<PasswordEntryDto> GetEntryAsync(Guid id, CancellationToken ct);
    Task<PasswordEntryDto> CreateEntryAsync(PasswordEntryRequest req, CancellationToken ct);
    Task<PasswordEntryDto> UpdateEntryAsync(Guid id, PasswordEntryRequest req, CancellationToken ct);
    Task DeleteEntryAsync(Guid id, CancellationToken ct);
    Task<PasswordHistoryDto> AddHistoryAsync(Guid entryId, PasswordHistoryRequest req, CancellationToken ct);
    Task DeleteHistoryAsync(Guid entryId, Guid historyId, CancellationToken ct);
}
