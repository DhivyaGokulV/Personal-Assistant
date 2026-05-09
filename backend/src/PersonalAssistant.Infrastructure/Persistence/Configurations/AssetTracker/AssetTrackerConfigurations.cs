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
        b.Property(x => x.Status).HasConversion<int>();
        b.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.PriceHistory).WithOne(x => x.Investment).HasForeignKey(x => x.InvestmentId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Transactions).WithOne(x => x.Investment).HasForeignKey(x => x.InvestmentId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OwnerUserId, x.GroupId, x.Status, x.IsDeleted });
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
        b.Property(x => x.Note).HasMaxLength(2000);
        b.HasIndex(x => new { x.InvestmentId, x.Date });
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
