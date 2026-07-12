using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PersonalAssistant.Infrastructure.Persistence;

#nullable disable

namespace PersonalAssistant.Migrations.Sqlite.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260712073253_LiabilityAccountsBatch")]
    /// <inheritdoc />
    public partial class LiabilityAccountsBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LiabilityAccountAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LiabilityAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    OldValuesJson = table.Column<string>(type: "TEXT", maxLength: 12000, nullable: true),
                    NewValuesJson = table.Column<string>(type: "TEXT", maxLength: 12000, nullable: true),
                    ChangedFieldsJson = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiabilityAccountAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LiabilityAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreationDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiabilityAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LiabilityAccountEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LiabilityAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiabilityAccountEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiabilityAccountEntries_LiabilityAccounts_LiabilityAccountId",
                        column: x => x.LiabilityAccountId,
                        principalTable: "LiabilityAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LiabilityAccountStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LiabilityAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiabilityAccountStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiabilityAccountStatusHistory_LiabilityAccounts_LiabilityAccountId",
                        column: x => x.LiabilityAccountId,
                        principalTable: "LiabilityAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LiabilityAccountAuditEntries_OwnerUserId_LiabilityAccountId_CreatedAt",
                table: "LiabilityAccountAuditEntries",
                columns: new[] { "OwnerUserId", "LiabilityAccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LiabilityAccountEntries_LiabilityAccountId_Date",
                table: "LiabilityAccountEntries",
                columns: new[] { "LiabilityAccountId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_LiabilityAccountEntries_OwnerUserId_LiabilityAccountId_IsDeleted",
                table: "LiabilityAccountEntries",
                columns: new[] { "OwnerUserId", "LiabilityAccountId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_LiabilityAccounts_OwnerUserId_Category_Name_IsDeleted",
                table: "LiabilityAccounts",
                columns: new[] { "OwnerUserId", "Category", "Name", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_LiabilityAccounts_OwnerUserId_Category_Status_IsDeleted",
                table: "LiabilityAccounts",
                columns: new[] { "OwnerUserId", "Category", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_LiabilityAccountStatusHistory_LiabilityAccountId_EffectiveDate_IsDeleted",
                table: "LiabilityAccountStatusHistory",
                columns: new[] { "LiabilityAccountId", "EffectiveDate", "IsDeleted" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiabilityAccountAuditEntries");

            migrationBuilder.DropTable(
                name: "LiabilityAccountEntries");

            migrationBuilder.DropTable(
                name: "LiabilityAccountStatusHistory");

            migrationBuilder.DropTable(
                name: "LiabilityAccounts");
        }
    }
}

