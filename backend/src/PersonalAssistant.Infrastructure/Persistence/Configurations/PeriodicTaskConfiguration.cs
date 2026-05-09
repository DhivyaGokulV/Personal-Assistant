using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Tasks.Periodic;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations;

public class PeriodicTaskConfiguration : IEntityTypeConfiguration<PeriodicTask>
{
    public void Configure(EntityTypeBuilder<PeriodicTask> b)
    {
        b.ToTable("PeriodicTasks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.FrequencyUnit).HasConversion<int>();
        b.HasIndex(x => new { x.OwnerUserId, x.GroupId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);

        b.HasMany(x => x.History)
            .WithOne(x => x.PeriodicTask)
            .HasForeignKey(x => x.PeriodicTaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
