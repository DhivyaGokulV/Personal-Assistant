using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Tasks.Daily;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations;

public class DailyTaskCompletionConfiguration : IEntityTypeConfiguration<DailyTaskCompletion>
{
    public void Configure(EntityTypeBuilder<DailyTaskCompletion> b)
    {
        b.ToTable("DailyTaskCompletions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Note).HasMaxLength(2000);
        b.HasIndex(x => new { x.DailyTaskId, x.Date }).IsUnique();
        b.HasIndex(x => new { x.OwnerUserId, x.Date });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
