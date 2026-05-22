using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalAssistant.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class RemainingModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FoodDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Carbohydrates = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Protein = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Fat = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Calories = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoalPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Tag = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    HeightCm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Bmi = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    BodyFatPercentage = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    MusclePercentage = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    BicepsCm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    BellyCm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    ForearmCm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    ChestCm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    ThighsCm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    CalvesCm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    NeckCm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NutritionEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeOfDay = table.Column<int>(type: "int", nullable: false),
                    Food = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Carbohydrates = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Protein = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Fat = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Calories = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NutritionGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Carbohydrates = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Protein = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Fat = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Calories = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionGoals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordVaultSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Salt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VerifierCipherText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    VerifierIv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    KdfIterations = table.Column<int>(type: "int", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordVaultSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activity = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Tag = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    TargetedMuscle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Tag = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    WorkoutName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TargetedMuscle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Tag = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    Intensity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Distance = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    CaloriesBurned = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoalPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Tag = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Deadline = table.Column<DateOnly>(type: "date", nullable: false),
                    AchievedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Goals_GoalPlans_GoalPlanId",
                        column: x => x.GoalPlanId,
                        principalTable: "GoalPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    HasUsername = table.Column<bool>(type: "bit", nullable: false),
                    HasEmail = table.Column<bool>(type: "bit", nullable: false),
                    UsernameCipherText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UsernameIv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmailCipherText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EmailIv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PasswordCipherText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PasswordIv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UpdatedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordEntries_PasswordGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "PasswordGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkoutEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SetNumber = table.Column<int>(type: "int", nullable: false),
                    Reps = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    AddedWeight = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutSets_WorkoutEntries_WorkoutEntryId",
                        column: x => x.WorkoutEntryId,
                        principalTable: "WorkoutEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoalSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Deadline = table.Column<DateOnly>(type: "date", nullable: false),
                    AchievedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalSteps_Goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "Goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PasswordEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PreviousPasswordCipherText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PreviousPasswordIv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordHistory_PasswordEntries_PasswordEntryId",
                        column: x => x.PasswordEntryId,
                        principalTable: "PasswordEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoodDefinitions_OwnerUserId_Name_IsDeleted",
                table: "FoodDefinitions",
                columns: new[] { "OwnerUserId", "Name", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_GoalPlans_OwnerUserId_IsDeleted",
                table: "GoalPlans",
                columns: new[] { "OwnerUserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Goals_GoalPlanId",
                table: "Goals",
                column: "GoalPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_OwnerUserId_GoalPlanId_IsDeleted",
                table: "Goals",
                columns: new[] { "OwnerUserId", "GoalPlanId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_GoalSteps_GoalId",
                table: "GoalSteps",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalSteps_OwnerUserId_GoalId_IsDeleted",
                table: "GoalSteps",
                columns: new[] { "OwnerUserId", "GoalId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementEntries_OwnerUserId_Date_IsDeleted",
                table: "MeasurementEntries",
                columns: new[] { "OwnerUserId", "Date", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_NutritionEntries_OwnerUserId_Date_IsDeleted",
                table: "NutritionEntries",
                columns: new[] { "OwnerUserId", "Date", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_NutritionGoals_OwnerUserId_IsDeleted",
                table: "NutritionGoals",
                columns: new[] { "OwnerUserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordEntries_GroupId",
                table: "PasswordEntries",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordEntries_OwnerUserId_GroupId_IsDeleted",
                table: "PasswordEntries",
                columns: new[] { "OwnerUserId", "GroupId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordGroups_OwnerUserId_IsDeleted",
                table: "PasswordGroups",
                columns: new[] { "OwnerUserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistory_OwnerUserId_PasswordEntryId_ChangeDate_IsDeleted",
                table: "PasswordHistory",
                columns: new[] { "OwnerUserId", "PasswordEntryId", "ChangeDate", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistory_PasswordEntryId",
                table: "PasswordHistory",
                column: "PasswordEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordVaultSettings_OwnerUserId_IsDeleted",
                table: "PasswordVaultSettings",
                columns: new[] { "OwnerUserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_OwnerUserId_StartTime_IsDeleted",
                table: "TimeEntries",
                columns: new[] { "OwnerUserId", "StartTime", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutDefinitions_OwnerUserId_Name_IsDeleted",
                table: "WorkoutDefinitions",
                columns: new[] { "OwnerUserId", "Name", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutEntries_OwnerUserId_Date_Type_IsDeleted",
                table: "WorkoutEntries",
                columns: new[] { "OwnerUserId", "Date", "Type", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSets_WorkoutEntryId_SetNumber",
                table: "WorkoutSets",
                columns: new[] { "WorkoutEntryId", "SetNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoodDefinitions");

            migrationBuilder.DropTable(
                name: "GoalSteps");

            migrationBuilder.DropTable(
                name: "MeasurementEntries");

            migrationBuilder.DropTable(
                name: "NutritionEntries");

            migrationBuilder.DropTable(
                name: "NutritionGoals");

            migrationBuilder.DropTable(
                name: "PasswordHistory");

            migrationBuilder.DropTable(
                name: "PasswordVaultSettings");

            migrationBuilder.DropTable(
                name: "TimeEntries");

            migrationBuilder.DropTable(
                name: "WorkoutDefinitions");

            migrationBuilder.DropTable(
                name: "WorkoutSets");

            migrationBuilder.DropTable(
                name: "Goals");

            migrationBuilder.DropTable(
                name: "PasswordEntries");

            migrationBuilder.DropTable(
                name: "WorkoutEntries");

            migrationBuilder.DropTable(
                name: "GoalPlans");

            migrationBuilder.DropTable(
                name: "PasswordGroups");
        }
    }
}
