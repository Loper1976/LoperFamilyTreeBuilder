using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoperFamilyTreeBuilder.Data.Migrations;

[DbContext(typeof(FamilyTreeDbContext))]
[Migration("20260812050000_AddHandwritingTranscriptionWorkflow")]
public partial class AddHandwritingTranscriptionWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "HandwritingTranscriptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                DocumentTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                ArchiveRelativePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                OriginalImageHashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                SourceCitation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                Visibility = table.Column<int>(type: "int", nullable: false),
                ProviderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                ModelName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                MachineDraft = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ReviewedTranscript = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ApprovedTranscript = table.Column<string>(type: "nvarchar(max)", nullable: false),
                FailureMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ApprovedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HandwritingTranscriptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_HandwritingTranscriptions_People_PersonId",
                    column: x => x.PersonId,
                    principalTable: "People",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_HandwritingTranscriptions_ModifiedUtc",
            table: "HandwritingTranscriptions",
            column: "ModifiedUtc");

        migrationBuilder.CreateIndex(
            name: "IX_HandwritingTranscriptions_PersonId",
            table: "HandwritingTranscriptions",
            column: "PersonId");

        migrationBuilder.CreateIndex(
            name: "IX_HandwritingTranscriptions_Status",
            table: "HandwritingTranscriptions",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_HandwritingTranscriptions_Visibility",
            table: "HandwritingTranscriptions",
            column: "Visibility");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "HandwritingTranscriptions");
    }
}
