using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppEducativa.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanningCalendarAndCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ActualDate",
                table: "Clases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActualDurationMinutes",
                table: "Clases",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClassType",
                table: "Clases",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Clases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionNotes",
                table: "Clases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "Clases",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Proposito",
                table: "Clases",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "Clases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Titulo",
                table: "Clases",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClassLearningEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClassId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EvaluationIndicatorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EvidenceType = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassLearningEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassLearningEvidences_Clases_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Clases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearningObjectiveDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanningId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PrerequisiteObjectiveId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DependentObjectiveId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DependencyType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsMandatory = table.Column<bool>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningObjectiveDependencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanningAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanningId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClassId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ObjectiveId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IndicatorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AlertCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsResolved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "TEXT", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ContextJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanningCalendarSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanningId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    ClassId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockReason = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CancelReason = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningCalendarSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningCalendarSessions_Clases_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Clases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlanningCalendarSessions_Planificaciones_PlanningId",
                        column: x => x.PlanningId,
                        principalTable: "Planificaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanningScheduleConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanningId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TimeZoneId = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    DefaultClassDurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IncludeStartDate = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludeEndDate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningScheduleConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningScheduleConfigurations_Planificaciones_PlanningId",
                        column: x => x.PlanningId,
                        principalTable: "Planificaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanningSequenceProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanningId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProposalNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConfigurationJson = table.Column<string>(type: "TEXT", nullable: false),
                    SummaryJson = table.Column<string>(type: "TEXT", nullable: false),
                    WarningJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsCurrent = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlanningVersionHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningSequenceProposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningSequenceProposals_Planificaciones_PlanningId",
                        column: x => x.PlanningId,
                        principalTable: "Planificaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanningSuggestionStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanningId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SuggestionCode = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsIgnored = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsApplied = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningSuggestionStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanningSessionHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanningCalendarSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PreviousDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    NewDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PreviousStartTime = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    NewStartTime = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChangedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningSessionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningSessionHistories_PlanningCalendarSessions_PlanningCalendarSessionId",
                        column: x => x.PlanningCalendarSessionId,
                        principalTable: "PlanningCalendarSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanningExcludedDates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanningScheduleConfigurationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ExclusionType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRecurring = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningExcludedDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningExcludedDates_PlanningScheduleConfigurations_PlanningScheduleConfigurationId",
                        column: x => x.PlanningScheduleConfigurationId,
                        principalTable: "PlanningScheduleConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyClassSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanningScheduleConfigurationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionsPerDay = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyClassSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyClassSchedules_PlanningScheduleConfigurations_PlanningScheduleConfigurationId",
                        column: x => x.PlanningScheduleConfigurationId,
                        principalTable: "PlanningScheduleConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanningSequenceProposalItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanningSequenceProposalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    CalendarSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObjectiveLearningId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BloomLevel = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    SuggestedTitle = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    SuggestedPurpose = table.Column<string>(type: "TEXT", nullable: true),
                    SuggestedIndicatorIdsJson = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedSkillIdsJson = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedAttitudeIdsJson = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedTransversalObjectiveIdsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ClassType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    WasManuallyModified = table.Column<bool>(type: "INTEGER", nullable: false),
                    WarningJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningSequenceProposalItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningSequenceProposalItems_PlanningCalendarSessions_CalendarSessionId",
                        column: x => x.CalendarSessionId,
                        principalTable: "PlanningCalendarSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanningSequenceProposalItems_PlanningSequenceProposals_PlanningSequenceProposalId",
                        column: x => x.PlanningSequenceProposalId,
                        principalTable: "PlanningSequenceProposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanningSequenceItemIndicators",
                columns: table => new
                {
                    PlanningSequenceProposalItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EvaluationIndicatorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsageType = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningSequenceItemIndicators", x => new { x.PlanningSequenceProposalItemId, x.EvaluationIndicatorId });
                    table.ForeignKey(
                        name: "FK_PlanningSequenceItemIndicators_PlanningSequenceProposalItems_PlanningSequenceProposalItemId",
                        column: x => x.PlanningSequenceProposalItemId,
                        principalTable: "PlanningSequenceProposalItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassLearningEvidences_ClassId_EvaluationIndicatorId",
                table: "ClassLearningEvidences",
                columns: new[] { "ClassId", "EvaluationIndicatorId" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningObjectiveDependencies_PlanningId_PrerequisiteObjectiveId_DependentObjectiveId",
                table: "LearningObjectiveDependencies",
                columns: new[] { "PlanningId", "PrerequisiteObjectiveId", "DependentObjectiveId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanningAlerts_PlanningId_IsResolved_Severity",
                table: "PlanningAlerts",
                columns: new[] { "PlanningId", "IsResolved", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanningCalendarSessions_ClassId",
                table: "PlanningCalendarSessions",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningCalendarSessions_PlanningId_ScheduledDate",
                table: "PlanningCalendarSessions",
                columns: new[] { "PlanningId", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanningCalendarSessions_PlanningId_SessionNumber",
                table: "PlanningCalendarSessions",
                columns: new[] { "PlanningId", "SessionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanningExcludedDates_PlanningScheduleConfigurationId_Date",
                table: "PlanningExcludedDates",
                columns: new[] { "PlanningScheduleConfigurationId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanningScheduleConfigurations_PlanningId",
                table: "PlanningScheduleConfigurations",
                column: "PlanningId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanningSequenceProposalItems_CalendarSessionId",
                table: "PlanningSequenceProposalItems",
                column: "CalendarSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningSequenceProposalItems_PlanningSequenceProposalId_Order",
                table: "PlanningSequenceProposalItems",
                columns: new[] { "PlanningSequenceProposalId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanningSequenceProposals_PlanningId_IsCurrent",
                table: "PlanningSequenceProposals",
                columns: new[] { "PlanningId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanningSessionHistories_PlanningCalendarSessionId",
                table: "PlanningSessionHistories",
                column: "PlanningCalendarSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningSuggestionStates_PlanningId_SuggestionCode",
                table: "PlanningSuggestionStates",
                columns: new[] { "PlanningId", "SuggestionCode" });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyClassSchedules_PlanningScheduleConfigurationId_DayOfWeek_StartTime",
                table: "WeeklyClassSchedules",
                columns: new[] { "PlanningScheduleConfigurationId", "DayOfWeek", "StartTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassLearningEvidences");

            migrationBuilder.DropTable(
                name: "LearningObjectiveDependencies");

            migrationBuilder.DropTable(
                name: "PlanningAlerts");

            migrationBuilder.DropTable(
                name: "PlanningExcludedDates");

            migrationBuilder.DropTable(
                name: "PlanningSequenceItemIndicators");

            migrationBuilder.DropTable(
                name: "PlanningSessionHistories");

            migrationBuilder.DropTable(
                name: "PlanningSuggestionStates");

            migrationBuilder.DropTable(
                name: "WeeklyClassSchedules");

            migrationBuilder.DropTable(
                name: "PlanningSequenceProposalItems");

            migrationBuilder.DropTable(
                name: "PlanningScheduleConfigurations");

            migrationBuilder.DropTable(
                name: "PlanningCalendarSessions");

            migrationBuilder.DropTable(
                name: "PlanningSequenceProposals");

            migrationBuilder.DropColumn(
                name: "ActualDate",
                table: "Clases");

            migrationBuilder.DropColumn(
                name: "ActualDurationMinutes",
                table: "Clases");

            migrationBuilder.DropColumn(
                name: "ClassType",
                table: "Clases");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Clases");

            migrationBuilder.DropColumn(
                name: "CompletionNotes",
                table: "Clases");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "Clases");

            migrationBuilder.DropColumn(
                name: "Proposito",
                table: "Clases");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Clases");

            migrationBuilder.DropColumn(
                name: "Titulo",
                table: "Clases");
        }
    }
}
