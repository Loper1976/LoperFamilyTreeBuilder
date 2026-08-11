using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoperFamilyTreeBuilder.Data.Migrations;

public partial class MediaMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name:"MediaMigrationSessions",columns:table=>new{Id=table.Column<Guid>(type:"uniqueidentifier",nullable:false),SessionCode=table.Column<string>(type:"nvarchar(64)",maxLength:64,nullable:false),SourceType=table.Column<int>(type:"int",nullable:false),SourceRootPath=table.Column<string>(type:"nvarchar(2000)",maxLength:2000,nullable:false),StartedBy=table.Column<string>(type:"nvarchar(500)",maxLength:500,nullable:false),Status=table.Column<int>(type:"int",nullable:false),FilesScanned=table.Column<int>(type:"int",nullable:false),ReadyToImportCount=table.Column<int>(type:"int",nullable:false),ExactDuplicateCount=table.Column<int>(type:"int",nullable:false),NeedsReviewCount=table.Column<int>(type:"int",nullable:false),ImportedCount=table.Column<int>(type:"int",nullable:false),FailedCount=table.Column<int>(type:"int",nullable:false),CreatedUtc=table.Column<DateTimeOffset>(type:"datetimeoffset",nullable:false),ModifiedUtc=table.Column<DateTimeOffset>(type:"datetimeoffset",nullable:false),CompletedUtc=table.Column<DateTimeOffset>(type:"datetimeoffset",nullable:true)},constraints:table=>table.PrimaryKey("PK_MediaMigrationSessions",x=>x.Id));
        migrationBuilder.CreateTable(name:"MediaMigrationItems",columns:table=>new{Id=table.Column<Guid>(type:"uniqueidentifier",nullable:false),SessionId=table.Column<Guid>(type:"uniqueidentifier",nullable:false),SourceRelativePath=table.Column<string>(type:"nvarchar(2000)",maxLength:2000,nullable:false),OriginalFileName=table.Column<string>(type:"nvarchar(1000)",maxLength:1000,nullable:false),FileSizeBytes=table.Column<long>(type:"bigint",nullable:false),Sha256=table.Column<string>(type:"nvarchar(64)",maxLength:64,nullable:false),MediaType=table.Column<int>(type:"int",nullable:false),MimeType=table.Column<string>(type:"nvarchar(250)",maxLength:250,nullable:false),CapturedMetadataJson=table.Column<string>(type:"nvarchar(max)",nullable:false),Status=table.Column<int>(type:"int",nullable:false),ExistingMediaFileId=table.Column<Guid>(type:"uniqueidentifier",nullable:true),SuggestedPersonId=table.Column<Guid>(type:"uniqueidentifier",nullable:true),SuggestedMatchReason=table.Column<string>(type:"nvarchar(1000)",maxLength:1000,nullable:false),ImportedMediaFileId=table.Column<Guid>(type:"uniqueidentifier",nullable:true),ReviewNote=table.Column<string>(type:"nvarchar(2000)",maxLength:2000,nullable:false),CreatedUtc=table.Column<DateTimeOffset>(type:"datetimeoffset",nullable:false),ModifiedUtc=table.Column<DateTimeOffset>(type:"datetimeoffset",nullable:false)},constraints:table=>{table.PrimaryKey("PK_MediaMigrationItems",x=>x.Id);table.ForeignKey("FK_MediaMigrationItems_MediaMigrationSessions_SessionId",x=>x.SessionId,"MediaMigrationSessions","Id",onDelete:ReferentialAction.Cascade);});
        migrationBuilder.CreateIndex("IX_MediaMigrationSessions_SessionCode","MediaMigrationSessions","SessionCode",unique:true);
        migrationBuilder.CreateIndex("IX_MediaMigrationSessions_CreatedUtc","MediaMigrationSessions","CreatedUtc");
        migrationBuilder.CreateIndex("IX_MediaMigrationItems_SessionId_Status","MediaMigrationItems",new[]{"SessionId","Status"});
        migrationBuilder.CreateIndex("IX_MediaMigrationItems_Sha256","MediaMigrationItems","Sha256");
    }
    protected override void Down(MigrationBuilder migrationBuilder){migrationBuilder.DropTable("MediaMigrationItems");migrationBuilder.DropTable("MediaMigrationSessions");}
}
