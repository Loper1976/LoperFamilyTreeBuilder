using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoperFamilyTreeBuilder.Data.Migrations;

[DbContext(typeof(FamilyTreeDbContext))]
[Migration("20260823010000_AddResearchPromotion")]
public sealed class AddResearchPromotion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("ResearchApprovals", table => new
        {
            Id = table.Column<Guid>("uniqueidentifier", nullable: false),
            EntityType = table.Column<string>("nvarchar(100)", maxLength: 100, nullable: false),
            EntityId = table.Column<Guid>("uniqueidentifier", nullable: false),
            Decision = table.Column<string>("nvarchar(100)", maxLength: 100, nullable: false),
            Actor = table.Column<string>("nvarchar(250)", maxLength: 250, nullable: false),
            Reason = table.Column<string>("nvarchar(2000)", maxLength: 2000, nullable: false),
            CreatedUtc = table.Column<DateTimeOffset>("datetimeoffset", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_ResearchApprovals", x => x.Id));
        migrationBuilder.CreateIndex("IX_ResearchApprovals_EntityId_Decision", "ResearchApprovals", new[] { "EntityId", "Decision" });

        migrationBuilder.CreateTable("AcceptedFacts", table => new
        {
            Id = table.Column<Guid>("uniqueidentifier", nullable: false),
            PersonId = table.Column<Guid>("uniqueidentifier", nullable: false),
            ResearchClaimId = table.Column<Guid>("uniqueidentifier", nullable: false),
            ApprovalId = table.Column<Guid>("uniqueidentifier", nullable: false),
            FactType = table.Column<string>("nvarchar(100)", maxLength: 100, nullable: false),
            Value = table.Column<string>("nvarchar(2000)", maxLength: 2000, nullable: false),
            AcceptedBy = table.Column<string>("nvarchar(250)", maxLength: 250, nullable: false),
            AcceptedUtc = table.Column<DateTimeOffset>("datetimeoffset", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_AcceptedFacts", x => x.Id);
            table.ForeignKey("FK_AcceptedFacts_People_PersonId", x => x.PersonId, "People", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_AcceptedFacts_ResearchClaimId", "AcceptedFacts", "ResearchClaimId", unique: true);
        migrationBuilder.CreateIndex("IX_AcceptedFacts_PersonId_FactType", "AcceptedFacts", new[] { "PersonId", "FactType" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AcceptedFacts");
        migrationBuilder.DropTable("ResearchApprovals");
    }
}
