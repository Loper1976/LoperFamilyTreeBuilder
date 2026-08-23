using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoperFamilyTreeBuilder.Data.Migrations;

[DbContext(typeof(FamilyTreeDbContext))]
[Migration("20260808030000_AddAuditTrail")]
public partial class AddAuditTrail : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: false),

                OccurredUtc = table.Column<DateTimeOffset>(
                    type: "datetimeoffset",
                    nullable: false),

                Action = table.Column<string>(
                    type: "nvarchar(100)",
                    maxLength: 100,
                    nullable: false),

                EntityType = table.Column<string>(
                    type: "nvarchar(150)",
                    maxLength: 150,
                    nullable: false),

                EntityId = table.Column<string>(
                    type: "nvarchar(100)",
                    maxLength: 100,
                    nullable: false),

                Actor = table.Column<string>(
                    type: "nvarchar(250)",
                    maxLength: 250,
                    nullable: false),

                Summary = table.Column<string>(
                    type: "nvarchar(2000)",
                    maxLength: 2000,
                    nullable: false),

                PreviousValueJson = table.Column<string>(
                    type: "nvarchar(max)",
                    nullable: true),

                NewValueJson = table.Column<string>(
                    type: "nvarchar(max)",
                    nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditEvents", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditEvents_Action",
            table: "AuditEvents",
            column: "Action");

        migrationBuilder.CreateIndex(
            name: "IX_AuditEvents_EntityId",
            table: "AuditEvents",
            column: "EntityId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditEvents_EntityType",
            table: "AuditEvents",
            column: "EntityType");

        migrationBuilder.CreateIndex(
            name: "IX_AuditEvents_OccurredUtc",
            table: "AuditEvents",
            column: "OccurredUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AuditEvents");
    }
}
