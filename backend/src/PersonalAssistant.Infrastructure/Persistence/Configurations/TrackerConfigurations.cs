using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalAssistant.Domain.Goals;
using PersonalAssistant.Domain.Health;
using PersonalAssistant.Domain.PasswordVault;
using PersonalAssistant.Domain.TimeTracker;

namespace PersonalAssistant.Infrastructure.Persistence.Configurations;

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> b)
    {
        b.ToTable("TimeEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Activity).IsRequired().HasMaxLength(300);
        b.Property(x => x.Note).HasMaxLength(2000);
        b.Property(x => x.Tag).HasMaxLength(120);
        b.HasIndex(x => new { x.OwnerUserId, x.StartTime, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class MeasurementEntryConfiguration : IEntityTypeConfiguration<MeasurementEntry>
{
    public void Configure(EntityTypeBuilder<MeasurementEntry> b)
    {
        b.ToTable("MeasurementEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Note).HasMaxLength(2000);
        ConfigureDecimalMeasurements(b);
        b.HasIndex(x => new { x.OwnerUserId, x.Date, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }

    private static void ConfigureDecimalMeasurements(EntityTypeBuilder<MeasurementEntry> b)
    {
        b.Property(x => x.HeightCm).HasPrecision(10, 2);
        b.Property(x => x.WeightKg).HasPrecision(10, 2);
        b.Property(x => x.Bmi).HasPrecision(10, 2);
        b.Property(x => x.BodyFatPercentage).HasPrecision(10, 2);
        b.Property(x => x.MusclePercentage).HasPrecision(10, 2);
        b.Property(x => x.BicepsCm).HasPrecision(10, 2);
        b.Property(x => x.BellyCm).HasPrecision(10, 2);
        b.Property(x => x.ForearmCm).HasPrecision(10, 2);
        b.Property(x => x.ChestCm).HasPrecision(10, 2);
        b.Property(x => x.ThighsCm).HasPrecision(10, 2);
        b.Property(x => x.CalvesCm).HasPrecision(10, 2);
        b.Property(x => x.NeckCm).HasPrecision(10, 2);
    }
}

public class WorkoutDefinitionConfiguration : IEntityTypeConfiguration<WorkoutDefinition>
{
    public void Configure(EntityTypeBuilder<WorkoutDefinition> b)
    {
        b.ToTable("WorkoutDefinitions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(300);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.TargetedMuscle).HasMaxLength(200);
        b.Property(x => x.Tag).HasMaxLength(120);
        b.HasIndex(x => new { x.OwnerUserId, x.Name, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class WorkoutEntryConfiguration : IEntityTypeConfiguration<WorkoutEntry>
{
    public void Configure(EntityTypeBuilder<WorkoutEntry> b)
    {
        b.ToTable("WorkoutEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.WorkoutName).IsRequired().HasMaxLength(300);
        b.Property(x => x.TargetedMuscle).HasMaxLength(200);
        b.Property(x => x.Tag).HasMaxLength(120);
        b.Property(x => x.Intensity).HasMaxLength(100);
        b.Property(x => x.Distance).HasPrecision(12, 2);
        b.Property(x => x.CaloriesBurned).HasPrecision(12, 2);
        b.Property(x => x.Note).HasMaxLength(2000);
        b.HasMany(x => x.Sets).WithOne(x => x.WorkoutEntry).HasForeignKey(x => x.WorkoutEntryId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OwnerUserId, x.Date, x.Type, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class WorkoutSetConfiguration : IEntityTypeConfiguration<WorkoutSet>
{
    public void Configure(EntityTypeBuilder<WorkoutSet> b)
    {
        b.ToTable("WorkoutSets");
        b.HasKey(x => x.Id);
        b.Property(x => x.Weight).HasPrecision(12, 2);
        b.Property(x => x.AddedWeight).HasPrecision(12, 2);
        b.HasIndex(x => new { x.WorkoutEntryId, x.SetNumber });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class FoodDefinitionConfiguration : IEntityTypeConfiguration<FoodDefinition>
{
    public void Configure(EntityTypeBuilder<FoodDefinition> b)
    {
        b.ToTable("FoodDefinitions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(300);
        b.Property(x => x.Unit).IsRequired().HasMaxLength(60);
        b.Property(x => x.Carbohydrates).HasPrecision(12, 2);
        b.Property(x => x.Protein).HasPrecision(12, 2);
        b.Property(x => x.Fat).HasPrecision(12, 2);
        b.Property(x => x.Calories).HasPrecision(12, 2);
        b.HasIndex(x => new { x.OwnerUserId, x.Name, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class NutritionEntryConfiguration : IEntityTypeConfiguration<NutritionEntry>
{
    public void Configure(EntityTypeBuilder<NutritionEntry> b)
    {
        b.ToTable("NutritionEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.TimeOfDay).HasConversion<int>();
        b.Property(x => x.Food).IsRequired().HasMaxLength(300);
        b.Property(x => x.Unit).IsRequired().HasMaxLength(60);
        b.Property(x => x.Quantity).HasPrecision(12, 2);
        b.Property(x => x.Carbohydrates).HasPrecision(12, 2);
        b.Property(x => x.Protein).HasPrecision(12, 2);
        b.Property(x => x.Fat).HasPrecision(12, 2);
        b.Property(x => x.Calories).HasPrecision(12, 2);
        b.Property(x => x.Note).HasMaxLength(2000);
        b.HasIndex(x => new { x.OwnerUserId, x.Date, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class NutritionGoalConfiguration : IEntityTypeConfiguration<NutritionGoal>
{
    public void Configure(EntityTypeBuilder<NutritionGoal> b)
    {
        b.ToTable("NutritionGoals");
        b.HasKey(x => x.Id);
        b.Property(x => x.Carbohydrates).HasPrecision(12, 2);
        b.Property(x => x.Protein).HasPrecision(12, 2);
        b.Property(x => x.Fat).HasPrecision(12, 2);
        b.Property(x => x.Calories).HasPrecision(12, 2);
        b.HasIndex(x => new { x.OwnerUserId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class GoalPlanConfiguration : IEntityTypeConfiguration<GoalPlan>
{
    public void Configure(EntityTypeBuilder<GoalPlan> b)
    {
        b.ToTable("GoalPlans");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Tag).HasMaxLength(120);
        b.HasMany(x => x.Goals).WithOne(x => x.GoalPlan).HasForeignKey(x => x.GoalPlanId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OwnerUserId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> b)
    {
        b.ToTable("Goals");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Tag).HasMaxLength(120);
        b.Property(x => x.Note).HasMaxLength(2000);
        b.HasMany(x => x.Steps).WithOne(x => x.Goal).HasForeignKey(x => x.GoalId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OwnerUserId, x.GoalPlanId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class GoalStepConfiguration : IEntityTypeConfiguration<GoalStep>
{
    public void Configure(EntityTypeBuilder<GoalStep> b)
    {
        b.ToTable("GoalSteps");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Note).HasMaxLength(2000);
        b.HasIndex(x => new { x.OwnerUserId, x.GoalId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PasswordVaultSettingConfiguration : IEntityTypeConfiguration<PasswordVaultSetting>
{
    public void Configure(EntityTypeBuilder<PasswordVaultSetting> b)
    {
        b.ToTable("PasswordVaultSettings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Salt).IsRequired().HasMaxLength(500);
        b.Property(x => x.VerifierCipherText).IsRequired().HasMaxLength(4000);
        b.Property(x => x.VerifierIv).IsRequired().HasMaxLength(500);
        b.HasIndex(x => new { x.OwnerUserId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PasswordGroupConfiguration : IEntityTypeConfiguration<PasswordGroup>
{
    public void Configure(EntityTypeBuilder<PasswordGroup> b)
    {
        b.ToTable("PasswordGroups");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.HasMany(x => x.Entries).WithOne(x => x.Group).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OwnerUserId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PasswordEntryConfiguration : IEntityTypeConfiguration<PasswordEntry>
{
    public void Configure(EntityTypeBuilder<PasswordEntry> b)
    {
        b.ToTable("PasswordEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(300);
        b.Property(x => x.UsernameCipherText).HasMaxLength(4000);
        b.Property(x => x.UsernameIv).HasMaxLength(500);
        b.Property(x => x.EmailCipherText).HasMaxLength(4000);
        b.Property(x => x.EmailIv).HasMaxLength(500);
        b.Property(x => x.PasswordCipherText).HasMaxLength(4000);
        b.Property(x => x.PasswordIv).HasMaxLength(500);
        b.HasMany(x => x.History).WithOne(x => x.PasswordEntry).HasForeignKey(x => x.PasswordEntryId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OwnerUserId, x.GroupId, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> b)
    {
        b.ToTable("PasswordHistory");
        b.HasKey(x => x.Id);
        b.Property(x => x.PreviousPasswordCipherText).IsRequired().HasMaxLength(4000);
        b.Property(x => x.PreviousPasswordIv).IsRequired().HasMaxLength(500);
        b.HasIndex(x => new { x.OwnerUserId, x.PasswordEntryId, x.ChangeDate, x.IsDeleted });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
