using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Finance;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations.Finance;

public class PaymentTypeConfiguration : IEntityTypeConfiguration<PaymentType>
{
    public void Configure(EntityTypeBuilder<PaymentType> b)
    {
        b.ToTable("PaymentTypes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.HasIndex(x => new { x.OwnerUserId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
