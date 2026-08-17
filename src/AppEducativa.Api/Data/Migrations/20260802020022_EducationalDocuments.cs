using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppEducativa.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EducationalDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DocumentId",
                table: "AiUsageRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "AiUsageRecords",
                type: "TEXT",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenerationType",
                table: "AiUsageRecords",
                type: "TEXT",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ItemId",
                table: "AiUsageRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EducationalDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClassId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentType = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", nullable: true),
                    Instructions = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CurriculumSnapshotId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClassStructureGenerationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BloomLevel = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Difficulty = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalPoints = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    PromptVersion = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CurriculumRelease = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ObjectiveId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObjectiveCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    WarningsJson = table.Column<string>(type: "TEXT", nullable: false),
                    RequiresReview = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsCurrentVersion = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOutdated = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ConfigurationFingerprint = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationalDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentSpecifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EducationalDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EvaluationIndicatorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BloomLevel = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalPoints = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    WeightPercentage = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentSpecifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentSpecifications_EducationalDocuments_EducationalDocumentId",
                        column: x => x.EducationalDocumentId,
                        principalTable: "EducationalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EducationalDocumentGenerations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EducationalDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GenerationNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestJsonPath = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseJsonPath = table.Column<string>(type: "TEXT", nullable: true),
                    InputTokenCount = table.Column<int>(type: "INTEGER", nullable: true),
                    OutputTokenCount = table.Column<int>(type: "INTEGER", nullable: true),
                    DurationMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationalDocumentGenerations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducationalDocumentGenerations_EducationalDocuments_EducationalDocumentId",
                        column: x => x.EducationalDocumentId,
                        principalTable: "EducationalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EducationalDocumentRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EducationalDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentJsonPath = table.Column<string>(type: "TEXT", nullable: false),
                    ChangeSummary = table.Column<string>(type: "TEXT", nullable: true),
                    EditedBy = table.Column<string>(type: "TEXT", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsCurrent = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationalDocumentRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducationalDocumentRevisions_EducationalDocuments_EducationalDocumentId",
                        column: x => x.EducationalDocumentId,
                        principalTable: "EducationalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EducationalItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EducationalDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemType = table.Column<int>(type: "INTEGER", nullable: false),
                    Statement = table.Column<string>(type: "TEXT", nullable: false),
                    Instructions = table.Column<string>(type: "TEXT", nullable: true),
                    Difficulty = table.Column<int>(type: "INTEGER", nullable: false),
                    BloomLevel = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Points = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    ExpectedAnswer = table.Column<string>(type: "TEXT", nullable: true),
                    Explanation = table.Column<string>(type: "TEXT", nullable: true),
                    TeacherNotes = table.Column<string>(type: "TEXT", nullable: true),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsManuallyEdited = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    SourceGenerationId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationalItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducationalItems_EducationalDocuments_EducationalDocumentId",
                        column: x => x.EducationalDocumentId,
                        principalTable: "EducationalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EducationalItemIndicators",
                columns: table => new
                {
                    EducationalItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EvaluationIndicatorId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationalItemIndicators", x => new { x.EducationalItemId, x.EvaluationIndicatorId });
                    table.ForeignKey(
                        name: "FK_EducationalItemIndicators_EducationalItems_EducationalItemId",
                        column: x => x.EducationalItemId,
                        principalTable: "EducationalItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EducationalItemOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EducationalItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    IsCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    Feedback = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationalItemOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducationalItemOptions_EducationalItems_EducationalItemId",
                        column: x => x.EducationalItemId,
                        principalTable: "EducationalItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageRecords_DocumentId_StartedAt",
                table: "AiUsageRecords",
                columns: new[] { "DocumentId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSpecifications_EducationalDocumentId",
                table: "AssessmentSpecifications",
                column: "EducationalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EducationalDocumentGenerations_EducationalDocumentId_GenerationNumber",
                table: "EducationalDocumentGenerations",
                columns: new[] { "EducationalDocumentId", "GenerationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EducationalDocumentRevisions_EducationalDocumentId_RevisionNumber",
                table: "EducationalDocumentRevisions",
                columns: new[] { "EducationalDocumentId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EducationalDocuments_ClassId_DocumentType_IsCurrentVersion",
                table: "EducationalDocuments",
                columns: new[] { "ClassId", "DocumentType", "IsCurrentVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_EducationalDocuments_ClassId_Status",
                table: "EducationalDocuments",
                columns: new[] { "ClassId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EducationalItemOptions_EducationalItemId_Order",
                table: "EducationalItemOptions",
                columns: new[] { "EducationalItemId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_EducationalItems_EducationalDocumentId_Order",
                table: "EducationalItems",
                columns: new[] { "EducationalDocumentId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentSpecifications");

            migrationBuilder.DropTable(
                name: "EducationalDocumentGenerations");

            migrationBuilder.DropTable(
                name: "EducationalDocumentRevisions");

            migrationBuilder.DropTable(
                name: "EducationalItemIndicators");

            migrationBuilder.DropTable(
                name: "EducationalItemOptions");

            migrationBuilder.DropTable(
                name: "EducationalItems");

            migrationBuilder.DropTable(
                name: "EducationalDocuments");

            migrationBuilder.DropIndex(
                name: "IX_AiUsageRecords_DocumentId_StartedAt",
                table: "AiUsageRecords");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "AiUsageRecords");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "AiUsageRecords");

            migrationBuilder.DropColumn(
                name: "GenerationType",
                table: "AiUsageRecords");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "AiUsageRecords");
        }
    }
}
