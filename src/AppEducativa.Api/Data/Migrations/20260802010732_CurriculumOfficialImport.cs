using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppEducativa.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CurriculumOfficialImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CurriculumDocuments_HashSha256",
                table: "CurriculumDocuments");

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "CurriculumSources",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaUltimaRevision",
                table: "CurriculumSources",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CantidadActitudes",
                table: "CurriculumImportBatches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CantidadHabilidades",
                table: "CurriculumImportBatches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CantidadIndicadores",
                table: "CurriculumImportBatches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CantidadOA",
                table: "CurriculumImportBatches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CantidadUnidades",
                table: "CurriculumImportBatches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "ConfianzaPromedio",
                table: "CurriculumImportBatches",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "CorrectedExtractionJson",
                table: "CurriculumImportBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectedJsonPath",
                table: "CurriculumImportBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurriculumSourceId",
                table: "CurriculumImportBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractionJsonPath",
                table: "CurriculumImportBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAprobacion",
                table: "CurriculumImportBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalExtractionJson",
                table: "CurriculumImportBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceExternalId",
                table: "CurriculumImportBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "CurriculumImportBatches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioRevisor",
                table: "CurriculumImportBatches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "CurriculumDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaProcesamiento",
                table: "CurriculumDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "CurriculumDocuments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "TextoExtraidoPath",
                table: "CurriculumDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CurriculumReviewChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurriculumImportBatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", nullable: false),
                    EntityKey = table.Column<string>(type: "TEXT", nullable: false),
                    Field = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalValue = table.Column<string>(type: "TEXT", nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", nullable: true),
                    UsuarioRevisor = table.Column<string>(type: "TEXT", nullable: true),
                    FechaCambio = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumReviewChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumReviewChanges_CurriculumImportBatches_CurriculumImportBatchId",
                        column: x => x.CurriculumImportBatchId,
                        principalTable: "CurriculumImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumSources_ExternalId",
                table: "CurriculumSources",
                column: "ExternalId",
                unique: true,
                filter: "\"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumImportBatches_CurriculumDocumentId",
                table: "CurriculumImportBatches",
                column: "CurriculumDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumImportBatches_CurriculumSourceId_Status",
                table: "CurriculumImportBatches",
                columns: new[] { "CurriculumSourceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumDocuments_HashSha256",
                table: "CurriculumDocuments",
                column: "HashSha256",
                unique: true,
                filter: "\"HashSha256\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumReviewChanges_CurriculumImportBatchId",
                table: "CurriculumReviewChanges",
                column: "CurriculumImportBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_CurriculumImportBatches_CurriculumDocuments_CurriculumDocumentId",
                table: "CurriculumImportBatches",
                column: "CurriculumDocumentId",
                principalTable: "CurriculumDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CurriculumImportBatches_CurriculumSources_CurriculumSourceId",
                table: "CurriculumImportBatches",
                column: "CurriculumSourceId",
                principalTable: "CurriculumSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CurriculumImportBatches_CurriculumDocuments_CurriculumDocumentId",
                table: "CurriculumImportBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_CurriculumImportBatches_CurriculumSources_CurriculumSourceId",
                table: "CurriculumImportBatches");

            migrationBuilder.DropTable(
                name: "CurriculumReviewChanges");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumSources_ExternalId",
                table: "CurriculumSources");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumImportBatches_CurriculumDocumentId",
                table: "CurriculumImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumImportBatches_CurriculumSourceId_Status",
                table: "CurriculumImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumDocuments_HashSha256",
                table: "CurriculumDocuments");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "CurriculumSources");

            migrationBuilder.DropColumn(
                name: "FechaUltimaRevision",
                table: "CurriculumSources");

            migrationBuilder.DropColumn(
                name: "CantidadActitudes",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "CantidadHabilidades",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "CantidadIndicadores",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "CantidadOA",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "CantidadUnidades",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "ConfianzaPromedio",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "CorrectedExtractionJson",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "CorrectedJsonPath",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "CurriculumSourceId",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "ExtractionJsonPath",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "FechaAprobacion",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "OriginalExtractionJson",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "SourceExternalId",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "UsuarioRevisor",
                table: "CurriculumImportBatches");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "CurriculumDocuments");

            migrationBuilder.DropColumn(
                name: "FechaProcesamiento",
                table: "CurriculumDocuments");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "CurriculumDocuments");

            migrationBuilder.DropColumn(
                name: "TextoExtraidoPath",
                table: "CurriculumDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumDocuments_HashSha256",
                table: "CurriculumDocuments",
                column: "HashSha256");
        }
    }
}
