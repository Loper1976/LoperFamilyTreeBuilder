using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoperFamilyTreeBuilder.Data.Migrations;

[DbContext(typeof(FamilyTreeDbContext))]
[Migration("20260808020000_InitialCoreGenealogy")]
public partial class InitialCoreGenealogy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FamilyBranches",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                ShortCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                RootPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                NumberingPolicyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FamilyBranches", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "People",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                GivenName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                MiddleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Surname = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Suffix = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                DeathDate = table.Column<DateOnly>(type: "date", nullable: true),
                IsLiving = table.Column<bool>(type: "bit", nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_People", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "BranchMemberships",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FamilyBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BranchMemberships", x => x.Id);
                table.ForeignKey(
                    name: "FK_BranchMemberships_FamilyBranches_FamilyBranchId",
                    column: x => x.FamilyBranchId,
                    principalTable: "FamilyBranches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_BranchMemberships_People_PersonId",
                    column: x => x.PersonId,
                    principalTable: "People",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ParentChildRelationships",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ParentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ChildPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RelationshipType = table.Column<int>(type: "int", nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ParentChildRelationships", x => x.Id);
                table.ForeignKey(
                    name: "FK_ParentChildRelationships_People_ChildPersonId",
                    column: x => x.ChildPersonId,
                    principalTable: "People",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_ParentChildRelationships_People_ParentPersonId",
                    column: x => x.ParentPersonId,
                    principalTable: "People",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "PersonIdentifiers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IdentifierType = table.Column<int>(type: "int", nullable: false),
                Value = table.Column<string>(
                    type: "nvarchar(255)",
                    maxLength: 255,
                    nullable: false,
                    collation: "Latin1_General_100_BIN2"),
                IsProtected = table.Column<bool>(type: "bit", nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PersonIdentifiers", x => x.Id);
                table.ForeignKey(
                    name: "FK_PersonIdentifiers_People_PersonId",
                    column: x => x.PersonId,
                    principalTable: "People",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BranchMemberships_FamilyBranchId",
            table: "BranchMemberships",
            column: "FamilyBranchId");

        migrationBuilder.CreateIndex(
            name: "IX_BranchMemberships_PersonId_FamilyBranchId",
            table: "BranchMemberships",
            columns: new[] { "PersonId", "FamilyBranchId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_FamilyBranches_Name",
            table: "FamilyBranches",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_FamilyBranches_ShortCode",
            table: "FamilyBranches",
            column: "ShortCode");

        migrationBuilder.CreateIndex(
            name: "IX_ParentChildRelationships_ChildPersonId",
            table: "ParentChildRelationships",
            column: "ChildPersonId");

        migrationBuilder.CreateIndex(
            name: "IX_ParentChildRelationships_ParentPersonId",
            table: "ParentChildRelationships",
            column: "ParentPersonId");

        migrationBuilder.CreateIndex(
            name: "IX_ParentChildRelationships_ParentPersonId_ChildPersonId_RelationshipType",
            table: "ParentChildRelationships",
            columns: new[] { "ParentPersonId", "ChildPersonId", "RelationshipType" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_People_GivenName",
            table: "People",
            column: "GivenName");

        migrationBuilder.CreateIndex(
            name: "IX_People_IsLiving",
            table: "People",
            column: "IsLiving");

        migrationBuilder.CreateIndex(
            name: "IX_People_Surname",
            table: "People",
            column: "Surname");

        migrationBuilder.CreateIndex(
            name: "IX_PersonIdentifiers_IdentifierType_Value",
            table: "PersonIdentifiers",
            columns: new[] { "IdentifierType", "Value" });

        migrationBuilder.CreateIndex(
            name: "IX_PersonIdentifiers_PersonId",
            table: "PersonIdentifiers",
            column: "PersonId");

        migrationBuilder.CreateIndex(
            name: "IX_PersonIdentifiers_Value",
            table: "PersonIdentifiers",
            column: "Value");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BranchMemberships");
        migrationBuilder.DropTable(name: "ParentChildRelationships");
        migrationBuilder.DropTable(name: "PersonIdentifiers");
        migrationBuilder.DropTable(name: "FamilyBranches");
        migrationBuilder.DropTable(name: "People");
    }
}
