using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProfeAsistente.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiUsageObservabilityP11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedCostUsd",
                table: "AiUsageRecords",
                type: "TEXT",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GenerationId",
                table: "AiUsageRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InstitutionId",
                table: "AiUsageRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LatencyMilliseconds",
                table: "AiUsageRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "PromptId",
                table: "AiUsageRecords",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PromptVersion",
                table: "AiUsageRecords",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "AiUsageRecords",
                type: "TEXT",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "AiUsageRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageRecords_InstitutionId_StartedAt",
                table: "AiUsageRecords",
                columns: new[] { "InstitutionId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageRecords_Purpose_StartedAt",
                table: "AiUsageRecords",
                columns: new[] { "Purpose", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageRecords_UserId_StartedAt",
                table: "AiUsageRecords",
                columns: new[] { "UserId", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiUsageRecords_InstitutionId_StartedAt",
                table: "AiUsageRecords");

            migrationBuilder.DropIndex(
                name: "IX_AiUsageRecords_Purpose_StartedAt",
                table: "AiUsageRecords");

            migrationBuilder.DropIndex(
                name: "IX_AiUsageRecords_UserId_StartedAt",
                table: "AiUsageRecords");

            migrationBuilder.DropColumn(
                name: "EstimatedCostUsd",
                table: "AiUsageRecords");

            migrationBuilder.DropColumn(
                name: "GenerationId",
                table: "AiUsageRecords");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "AiUsageRecords");

            migrationBuilder.DropColumn(
                name: "LatencyMilliseconds",
                table: "AiUsageRecords");

            migrationBuilder.DropColumn(
                name: "PromptId",
                table: "AiUsageRecords");

            migrationBuilder.DropColumn(
                name: "PromptVersion",
                table: "AiUsageRecords");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "AiUsageRecords");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AiUsageRecords");
        }
    }
}
