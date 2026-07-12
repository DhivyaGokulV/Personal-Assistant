using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Tasks;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations;

public class TaskArchiveEntryConfiguration : IEntityTypeConfiguration<TaskArchiveEntry>
{
    public void Configure(EntityTypeBuilder<TaskArchiveEntry> b)
    {
        b.ToTable("TaskArchiveEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Module).IsRequired().HasMaxLength(40);
        b.Property(x => x.EntityType).IsRequired().HasMaxLength(80);
        b.Property(x => x.ActivityType).IsRequired().HasMaxLength(40);
        b.Property(x => x.OldValue).HasMaxLength(8000);
        b.Property(x => x.NewValue).HasMaxLength(8000);
        b.HasIndex(x => new { x.OwnerUserId, x.Module, x.ActionDate, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
