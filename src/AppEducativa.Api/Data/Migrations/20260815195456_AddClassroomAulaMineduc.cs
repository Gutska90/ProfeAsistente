using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppEducativa.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClassroomAulaMineduc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DuaUnitNotes",
                table: "Planificaciones",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PeiAlignment",
                table: "Planificaciones",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PmeAction",
                table: "Planificaciones",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvaluationRegulationNotes",
                table: "EducationalInstitutions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PeiSeals",
                table: "EducationalInstitutions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PeiVision",
                table: "EducationalInstitutions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssessmentScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LearningAssessmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Score = table.Column<decimal>(type: "TEXT", nullable: true),
                    AchievementLevel = table.Column<string>(type: "TEXT", nullable: true),
                    Feedback = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClassId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Justification = table.Column<string>(type: "TEXT", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassDuaStrategies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClassId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Principle = table.Column<int>(type: "INTEGER", nullable: false),
                    Strategy = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassDuaStrategies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassFeedbackNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClassId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassFeedbackNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SchoolCourseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    EnrolledOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEnrollments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SchoolCourseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClassId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PlanningId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ObjectiveLearningId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EducationalDocumentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Purpose = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Criteria = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningAssessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentSupportPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanType = table.Column<int>(type: "INTEGER", nullable: false),
                    NeedType = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Strategies = table.Column<string>(type: "TEXT", nullable: false),
                    AccessAdjustments = table.Column<string>(type: "TEXT", nullable: true),
                    ObjectiveAdjustments = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSupportPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentScores_LearningAssessmentId_StudentId",
                table: "AssessmentScores",
                columns: new[] { "LearningAssessmentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ClassId_StudentId",
                table: "AttendanceRecords",
                columns: new[] { "ClassId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassDuaStrategies_ClassId",
                table: "ClassDuaStrategies",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassFeedbackNotes_ClassId",
                table: "ClassFeedbackNotes",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_SchoolCourseId_StudentId",
                table: "CourseEnrollments",
                columns: new[] { "SchoolCourseId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningAssessments_InstitutionId_Date",
                table: "LearningAssessments",
                columns: new[] { "InstitutionId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningAssessments_SchoolCourseId",
                table: "LearningAssessments",
                column: "SchoolCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_InstitutionId_LastName_FirstName",
                table: "Students",
                columns: new[] { "InstitutionId", "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentSupportPlans_StudentId_IsActive",
                table: "StudentSupportPlans",
                columns: new[] { "StudentId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentScores");

            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "ClassDuaStrategies");

            migrationBuilder.DropTable(
                name: "ClassFeedbackNotes");

            migrationBuilder.DropTable(
                name: "CourseEnrollments");

            migrationBuilder.DropTable(
                name: "LearningAssessments");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "StudentSupportPlans");

            migrationBuilder.DropColumn(
                name: "DuaUnitNotes",
                table: "Planificaciones");

            migrationBuilder.DropColumn(
                name: "PeiAlignment",
                table: "Planificaciones");

            migrationBuilder.DropColumn(
                name: "PmeAction",
                table: "Planificaciones");

            migrationBuilder.DropColumn(
                name: "EvaluationRegulationNotes",
                table: "EducationalInstitutions");

            migrationBuilder.DropColumn(
                name: "PeiSeals",
                table: "EducationalInstitutions");

            migrationBuilder.DropColumn(
                name: "PeiVision",
                table: "EducationalInstitutions");
        }
    }
}
