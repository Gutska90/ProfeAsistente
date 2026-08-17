using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProfeAsistente.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPilotSessionReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PilotSessionReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClassId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MinutesSavedEstimate = table.Column<int>(type: "INTEGER", nullable: false),
                    WouldUseAgain = table.Column<bool>(type: "INTEGER", nullable: true),
                    MaterialsUsedInClass = table.Column<bool>(type: "INTEGER", nullable: true),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PilotSessionReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PilotSessionReports_CreatedAt",
                table: "PilotSessionReports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PilotSessionReports_UserId_CreatedAt",
                table: "PilotSessionReports",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PilotSessionReports");
        }
    }
}
