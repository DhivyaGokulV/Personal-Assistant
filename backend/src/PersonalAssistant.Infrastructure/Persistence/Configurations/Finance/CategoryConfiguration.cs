using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Finance;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations.Finance;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Type).HasConversion<int>();
        b.HasIndex(x => new { x.OwnerUserId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
