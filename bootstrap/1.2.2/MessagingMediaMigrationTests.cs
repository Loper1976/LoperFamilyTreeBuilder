using LoperFamilyTreeBuilder.Core.Entities;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class MessagingMediaMigrationTests
{
    [Fact]
    public void Family_message_archives_after_120_days_without_deleting_content()
    {
        var message=new FamilyMessage(Guid.NewGuid(),Guid.NewGuid(),"Family history","Preserve this conversation.");
        Assert.False(message.IsArchived);
        Assert.False(message.ArchiveIfExpired(message.SentUtc.AddDays(119)));
        Assert.True(message.ArchiveIfExpired(message.SentUtc.AddDays(121)));
        Assert.True(message.IsArchived);
        Assert.Equal("Preserve this conversation.",message.Body);
    }

    [Fact]
    public void Conversation_key_is_stable_regardless_of_sender_direction()
    {
        var first=Guid.NewGuid();var second=Guid.NewGuid();
        Assert.Equal(FamilyMessage.CreateConversationKey(first,second),FamilyMessage.CreateConversationKey(second,first));
    }

    [Fact]
    public void Messaging_does_not_modify_legacy_numbers()
    {
        var person=new Person("Robert","Loper");person.AddLegacyNumber("21313.00");
        _=new FamilyMessage(Guid.NewGuid(),Guid.NewGuid(),"Archive note","Reference-only family discussion.");
        Assert.Equal("21313.00",person.Identifiers.Single(x=>x.IdentifierType==PersonIdentifierType.LegacyNumber).Value);
    }

    [Fact]
    public void Media_migration_status_separates_duplicates_from_review_items()
    {
        var session=new MediaMigrationSession(MediaMigrationSourceType.FamilyTreeMaker,@"D:\FTM Media","Owner");
        var item=new MediaMigrationItem(session.Id,"Photos/family.jpg","family.jpg",100,new string('a',64),ArchiveMediaType.Photo,"image/jpeg","{}",MediaMigrationItemStatus.ReadyToImport);
        var existing=Guid.NewGuid();item.MarkDuplicate(existing);
        Assert.Equal(MediaMigrationItemStatus.ExactDuplicate,item.Status);
        Assert.Equal(existing,item.ExistingMediaFileId);
    }
}
