using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Tasks.Daily;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations;

public class DailyTaskConfiguration : IEntityTypeConfiguration<DailyTask>
{
    public void Configure(EntityTypeBuilder<DailyTask> b)
    {
        b.ToTable("DailyTasks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.DisplayOrder).HasDefaultValue(0);
        b.HasIndex(x => new { x.OwnerUserId, x.GroupId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);

        b.HasMany(x => x.Completions)
            .WithOne(x => x.DailyTask)
            .HasForeignKey(x => x.DailyTaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
