using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProfeAsistente.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentExports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentExports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentType = table.Column<int>(type: "INTEGER", nullable: false),
                    Audience = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanningId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClassId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EducationalDocumentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    RelativeFilePath = table.Column<string>(type: "TEXT", nullable: true),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    Sha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RequestedBy = table.Column<string>(type: "TEXT", nullable: true),
                    OptionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CurriculumSnapshotId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CurriculumReleaseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    WarningsJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentExports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentExports_ClassId",
                table: "DocumentExports",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentExports_EducationalDocumentId",
                table: "DocumentExports",
                column: "EducationalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentExports_PlanningId",
                table: "DocumentExports",
                column: "PlanningId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentExports_RequestedAt",
                table: "DocumentExports",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentExports_Status_ExpiresAt",
                table: "DocumentExports",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentExports");
        }
    }
}
