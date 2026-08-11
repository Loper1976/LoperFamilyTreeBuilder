using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoperFamilyTreeBuilder.Data.Migrations;

[DbContext(typeof(FamilyTreeDbContext))]
[Migration("20260811203000_GedcomImportReadiness")]
public partial class GedcomImportReadiness : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "GedcomImportedNotes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ImportSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RecordPointer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GedcomImportedNotes", x => x.Id);
                table.ForeignKey("FK_GedcomImportedNotes_GedcomImportSessions_ImportSessionId", x => x.ImportSessionId,
                    "GedcomImportSessions", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_GedcomImportedNotes_People_PersonId", x => x.PersonId,
                    "People", "Id", onDelete: ReferentialAction.SetNull);
            });
        migrationBuilder.CreateIndex("IX_GedcomImportedNotes_ImportSessionId", "GedcomImportedNotes", "ImportSessionId");
        migrationBuilder.CreateIndex("IX_GedcomImportedNotes_PersonId", "GedcomImportedNotes", "PersonId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("GedcomImportedNotes");
}
