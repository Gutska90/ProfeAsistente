using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProfeAsistente.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentReuseP12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTemplate",
                table: "EducationalDocuments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceDocumentId",
                table: "EducationalDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EducationalDocuments_IsTemplate_UpdatedAt",
                table: "EducationalDocuments",
                columns: new[] { "IsTemplate", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EducationalDocuments_SourceDocumentId",
                table: "EducationalDocuments",
                column: "SourceDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EducationalDocuments_IsTemplate_UpdatedAt",
                table: "EducationalDocuments");

            migrationBuilder.DropIndex(
                name: "IX_EducationalDocuments_SourceDocumentId",
                table: "EducationalDocuments");

            migrationBuilder.DropColumn(
                name: "IsTemplate",
                table: "EducationalDocuments");

            migrationBuilder.DropColumn(
                name: "SourceDocumentId",
                table: "EducationalDocuments");
        }
    }
}
