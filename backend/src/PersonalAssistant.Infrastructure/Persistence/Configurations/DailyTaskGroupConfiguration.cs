using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Tasks.Daily;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations;

public class DailyTaskGroupConfiguration : IEntityTypeConfiguration<DailyTaskGroup>
{
    public void Configure(EntityTypeBuilder<DailyTaskGroup> b)
    {
        b.ToTable("DailyTaskGroups");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.HasIndex(x => new { x.OwnerUserId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);

        b.HasMany(x => x.Tasks)
            .WithOne(x => x.Group)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
