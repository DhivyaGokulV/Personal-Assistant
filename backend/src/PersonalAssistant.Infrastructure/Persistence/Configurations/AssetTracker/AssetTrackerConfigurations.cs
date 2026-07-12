using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.AssetTracker;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations.AssetTracker;

public class AssetTagConfiguration : IEntityTypeConfiguration<AssetTag>
{
    public void Configure(EntityTypeBuilder<AssetTag> b)
    {
        b.ToTable("AssetTags");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Color).IsRequired().HasMaxLength(20);
        b.HasIndex(x => new { x.OwnerUserId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class AssetGroupConfiguration : IEntityTypeConfiguration<AssetGroup>
{
    public void Configure(EntityTypeBuilder<AssetGroup> b)
    {
        b.ToTable("AssetGroups");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.Assets).WithOne(x => x.Group).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OwnerUserId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> b)
    {
        b.ToTable("Assets");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.BuyingPrice).HasPrecision(18, 2);
        b.Property(x => x.SellingPrice).HasPrecision(18, 2);
        b.Property(x => x.Status).HasConversion<int>();
        b.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.PriceHistory).WithOne(x => x.Asset).HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OwnerUserId, x.GroupId, x.Status, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class AssetPriceHistoryConfiguration : IEntityTypeConfiguration<AssetPriceHistory>
{
    public void Configure(EntityTypeBuilder<AssetPriceHistory> b)
    {
        b.ToTable("AssetPriceHistory");
        b.HasKey(x => x.Id);
        b.Property(x => x.Price).HasPrecision(18, 2);
        b.Property(x => x.Note).HasMaxLength(2000);
        b.HasIndex(x => new { x.AssetId, x.AsOf });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class InvestmentGroupConfiguration : IEntityTypeConfiguration<InvestmentGroup>
{
    public void Configure(EntityTypeBuilder<InvestmentGroup> b)
    {
        b.ToTable("InvestmentGroups");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<int>();
        b.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.Investments).WithOne(x => x.Group).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OwnerUserId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class InvestmentConfiguration : IEntityTypeConfiguration<Investment>
{
    public void Configure(EntityTypeBuilder<Investment> b)
    {
        b.ToTable("Investments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Unit).IsRequired().HasMaxLength(50);
        b.Property(x => x.InvestmentType).HasConversion<int>();
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.Property(x => x.Status).HasConversion<int>();
        b.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.PriceHistory).WithOne(x => x.Investment).HasForeignKey(x => x.InvestmentId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Transactions).WithOne(x => x.Investment).HasForeignKey(x => x.InvestmentId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.StatusHistory).WithOne(x => x.Investment).HasForeignKey(x => x.InvestmentId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OwnerUserId, x.GroupId, x.Status, x.IsDeleted });
        b.HasIndex(x => new { x.OwnerUserId, x.InvestmentType, x.CurrencyCode, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class InvestmentPriceHistoryConfiguration : IEntityTypeConfiguration<InvestmentPriceHistory>
{
    public void Configure(EntityTypeBuilder<InvestmentPriceHistory> b)
    {
        b.ToTable("InvestmentPriceHistory");
        b.HasKey(x => x.Id);
        b.Property(x => x.Price).HasPrecision(18, 4);
        // Retain the legacy database width; the investment API enforces the new
        // 200-character input limit without risking truncation during upgrades.
        b.Property(x => x.Note).HasMaxLength(2000);
        b.HasIndex(x => new { x.InvestmentId, x.AsOf });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class InvestmentTransactionConfiguration : IEntityTypeConfiguration<InvestmentTransaction>
{
    public void Configure(EntityTypeBuilder<InvestmentTransaction> b)
    {
        b.ToTable("InvestmentTransactions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Units).HasPrecision(18, 4);
        b.Property(x => x.Price).HasPrecision(18, 4);
        b.Property(x => x.Amount).HasPrecision(18, 4);
        b.Property(x => x.Note).HasMaxLength(2000);
        b.HasIndex(x => new { x.InvestmentId, x.Date });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class InvestmentStatusHistoryConfiguration : IEntityTypeConfiguration<InvestmentStatusHistory>
{
    public void Configure(EntityTypeBuilder<InvestmentStatusHistory> b)
    {
        b.ToTable("InvestmentStatusHistory");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<int>();
        b.HasIndex(x => new { x.InvestmentId, x.EffectiveDate, x.IsDeleted }).IsUnique();
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class InvestmentAuditEntryConfiguration : IEntityTypeConfiguration<InvestmentAuditEntry>
{
    public void Configure(EntityTypeBuilder<InvestmentAuditEntry> b)
    {
        b.ToTable("InvestmentAuditEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.EntityType).IsRequired().HasMaxLength(50);
        b.Property(x => x.Action).HasConversion<int>();
        b.Property(x => x.OldValuesJson).HasMaxLength(12000);
        b.Property(x => x.NewValuesJson).HasMaxLength(12000);
        b.Property(x => x.ChangedFieldsJson).HasMaxLength(1000);
        b.HasIndex(x => new { x.OwnerUserId, x.InvestmentId, x.CreatedAt });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PreciousMetalConfiguration : IEntityTypeConfiguration<PreciousMetal>
{
    public void Configure(EntityTypeBuilder<PreciousMetal> b)
    {
        b.ToTable("PreciousMetals");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(30);
        b.Property(x => x.Description).HasMaxLength(200);
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.HasMany(x => x.Transactions).WithOne(x => x.PreciousMetal).HasForeignKey(x => x.PreciousMetalId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.PriceHistory).WithOne(x => x.PreciousMetal).HasForeignKey(x => x.PreciousMetalId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OwnerUserId, x.Name, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PreciousMetalTransactionConfiguration : IEntityTypeConfiguration<PreciousMetalTransaction>
{
    public void Configure(EntityTypeBuilder<PreciousMetalTransaction> b)
    {
        b.ToTable("PreciousMetalTransactions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Note).HasMaxLength(200);
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.PricePerUnit).HasPrecision(18, 4);
        b.HasIndex(x => new { x.PreciousMetalId, x.Date });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PreciousMetalPriceHistoryConfiguration : IEntityTypeConfiguration<PreciousMetalPriceHistory>
{
    public void Configure(EntityTypeBuilder<PreciousMetalPriceHistory> b)
    {
        b.ToTable("PreciousMetalPriceHistory");
        b.HasKey(x => x.Id);
        b.Property(x => x.PricePerUnit).HasPrecision(18, 4);
        b.HasIndex(x => new { x.PreciousMetalId, x.AsOf, x.IsDeleted }).IsUnique();
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PreciousMetalAuditEntryConfiguration : IEntityTypeConfiguration<PreciousMetalAuditEntry>
{
    public void Configure(EntityTypeBuilder<PreciousMetalAuditEntry> b)
    {
        b.ToTable("PreciousMetalAuditEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.EntityType).IsRequired().HasMaxLength(50);
        b.Property(x => x.Action).HasConversion<int>();
        b.Property(x => x.OldValuesJson).HasMaxLength(12000);
        b.Property(x => x.NewValuesJson).HasMaxLength(12000);
        b.Property(x => x.ChangedFieldsJson).HasMaxLength(1000);
        b.HasIndex(x => new { x.OwnerUserId, x.PreciousMetalId, x.CreatedAt });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class JewelleryItemConfiguration : IEntityTypeConfiguration<JewelleryItem>
{
    public void Configure(EntityTypeBuilder<JewelleryItem> b)
    {
        b.ToTable("JewelleryItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(30);
        b.Property(x => x.Description).HasMaxLength(200);
        b.Property(x => x.BuyingPrice).HasPrecision(18, 4);
        b.Property(x => x.QuantityInGrams).HasPrecision(18, 4);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.SellingPrice).HasPrecision(18, 4);
        b.Property(x => x.SellingNote).HasMaxLength(200);
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.HasIndex(x => new { x.OwnerUserId, x.Status, x.BuyingDate, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class JewelleryAuditEntryConfiguration : IEntityTypeConfiguration<JewelleryAuditEntry>
{
    public void Configure(EntityTypeBuilder<JewelleryAuditEntry> b)
    {
        b.ToTable("JewelleryAuditEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).HasConversion<int>();
        b.Property(x => x.OldValuesJson).HasMaxLength(12000);
        b.Property(x => x.NewValuesJson).HasMaxLength(12000);
        b.Property(x => x.ChangedFieldsJson).HasMaxLength(1000);
        b.HasIndex(x => new { x.OwnerUserId, x.JewelleryItemId, x.CreatedAt });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PersonalAssetItemConfiguration : IEntityTypeConfiguration<PersonalAssetItem>
{
    public void Configure(EntityTypeBuilder<PersonalAssetItem> b)
    {
        b.ToTable("PersonalAssetItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(30);
        b.Property(x => x.Description).HasMaxLength(200);
        b.Property(x => x.BuyingPrice).HasPrecision(18, 4);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.SellingPrice).HasPrecision(18, 4);
        b.Property(x => x.SellingNote).HasMaxLength(200);
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.HasIndex(x => new { x.OwnerUserId, x.Status, x.BuyingDate, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PersonalAssetAuditEntryConfiguration : IEntityTypeConfiguration<PersonalAssetAuditEntry>
{
    public void Configure(EntityTypeBuilder<PersonalAssetAuditEntry> b)
    {
        b.ToTable("PersonalAssetAuditEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).HasConversion<int>();
        b.Property(x => x.OldValuesJson).HasMaxLength(12000);
        b.Property(x => x.NewValuesJson).HasMaxLength(12000);
        b.Property(x => x.ChangedFieldsJson).HasMaxLength(1000);
        b.HasIndex(x => new { x.OwnerUserId, x.PersonalAssetItemId, x.CreatedAt });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class LiabilityAccountConfiguration : IEntityTypeConfiguration<LiabilityAccount>
{
    public void Configure(EntityTypeBuilder<LiabilityAccount> b)
    {
        b.ToTable("LiabilityAccounts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Category).HasConversion<int>();
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.HasMany(x => x.Entries).WithOne(x => x.LiabilityAccount).HasForeignKey(x => x.LiabilityAccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.StatusHistory).WithOne(x => x.LiabilityAccount).HasForeignKey(x => x.LiabilityAccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OwnerUserId, x.Category, x.Status, x.IsDeleted });
        b.HasIndex(x => new { x.OwnerUserId, x.Category, x.Name, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class LiabilityAccountEntryConfiguration : IEntityTypeConfiguration<LiabilityAccountEntry>
{
    public void Configure(EntityTypeBuilder<LiabilityAccountEntry> b)
    {
        b.ToTable("LiabilityAccountEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Note).HasMaxLength(200);
        b.Property(x => x.Amount).HasPrecision(18, 4);
        b.HasIndex(x => new { x.LiabilityAccountId, x.Date });
        b.HasIndex(x => new { x.OwnerUserId, x.LiabilityAccountId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class LiabilityAccountStatusHistoryConfiguration : IEntityTypeConfiguration<LiabilityAccountStatusHistory>
{
    public void Configure(EntityTypeBuilder<LiabilityAccountStatusHistory> b)
    {
        b.ToTable("LiabilityAccountStatusHistory");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<int>();
        b.HasIndex(x => new { x.LiabilityAccountId, x.EffectiveDate, x.IsDeleted }).IsUnique();
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class LiabilityAccountAuditEntryConfiguration : IEntityTypeConfiguration<LiabilityAccountAuditEntry>
{
    public void Configure(EntityTypeBuilder<LiabilityAccountAuditEntry> b)
    {
        b.ToTable("LiabilityAccountAuditEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.EntityType).IsRequired().HasMaxLength(50);
        b.Property(x => x.Action).HasConversion<int>();
        b.Property(x => x.OldValuesJson).HasMaxLength(12000);
        b.Property(x => x.NewValuesJson).HasMaxLength(12000);
        b.Property(x => x.ChangedFieldsJson).HasMaxLength(1000);
        b.HasIndex(x => new { x.OwnerUserId, x.LiabilityAccountId, x.CreatedAt });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class LiabilityConfiguration : IEntityTypeConfiguration<Liability>
{
    public void Configure(EntityTypeBuilder<Liability> b)
    {
        b.ToTable("Liabilities");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<int>();
        b.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.History).WithOne(x => x.Liability).HasForeignKey(x => x.LiabilityId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OwnerUserId, x.Status, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class LiabilityHistoryConfiguration : IEntityTypeConfiguration<LiabilityHistory>
{
    public void Configure(EntityTypeBuilder<LiabilityHistory> b)
    {
        b.ToTable("LiabilityHistory");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Note).HasMaxLength(2000);
        b.HasIndex(x => new { x.LiabilityId, x.Date });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
