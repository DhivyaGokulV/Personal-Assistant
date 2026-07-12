using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PersonalAssistant.Infrastructure.Persistence;

#nullable disable

namespace PersonalAssistant.Migrations.SqlServer.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260712073408_LiabilityAccountsBatch")]
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LiabilityAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", maxLength: 12000, nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", maxLength: 12000, nullable: true),
                    ChangedFieldsJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiabilityAccountAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LiabilityAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiabilityAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LiabilityAccountEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LiabilityAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LiabilityAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
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

