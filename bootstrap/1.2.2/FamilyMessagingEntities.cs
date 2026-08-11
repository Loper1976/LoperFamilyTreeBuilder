namespace LoperFamilyTreeBuilder.Core.Entities;

public sealed class FamilyMessage
{
    public const int AutomaticArchiveDays = 120;

    private FamilyMessage() { }

    public FamilyMessage(Guid senderUserId, Guid recipientUserId, string subject, string body)
    {
        if (senderUserId == Guid.Empty)
            throw new ArgumentException("Sender is required.", nameof(senderUserId));
        if (recipientUserId == Guid.Empty)
            throw new ArgumentException("Recipient is required.", nameof(recipientUserId));
        if (senderUserId == recipientUserId)
            throw new InvalidOperationException("A member cannot send a private message to the same account.");
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Message text is required.", nameof(body));

        Id = Guid.NewGuid();
        SenderUserId = senderUserId;
        RecipientUserId = recipientUserId;
        Subject = string.IsNullOrWhiteSpace(subject) ? "Family message" : subject.Trim();
        Body = body.Trim();
        ConversationKey = CreateConversationKey(senderUserId, recipientUserId);
        SentUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid SenderUserId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public string ConversationKey { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public DateTimeOffset SentUtc { get; private set; }
    public DateTimeOffset? ReadUtc { get; private set; }
    public DateTimeOffset? ArchivedUtc { get; private set; }
    public FamilyUser SenderUser { get; private set; } = null!;
    public FamilyUser RecipientUser { get; private set; } = null!;

    public bool IsArchived => ArchivedUtc.HasValue;
    public DateTimeOffset AutomaticArchiveUtc => SentUtc.AddDays(AutomaticArchiveDays);

    public void MarkRead()
    {
        ReadUtc ??= DateTimeOffset.UtcNow;
    }

    public bool ArchiveIfExpired(DateTimeOffset now)
    {
        if (ArchivedUtc.HasValue || now < AutomaticArchiveUtc)
            return false;

        ArchivedUtc = now;
        return true;
    }

    public static string CreateConversationKey(Guid firstUserId, Guid secondUserId)
    {
        var a = firstUserId.ToString("N");
        var b = secondUserId.ToString("N");
        return string.CompareOrdinal(a, b) <= 0 ? $"{a}:{b}" : $"{b}:{a}";
    }
}
