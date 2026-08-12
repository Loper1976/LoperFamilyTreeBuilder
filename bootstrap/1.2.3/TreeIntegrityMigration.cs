using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoperFamilyTreeBuilder.Data.Migrations;

public partial class TreeIntegrityChecker : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TreeIntegrityIssues",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IssueKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                IssueType = table.Column<int>(type: "int", nullable: false),
                Severity = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                EvidenceSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RelatedPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RelationshipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                ReviewReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                ReviewedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                FirstDetectedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                LastDetectedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ReviewedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ResolvedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_TreeIntegrityIssues", x => x.Id));

        migrationBuilder.CreateTable(
            name: "TreeIntegrityScanRuns",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StartedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                RulesVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                CriticalCount = table.Column<int>(type: "int", nullable: false),
                HighCount = table.Column<int>(type: "int", nullable: false),
                MediumCount = table.Column<int>(type: "int", nullable: false),
                LowCount = table.Column<int>(type: "int", nullable: false),
                InformationalCount = table.Column<int>(type: "int", nullable: false),
                TotalFindings = table.Column<int>(type: "int", nullable: false),
                DurationMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                StartedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CompletedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_TreeIntegrityScanRuns", x => x.Id));

        migrationBuilder.CreateIndex("IX_TreeIntegrityIssues_IssueKey", "TreeIntegrityIssues", "IssueKey", unique: true);
        migrationBuilder.CreateIndex("IX_TreeIntegrityIssues_IsActive_Severity_Status", "TreeIntegrityIssues", new[] { "IsActive", "Severity", "Status" });
        migrationBuilder.CreateIndex("IX_TreeIntegrityIssues_LastDetectedUtc", "TreeIntegrityIssues", "LastDetectedUtc");
        migrationBuilder.CreateIndex("IX_TreeIntegrityIssues_PersonId", "TreeIntegrityIssues", "PersonId");
        migrationBuilder.CreateIndex("IX_TreeIntegrityIssues_RelatedPersonId", "TreeIntegrityIssues", "RelatedPersonId");
        migrationBuilder.CreateIndex("IX_TreeIntegrityScanRuns_CompletedUtc", "TreeIntegrityScanRuns", "CompletedUtc");
        migrationBuilder.CreateIndex("IX_TreeIntegrityScanRuns_StartedUtc", "TreeIntegrityScanRuns", "StartedUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("TreeIntegrityIssues");
        migrationBuilder.DropTable("TreeIntegrityScanRuns");
    }
}
