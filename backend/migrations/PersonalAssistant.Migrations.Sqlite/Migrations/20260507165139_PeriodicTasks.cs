using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalAssistant.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class PeriodicTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PeriodicTaskGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodicTaskGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PeriodicTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    FrequencyValue = table.Column<int>(type: "INTEGER", nullable: false),
                    FrequencyUnit = table.Column<int>(type: "INTEGER", nullable: false),
                    LastDoneOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodicTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeriodicTasks_PeriodicTaskGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "PeriodicTaskGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PeriodicTaskHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PeriodicTaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompletedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodicTaskHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeriodicTaskHistory_PeriodicTasks_PeriodicTaskId",
                        column: x => x.PeriodicTaskId,
                        principalTable: "PeriodicTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicTaskGroups_OwnerUserId_IsDeleted",
                table: "PeriodicTaskGroups",
                columns: new[] { "OwnerUserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicTaskHistory_OwnerUserId_CompletedOn",
                table: "PeriodicTaskHistory",
                columns: new[] { "OwnerUserId", "CompletedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicTaskHistory_PeriodicTaskId_CompletedOn",
                table: "PeriodicTaskHistory",
                columns: new[] { "PeriodicTaskId", "CompletedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicTasks_GroupId",
                table: "PeriodicTasks",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicTasks_OwnerUserId_GroupId_IsDeleted",
                table: "PeriodicTasks",
                columns: new[] { "OwnerUserId", "GroupId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PeriodicTaskHistory");

            migrationBuilder.DropTable(
                name: "PeriodicTasks");

            migrationBuilder.DropTable(
                name: "PeriodicTaskGroups");
        }
    }
}
