using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PersonalAssistant.Infrastructure.Persistence;

#nullable disable

namespace PersonalAssistant.Migrations.Sqlite.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260706181937_AssetsLiabilitiesInvestments")]
    public partial class AssetsLiabilitiesInvestments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "InvestmentTransactions",
                type: "TEXT",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CreationDate",
                table: "Investments",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1970, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Investments",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "INR");

            migrationBuilder.AddColumn<int>(
                name: "InvestmentType",
                table: "Investments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "InvestmentAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvestmentId = table.Column<Guid>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_InvestmentAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvestmentStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvestmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvestmentStatusHistory_Investments_InvestmentId",
                        column: x => x.InvestmentId,
                        principalTable: "Investments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                UPDATE Investments
                SET CreationDate = date(CreatedAt), CurrencyCode = 'INR', InvestmentType = 1;

                INSERT INTO InvestmentStatusHistory
                    (Id, InvestmentId, Status, EffectiveDate, OwnerUserId, CreatedAt, UpdatedAt, IsDeleted)
                SELECT lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' ||
                       substr(lower(hex(randomblob(2))), 2) || '-' ||
                       substr('89ab', abs(random()) % 4 + 1, 1) ||
                       substr(lower(hex(randomblob(2))), 2) || '-' || lower(hex(randomblob(6))),
                       Id, Status, date(CreatedAt), OwnerUserId, CreatedAt, UpdatedAt, 0
                FROM Investments
                WHERE IsDeleted = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Investments_OwnerUserId_InvestmentType_CurrencyCode_IsDeleted",
                table: "Investments",
                columns: new[] { "OwnerUserId", "InvestmentType", "CurrencyCode", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentAuditEntries_OwnerUserId_InvestmentId_CreatedAt",
                table: "InvestmentAuditEntries",
                columns: new[] { "OwnerUserId", "InvestmentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentStatusHistory_InvestmentId_EffectiveDate_IsDeleted",
                table: "InvestmentStatusHistory",
                columns: new[] { "InvestmentId", "EffectiveDate", "IsDeleted" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvestmentAuditEntries");

            migrationBuilder.DropTable(
                name: "InvestmentStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_Investments_OwnerUserId_InvestmentType_CurrencyCode_IsDeleted",
                table: "Investments");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "InvestmentTransactions");

            migrationBuilder.DropColumn(
                name: "CreationDate",
                table: "Investments");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Investments");

            migrationBuilder.DropColumn(
                name: "InvestmentType",
                table: "Investments");
        }
    }
}
