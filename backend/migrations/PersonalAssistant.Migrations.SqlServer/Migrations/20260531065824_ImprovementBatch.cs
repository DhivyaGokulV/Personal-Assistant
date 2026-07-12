using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalAssistant.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class ImprovementBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "PeriodicTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "PeriodicTaskGroups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MasterWrappedKeyCipherText",
                table: "PasswordVaultSettings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MasterWrappedKeyIv",
                table: "PasswordVaultSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecoveryKdfIterations",
                table: "PasswordVaultSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecoverySalt",
                table: "PasswordVaultSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryVerifierCipherText",
                table: "PasswordVaultSettings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryVerifierIv",
                table: "PasswordVaultSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryWrappedKeyCipherText",
                table: "PasswordVaultSettings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryWrappedKeyIv",
                table: "PasswordVaultSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "DailyTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "DailyTaskGroups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BudgetEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BudgetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetEntries_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetEntries_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskArchiveEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskArchiveEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WaterIntakeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Time = table.Column<TimeOnly>(type: "time", nullable: false),
                    QuantityMl = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaterIntakeEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetEntries_BudgetId",
                table: "BudgetEntries",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetEntries_CategoryId",
                table: "BudgetEntries",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetEntries_OwnerUserId_BudgetId_CategoryId_IsDeleted",
                table: "BudgetEntries",
                columns: new[] { "OwnerUserId", "BudgetId", "CategoryId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskArchiveEntries_OwnerUserId_Module_ActionDate_IsDeleted",
                table: "TaskArchiveEntries",
                columns: new[] { "OwnerUserId", "Module", "ActionDate", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_WaterIntakeEntries_OwnerUserId_Date_IsDeleted",
                table: "WaterIntakeEntries",
                columns: new[] { "OwnerUserId", "Date", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetEntries");

            migrationBuilder.DropTable(
                name: "TaskArchiveEntries");

            migrationBuilder.DropTable(
                name: "WaterIntakeEntries");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "PeriodicTasks");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "PeriodicTaskGroups");

            migrationBuilder.DropColumn(
                name: "MasterWrappedKeyCipherText",
                table: "PasswordVaultSettings");

            migrationBuilder.DropColumn(
                name: "MasterWrappedKeyIv",
                table: "PasswordVaultSettings");

            migrationBuilder.DropColumn(
                name: "RecoveryKdfIterations",
                table: "PasswordVaultSettings");

            migrationBuilder.DropColumn(
                name: "RecoverySalt",
                table: "PasswordVaultSettings");

            migrationBuilder.DropColumn(
                name: "RecoveryVerifierCipherText",
                table: "PasswordVaultSettings");

            migrationBuilder.DropColumn(
                name: "RecoveryVerifierIv",
                table: "PasswordVaultSettings");

            migrationBuilder.DropColumn(
                name: "RecoveryWrappedKeyCipherText",
                table: "PasswordVaultSettings");

            migrationBuilder.DropColumn(
                name: "RecoveryWrappedKeyIv",
                table: "PasswordVaultSettings");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "DailyTasks");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "DailyTaskGroups");
        }
    }
}
