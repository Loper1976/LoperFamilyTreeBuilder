using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoperFamilyTreeBuilder.Data.Migrations;

public partial class FamilyMessaging : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FamilyMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SenderUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ConversationKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                SentUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ReadUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ArchivedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FamilyMessages", x => x.Id);
                table.ForeignKey("FK_FamilyMessages_FamilyUsers_RecipientUserId", x => x.RecipientUserId, "FamilyUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_FamilyMessages_FamilyUsers_SenderUserId", x => x.SenderUserId, "FamilyUsers", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_FamilyMessages_ConversationKey_SentUtc", "FamilyMessages", new[] { "ConversationKey", "SentUtc" });
        migrationBuilder.CreateIndex("IX_FamilyMessages_RecipientUserId_ArchivedUtc_SentUtc", "FamilyMessages", new[] { "RecipientUserId", "ArchivedUtc", "SentUtc" });
        migrationBuilder.CreateIndex("IX_FamilyMessages_SenderUserId_ArchivedUtc_SentUtc", "FamilyMessages", new[] { "SenderUserId", "ArchivedUtc", "SentUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("FamilyMessages");
}
