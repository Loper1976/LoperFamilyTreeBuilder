using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoperFamilyTreeBuilder.Data.Migrations;

[DbContext(typeof(FamilyTreeDbContext))]
[Migration("20260812060000_AddDnaMatchClustering")]
public partial class AddDnaMatchClustering : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DnaMatches",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProviderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ExternalMatchId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                DisplayName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                TotalCentimorgans = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                SharedSegments = table.Column<int>(type: "int", nullable: true),
                Visibility = table.Column<int>(type: "int", nullable: false),
                ReviewStatus = table.Column<int>(type: "int", nullable: false),
                ManualAncestralLineLabel = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                ResearchNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DnaMatches", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DnaSharedMatches",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MatchAId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MatchBId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EvidenceSource = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DnaSharedMatches", x => x.Id);
                table.ForeignKey(
                    name: "FK_DnaSharedMatches_DnaMatches_MatchAId",
                    column: x => x.MatchAId,
                    principalTable: "DnaMatches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.NoAction);
                table.ForeignKey(
                    name: "FK_DnaSharedMatches_DnaMatches_MatchBId",
                    column: x => x.MatchBId,
                    principalTable: "DnaMatches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.NoAction);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DnaMatches_ProviderName_ExternalMatchId",
            table: "DnaMatches",
            columns: new[] { "ProviderName", "ExternalMatchId" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_DnaMatches_ReviewStatus",
            table: "DnaMatches",
            column: "ReviewStatus");
        migrationBuilder.CreateIndex(
            name: "IX_DnaMatches_TotalCentimorgans",
            table: "DnaMatches",
            column: "TotalCentimorgans");
        migrationBuilder.CreateIndex(
            name: "IX_DnaMatches_Visibility",
            table: "DnaMatches",
            column: "Visibility");
        migrationBuilder.CreateIndex(
            name: "IX_DnaSharedMatches_CreatedUtc",
            table: "DnaSharedMatches",
            column: "CreatedUtc");
        migrationBuilder.CreateIndex(
            name: "IX_DnaSharedMatches_MatchAId_MatchBId",
            table: "DnaSharedMatches",
            columns: new[] { "MatchAId", "MatchBId" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_DnaSharedMatches_MatchBId",
            table: "DnaSharedMatches",
            column: "MatchBId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DnaSharedMatches");
        migrationBuilder.DropTable(name: "DnaMatches");
    }
}
