using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Tasks.Todo;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations;

public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> b)
    {
        b.ToTable("Todos");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.CompletionNote).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<int>();
        b.HasIndex(x => new { x.OwnerUserId, x.Status, x.IsDeleted });
        b.HasIndex(x => new { x.OwnerUserId, x.Deadline });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
