using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Finance;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations.Finance;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> b)
    {
        b.ToTable("Transactions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Reason).IsRequired().HasMaxLength(300);
        b.Property(x => x.Note).HasMaxLength(2000);

        b.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(x => x.PaymentType)
            .WithMany()
            .HasForeignKey(x => x.PaymentTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => new { x.OwnerUserId, x.Date, x.IsDeleted });
        b.HasIndex(x => new { x.OwnerUserId, x.AccountId, x.Date });
        b.HasIndex(x => x.TransferGroupId);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class TransactionTagConfiguration : IEntityTypeConfiguration<TransactionTag>
{
    public void Configure(EntityTypeBuilder<TransactionTag> b)
    {
        b.ToTable("TransactionTags");
        b.HasKey(x => new { x.TransactionId, x.TagId });

        b.HasOne(x => x.Transaction)
            .WithMany(t => t.TransactionTags)
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Tag)
            .WithMany()
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.TagId);
    }
}
