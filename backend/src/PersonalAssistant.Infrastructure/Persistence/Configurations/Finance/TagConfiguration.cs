using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Finance;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations.Finance;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> b)
    {
        b.ToTable("Tags");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Color).IsRequired().HasMaxLength(20);
        b.HasIndex(x => new { x.OwnerUserId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
