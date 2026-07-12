using PersonalAssistant.Domain.Common;

namespace PersonalAssistant.Domain.PasswordVault;

public class PasswordVaultSetting : EntityBase
{
    public string Salt { get; set; } = string.Empty;
    public string VerifierCipherText { get; set; } = string.Empty;
    public string VerifierIv { get; set; } = string.Empty;
    public int KdfIterations { get; set; }
    public string? MasterWrappedKeyCipherText { get; set; }
    public string? MasterWrappedKeyIv { get; set; }
    public string? RecoverySalt { get; set; }
    public string? RecoveryVerifierCipherText { get; set; }
    public string? RecoveryVerifierIv { get; set; }
    public string? RecoveryWrappedKeyCipherText { get; set; }
    public string? RecoveryWrappedKeyIv { get; set; }
    public int? RecoveryKdfIterations { get; set; }
}

public class PasswordGroup : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<PasswordEntry> Entries { get; set; } = new List<PasswordEntry>();
}

public class PasswordEntry : EntityBase
{
    public Guid GroupId { get; set; }
    public PasswordGroup? Group { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool HasUsername { get; set; }
    public bool HasEmail { get; set; }
    public string? UsernameCipherText { get; set; }
    public string? UsernameIv { get; set; }
    public string? EmailCipherText { get; set; }
    public string? EmailIv { get; set; }
    public string? PasswordCipherText { get; set; }
    public string? PasswordIv { get; set; }
    public DateOnly CreatedDate { get; set; }
    public DateOnly? UpdatedDate { get; set; }
    public ICollection<PasswordHistory> History { get; set; } = new List<PasswordHistory>();
}

public class PasswordHistory : EntityBase
{
    public Guid PasswordEntryId { get; set; }
    public PasswordEntry? PasswordEntry { get; set; }
    public DateOnly ChangeDate { get; set; }
    public string PreviousPasswordCipherText { get; set; } = string.Empty;
    public string PreviousPasswordIv { get; set; } = string.Empty;
}
