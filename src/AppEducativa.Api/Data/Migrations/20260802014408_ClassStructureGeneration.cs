using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppEducativa.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClassStructureGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiUsageRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OperationType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ClassId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InputTokens = table.Column<int>(type: "INTEGER", nullable: true),
                    OutputTokens = table.Column<int>(type: "INTEGER", nullable: true),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUsageRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassStructureGenerations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClassId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GenerationNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    PromptVersion = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CurriculumSnapshotId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RequestJsonPath = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseJsonPath = table.Column<string>(type: "TEXT", nullable: true),
                    GeneratedTitle = table.Column<string>(type: "TEXT", nullable: true),
                    GeneratedPurpose = table.Column<string>(type: "TEXT", nullable: true),
                    GeneratedStartJson = table.Column<string>(type: "TEXT", nullable: true),
                    GeneratedDevelopmentJson = table.Column<string>(type: "TEXT", nullable: true),
                    GeneratedClosureJson = table.Column<string>(type: "TEXT", nullable: true),
                    FormativeAssessmentJson = table.Column<string>(type: "TEXT", nullable: true),
                    DifferentiationJson = table.Column<string>(type: "TEXT", nullable: true),
                    CurriculumReferenceJson = table.Column<string>(type: "TEXT", nullable: true),
                    RequiresReview = table.Column<bool>(type: "INTEGER", nullable: false),
                    WarningsJson = table.Column<string>(type: "TEXT", nullable: false),
                    InputTokenCount = table.Column<int>(type: "INTEGER", nullable: true),
                    OutputTokenCount = table.Column<int>(type: "INTEGER", nullable: true),
                    DurationMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    IsCurrentVersion = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOutdated = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ConfigurationFingerprint = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassStructureGenerations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassStructureRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GenerationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", nullable: false),
                    StartJson = table.Column<string>(type: "TEXT", nullable: false),
                    DevelopmentJson = table.Column<string>(type: "TEXT", nullable: false),
                    ClosureJson = table.Column<string>(type: "TEXT", nullable: false),
                    FormativeAssessmentJson = table.Column<string>(type: "TEXT", nullable: true),
                    DifferentiationJson = table.Column<string>(type: "TEXT", nullable: true),
                    EditedBy = table.Column<string>(type: "TEXT", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsCurrent = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChangeSummary = table.Column<string>(type: "TEXT", nullable: true),
                    WasManuallyModified = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassStructureRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassStructureRevisions_ClassStructureGenerations_GenerationId",
                        column: x => x.GenerationId,
                        principalTable: "ClassStructureGenerations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageRecords_ClassId_StartedAt",
                table: "AiUsageRecords",
                columns: new[] { "ClassId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageRecords_OperationType",
                table: "AiUsageRecords",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_ClassStructureGenerations_ClassId_GenerationNumber",
                table: "ClassStructureGenerations",
                columns: new[] { "ClassId", "GenerationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassStructureGenerations_ClassId_IsCurrentVersion",
                table: "ClassStructureGenerations",
                columns: new[] { "ClassId", "IsCurrentVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassStructureGenerations_ClassId_Status",
                table: "ClassStructureGenerations",
                columns: new[] { "ClassId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassStructureRevisions_GenerationId_IsCurrent",
                table: "ClassStructureRevisions",
                columns: new[] { "GenerationId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassStructureRevisions_GenerationId_RevisionNumber",
                table: "ClassStructureRevisions",
                columns: new[] { "GenerationId", "RevisionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiUsageRecords");

            migrationBuilder.DropTable(
                name: "ClassStructureRevisions");

            migrationBuilder.DropTable(
                name: "ClassStructureGenerations");
        }
    }
}
