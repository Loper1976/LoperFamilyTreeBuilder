namespace LoperFamilyTreeBuilder.Core.Models;

public enum FamilyMessageMailbox
{
    Inbox = 1,
    Sent = 2,
    Archived = 3
}

public sealed record FamilyMessageMember(Guid UserId, string DisplayName, string Email);

public sealed record FamilyMessageListItem(
    Guid MessageId,
    Guid SenderUserId,
    string SenderName,
    Guid RecipientUserId,
    string RecipientName,
    string Subject,
    string Body,
    DateTimeOffset SentUtc,
    DateTimeOffset? ReadUtc,
    DateTimeOffset? ArchivedUtc)
{
    public bool IsArchived => ArchivedUtc.HasValue;
}

public sealed record FamilyConversationSummary(
    Guid OtherUserId,
    string OtherDisplayName,
    string OtherEmail,
    string Subject,
    string Preview,
    DateTimeOffset LastMessageUtc,
    int UnreadCount,
    bool ArchivedOnly);

public sealed class SendFamilyMessageRequest
{
    public Guid RecipientUserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
