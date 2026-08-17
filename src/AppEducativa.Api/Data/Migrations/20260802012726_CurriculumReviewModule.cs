using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppEducativa.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CurriculumReviewModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurriculumReleaseId",
                table: "Unidades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PublicationStatus",
                table: "Unidades",
                type: "INTEGER",
                nullable: false,
                // Published=1: filas existentes siguen visibles; los imports nuevos fijan Draft en código.
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "CurriculumReleaseId",
                table: "ObjetivosAprendizaje",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PublicationStatus",
                table: "ObjetivosAprendizaje",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ChangeType",
                table: "CurriculumReviewChanges",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChangedAt",
                table: "CurriculumReviewChanges",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ChangedBy",
                table: "CurriculumReviewChanges",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurriculumReviewSessionId",
                table: "CurriculumReviewChanges",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityTemporaryId",
                table: "CurriculumReviewChanges",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FieldName",
                table: "CurriculumReviewChanges",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsReverted",
                table: "CurriculumReviewChanges",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreviousValue",
                table: "CurriculumReviewChanges",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "CurriculumReviewChanges",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevertedAt",
                table: "CurriculumReviewChanges",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActiveReviewSessionId",
                table: "CurriculumImportBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurriculumReleaseId",
                table: "CurriculumImportBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalReviewJson",
                table: "CurriculumImportBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalReviewJsonPath",
                table: "CurriculumImportBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "CurriculumImportBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadyAt",
                table: "CurriculumImportBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReadyBy",
                table: "CurriculumImportBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewContentHash",
                table: "CurriculumImportBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CurriculumReleases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PublishedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SourceDocumentCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ImportBatchCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CurriculumImportBatchId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumReleases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumReviewSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurriculumImportBatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaUltimaModificacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevisadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    ObservacionGeneral = table.Column<string>(type: "TEXT", nullable: true),
                    VersionRevision = table.Column<int>(type: "INTEGER", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ReviewPackageJson = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewPackagePath = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewContentHash = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewContentPath = table.Column<string>(type: "TEXT", nullable: true),
                    ReadyAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReadyBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastValidationAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastDiffAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DiffJson = table.Column<string>(type: "TEXT", nullable: true),
                    IssuesJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumReviewSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumReviewSessions_CurriculumImportBatches_CurriculumImportBatchId",
                        column: x => x.CurriculumImportBatchId,
                        principalTable: "CurriculumImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumReviewComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurriculumReviewSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", nullable: true),
                    EntityTemporaryId = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsResolved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumReviewComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumReviewComments_CurriculumReviewSessions_CurriculumReviewSessionId",
                        column: x => x.CurriculumReviewSessionId,
                        principalTable: "CurriculumReviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumReviewDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurriculumReviewSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", nullable: false),
                    EntityTemporaryId = table.Column<string>(type: "TEXT", nullable: false),
                    Decision = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DecidedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumReviewDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumReviewDecisions_CurriculumReviewSessions_CurriculumReviewSessionId",
                        column: x => x.CurriculumReviewSessionId,
                        principalTable: "CurriculumReviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumReviewChanges_CurriculumReviewSessionId",
                table: "CurriculumReviewChanges",
                column: "CurriculumReviewSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumReleases_CurriculumImportBatchId",
                table: "CurriculumReleases",
                column: "CurriculumImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumReleases_PublishedAt",
                table: "CurriculumReleases",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumReviewComments_CurriculumReviewSessionId",
                table: "CurriculumReviewComments",
                column: "CurriculumReviewSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumReviewDecisions_CurriculumReviewSessionId_EntityTemporaryId",
                table: "CurriculumReviewDecisions",
                columns: new[] { "CurriculumReviewSessionId", "EntityTemporaryId" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumReviewSessions_CurriculumImportBatchId",
                table: "CurriculumReviewSessions",
                column: "CurriculumImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumReviewSessions_Estado",
                table: "CurriculumReviewSessions",
                column: "Estado");

            migrationBuilder.AddForeignKey(
                name: "FK_CurriculumReviewChanges_CurriculumReviewSessions_CurriculumReviewSessionId",
                table: "CurriculumReviewChanges",
                column: "CurriculumReviewSessionId",
                principalTable: "CurriculumReviewSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CurriculumReviewChanges_CurriculumReviewSessions_CurriculumReviewSessionId",
                table: "CurriculumReviewChanges");

            migrationBuilder.DropTable(
                name: "CurriculumReleases");

            migrationBuilder.DropTable(
                name: "CurriculumReviewComments");

            migrationBuilder.DropTable(
                name: "CurriculumReviewDecisions");

            migrationBuilder.DropTable(
                name: "CurriculumReviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumReviewChanges_CurriculumReviewSessionId",
                table: "CurriculumReviewChanges");

            migrationBuilder.DropColumn(
                name: "CurriculumReleaseId",
                table: "Unidades");

            migrationBuilder.DropColumn(
                name: "PublicationStatus",
                table: "Unidades");

            migrationBuilder.DropColumn(
                name: "CurriculumReleaseId",
                table: "ObjetivosAprendizaje");

            migrationBuilder.DropColumn(
                name: "PublicationStatus",
                table: "ObjetivosAprendizaje");

            migrationBuilder.DropColumn(
                name: "ChangeType",
                table: "CurriculumReviewChanges");

            migrationBuilder.DropColumn(
                name: "ChangedAt",
                table: "CurriculumReviewChanges");

            migrationBuilder.DropColumn(
                name: "ChangedBy",
                table: "CurriculumReviewChanges");

            migrationBuilder.DropColumn(
                name: "CurriculumReviewSessionId",
                table: "CurriculumReviewChanges");

            migrationBuilder.DropColumn(
                name: "EntityTemporaryId",
                table: "CurriculumReviewChanges");

            migrationBuilder.DropColumn(
                name: "FieldName",
                table: "CurriculumReviewChanges");

            migrationBuilder.DropColumn(
                name: "IsReverted",
                table: "CurriculumReviewChanges");

            migrationBuilder.DropColumn(
                name: "PreviousValue",
                table: "CurriculumReviewChanges");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "CurriculumReviewChanges");

            migrationBuilder.DropColumn(
                name: "RevertedAt",
                table: "CurriculumReviewChanges");

            migrationBuilder.DropColumn(
                name: "ActiveReviewSessionId",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "CurriculumReleaseId",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "FinalReviewJson",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "FinalReviewJsonPath",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "ReadyAt",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "ReadyBy",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "ReviewContentHash",
                table: "CurriculumImportBatches");
        }
    }
}
