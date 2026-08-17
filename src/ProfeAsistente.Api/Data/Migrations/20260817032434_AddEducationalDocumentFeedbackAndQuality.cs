using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProfeAsistente.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEducationalDocumentFeedbackAndQuality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QualityReportJson",
                table: "EducationalDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EducationalDocumentFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EducationalDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GenerationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Useful = table.Column<bool>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PromptVersion = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationalDocumentFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducationalDocumentFeedbacks_EducationalDocuments_EducationalDocumentId",
                        column: x => x.EducationalDocumentId,
                        principalTable: "EducationalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EducationalDocumentFeedbacks_EducationalDocumentId",
                table: "EducationalDocumentFeedbacks",
                column: "EducationalDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EducationalDocumentFeedbacks");

            migrationBuilder.DropColumn(
                name: "QualityReportJson",
                table: "EducationalDocuments");
        }
    }
}
