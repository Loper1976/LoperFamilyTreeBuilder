using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoperFamilyTreeBuilder.Data.Migrations;

public partial class AddRelationshipsTimelineMapsAndArchive : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FamilyUnions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Person1Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Person2Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UnionType = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                PlaceText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                SourceCitation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_FamilyUnions", x => x.Id));

        migrationBuilder.CreateTable(
            name: "LifeEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EventType = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                IsDateApproximate = table.Column<bool>(type: "bit", nullable: false),
                OriginalPlaceText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                SourceCitation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_LifeEvents", x => x.Id));

        migrationBuilder.CreateTable(
            name: "SourceRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                Citation = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                Repository = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                CallNumberOrUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_SourceRecords", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ArchiveItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                SourceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ItemType = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                OriginalPath = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                Sha256 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                CapturedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                OriginalPlaceText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                Caption = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Provenance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ArchiveItems", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_FamilyUnions_Person1Id", table: "FamilyUnions", column: "Person1Id");
        migrationBuilder.CreateIndex(name: "IX_FamilyUnions_Person2Id", table: "FamilyUnions", column: "Person2Id");
        migrationBuilder.CreateIndex(name: "IX_FamilyUnions_Person1Id_Person2Id", table: "FamilyUnions", columns: new[] { "Person1Id", "Person2Id" });
        migrationBuilder.CreateIndex(name: "IX_LifeEvents_PersonId_StartDate", table: "LifeEvents", columns: new[] { "PersonId", "StartDate" });
        migrationBuilder.CreateIndex(name: "IX_SourceRecords_PersonId", table: "SourceRecords", column: "PersonId");
        migrationBuilder.CreateIndex(name: "IX_ArchiveItems_PersonId", table: "ArchiveItems", column: "PersonId");
        migrationBuilder.CreateIndex(name: "IX_ArchiveItems_SourceRecordId", table: "ArchiveItems", column: "SourceRecordId");
        migrationBuilder.CreateIndex(name: "IX_ArchiveItems_ItemType", table: "ArchiveItems", column: "ItemType");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ArchiveItems");
        migrationBuilder.DropTable(name: "SourceRecords");
        migrationBuilder.DropTable(name: "LifeEvents");
        migrationBuilder.DropTable(name: "FamilyUnions");
    }
}
