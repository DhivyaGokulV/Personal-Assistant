using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PersonalAssistant.Infrastructure.Persistence;

#nullable disable

namespace PersonalAssistant.Migrations.SqlServer.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260712020912_AssetPreciousMetalsPossessions")]
    /// <inheritdoc />
    public partial class AssetPreciousMetalsPossessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JewelleryAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JewelleryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_JewelleryAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JewelleryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BuyingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BuyingPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityInGrams = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SellingDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SellingPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    SellingNote = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JewelleryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonalAssetAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonalAssetItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_PersonalAssetAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonalAssetItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BuyingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BuyingPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SellingDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SellingPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    SellingNote = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalAssetItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreciousMetalAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreciousMetalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_PreciousMetalAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreciousMetals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreciousMetals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreciousMetalPriceHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreciousMetalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsOf = table.Column<DateOnly>(type: "date", nullable: false),
                    PricePerUnit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreciousMetalPriceHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreciousMetalPriceHistory_PreciousMetals_PreciousMetalId",
                        column: x => x.PreciousMetalId,
                        principalTable: "PreciousMetals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreciousMetalTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreciousMetalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PricePerUnit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreciousMetalTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreciousMetalTransactions_PreciousMetals_PreciousMetalId",
                        column: x => x.PreciousMetalId,
                        principalTable: "PreciousMetals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JewelleryAuditEntries_OwnerUserId_JewelleryItemId_CreatedAt",
                table: "JewelleryAuditEntries",
                columns: new[] { "OwnerUserId", "JewelleryItemId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JewelleryItems_OwnerUserId_Status_BuyingDate_IsDeleted",
                table: "JewelleryItems",
                columns: new[] { "OwnerUserId", "Status", "BuyingDate", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalAssetAuditEntries_OwnerUserId_PersonalAssetItemId_CreatedAt",
                table: "PersonalAssetAuditEntries",
                columns: new[] { "OwnerUserId", "PersonalAssetItemId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalAssetItems_OwnerUserId_Status_BuyingDate_IsDeleted",
                table: "PersonalAssetItems",
                columns: new[] { "OwnerUserId", "Status", "BuyingDate", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PreciousMetalAuditEntries_OwnerUserId_PreciousMetalId_CreatedAt",
                table: "PreciousMetalAuditEntries",
                columns: new[] { "OwnerUserId", "PreciousMetalId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PreciousMetalPriceHistory_PreciousMetalId_AsOf_IsDeleted",
                table: "PreciousMetalPriceHistory",
                columns: new[] { "PreciousMetalId", "AsOf", "IsDeleted" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreciousMetals_OwnerUserId_Name_IsDeleted",
                table: "PreciousMetals",
                columns: new[] { "OwnerUserId", "Name", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PreciousMetalTransactions_PreciousMetalId_Date",
                table: "PreciousMetalTransactions",
                columns: new[] { "PreciousMetalId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JewelleryAuditEntries");

            migrationBuilder.DropTable(
                name: "JewelleryItems");

            migrationBuilder.DropTable(
                name: "PersonalAssetAuditEntries");

            migrationBuilder.DropTable(
                name: "PersonalAssetItems");

            migrationBuilder.DropTable(
                name: "PreciousMetalAuditEntries");

            migrationBuilder.DropTable(
                name: "PreciousMetalPriceHistory");

            migrationBuilder.DropTable(
                name: "PreciousMetalTransactions");

            migrationBuilder.DropTable(
                name: "PreciousMetals");
        }
    }
}

