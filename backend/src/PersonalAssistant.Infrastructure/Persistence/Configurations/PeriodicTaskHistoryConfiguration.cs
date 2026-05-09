using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Tasks.Periodic;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations;

public class PeriodicTaskHistoryConfiguration : IEntityTypeConfiguration<PeriodicTaskHistory>
{
    public void Configure(EntityTypeBuilder<PeriodicTaskHistory> b)
    {
        b.ToTable("PeriodicTaskHistory");
        b.HasKey(x => x.Id);
        b.Property(x => x.Note).HasMaxLength(2000);
        b.HasIndex(x => new { x.PeriodicTaskId, x.CompletedOn });
        b.HasIndex(x => new { x.OwnerUserId, x.CompletedOn });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
