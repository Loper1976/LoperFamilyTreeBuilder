using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoperFamilyTreeBuilder.Data.Migrations;

[DbContext(typeof(FamilyTreeDbContext))]
[Migration("20260812040000_AddMedicalFamilyHealthFoundation")]
public partial class AddMedicalFamilyHealthFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MedicalConditions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ConditionName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                Severity = table.Column<int>(type: "int", nullable: false),
                IsHereditaryRelevant = table.Column<bool>(type: "bit", nullable: false),
                DiagnosisDate = table.Column<DateOnly>(type: "date", nullable: true),
                OnsetAgeYears = table.Column<int>(type: "int", nullable: true),
                Provider = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                Facility = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                SourceCitation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Visibility = table.Column<int>(type: "int", nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MedicalConditions", x => x.Id);
                table.ForeignKey(
                    name: "FK_MedicalConditions_People_PersonId",
                    column: x => x.PersonId,
                    principalTable: "People",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MedicalConditions_ConditionName",
            table: "MedicalConditions",
            column: "ConditionName");

        migrationBuilder.CreateIndex(
            name: "IX_MedicalConditions_IsHereditaryRelevant",
            table: "MedicalConditions",
            column: "IsHereditaryRelevant");

        migrationBuilder.CreateIndex(
            name: "IX_MedicalConditions_PersonId",
            table: "MedicalConditions",
            column: "PersonId");

        migrationBuilder.CreateIndex(
            name: "IX_MedicalConditions_Visibility",
            table: "MedicalConditions",
            column: "Visibility");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "MedicalConditions");
    }
}
