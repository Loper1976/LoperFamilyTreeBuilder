using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class FamilyMessagingService(IDbContextFactory<FamilyTreeDbContext> contextFactory)
{
    public async Task<IReadOnlyList<FamilyMessageMember>> GetRecipientsAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.FamilyUsers.AsNoTracking()
            .Where(x => x.Id != currentUserId && x.IsActive && !x.IsLocked)
            .OrderBy(x => x.DisplayName)
            .Select(x => new FamilyMessageMember(x.Id, x.DisplayName, x.Email))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        await ArchiveExpiredAsync(cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.FamilyMessages.AsNoTracking()
            .CountAsync(x => x.RecipientUserId == currentUserId && x.ReadUtc == null && x.ArchivedUtc == null, cancellationToken);
    }

    public async Task<IReadOnlyList<FamilyConversationSummary>> GetConversationsAsync(
        Guid currentUserId,
        FamilyMessageMailbox mailbox,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        await ArchiveExpiredAsync(cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var queryText = (search ?? string.Empty).Trim();

        var messages = db.FamilyMessages.AsNoTracking()
            .Where(x => x.SenderUserId == currentUserId || x.RecipientUserId == currentUserId);

        messages = mailbox switch
        {
            FamilyMessageMailbox.Inbox => messages.Where(x => x.RecipientUserId == currentUserId && x.ArchivedUtc == null),
            FamilyMessageMailbox.Sent => messages.Where(x => x.SenderUserId == currentUserId && x.ArchivedUtc == null),
            FamilyMessageMailbox.Archived => messages.Where(x => x.ArchivedUtc != null),
            _ => messages
        };

        if (!string.IsNullOrWhiteSpace(queryText))
        {
            messages = messages.Where(x =>
                x.Subject.Contains(queryText) ||
                x.Body.Contains(queryText) ||
                x.SenderUser.DisplayName.Contains(queryText) ||
                x.RecipientUser.DisplayName.Contains(queryText));
        }

        var rows = await messages
            .OrderByDescending(x => x.SentUtc)
            .Select(x => new
            {
                Message = x,
                SenderName = x.SenderUser.DisplayName,
                SenderEmail = x.SenderUser.Email,
                RecipientName = x.RecipientUser.DisplayName,
                RecipientEmail = x.RecipientUser.Email
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.Message.ConversationKey)
            .Select(group =>
            {
                var last = group.OrderByDescending(x => x.Message.SentUtc).First();
                var otherId = last.Message.SenderUserId == currentUserId ? last.Message.RecipientUserId : last.Message.SenderUserId;
                var otherName = last.Message.SenderUserId == currentUserId ? last.RecipientName : last.SenderName;
                var otherEmail = last.Message.SenderUserId == currentUserId ? last.RecipientEmail : last.SenderEmail;
                var preview = last.Message.Body.Length <= 96 ? last.Message.Body : last.Message.Body[..96] + "…";
                var unread = group.Count(x => x.Message.RecipientUserId == currentUserId && x.Message.ReadUtc == null && x.Message.ArchivedUtc == null);
                return new FamilyConversationSummary(otherId, otherName, otherEmail, last.Message.Subject, preview, last.Message.SentUtc, unread, group.All(x => x.Message.ArchivedUtc != null));
            })
            .OrderByDescending(x => x.LastMessageUtc)
            .ToList();
    }

    public async Task<IReadOnlyList<FamilyMessageListItem>> GetConversationAsync(
        Guid currentUserId,
        Guid otherUserId,
        bool includeArchived = true,
        CancellationToken cancellationToken = default)
    {
        await ArchiveExpiredAsync(cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var key = FamilyMessage.CreateConversationKey(currentUserId, otherUserId);
        var query = db.FamilyMessages
            .Where(x => x.ConversationKey == key && (x.SenderUserId == currentUserId || x.RecipientUserId == currentUserId));

        if (!includeArchived)
            query = query.Where(x => x.ArchivedUtc == null);

        var unread = await query.Where(x => x.RecipientUserId == currentUserId && x.ReadUtc == null).ToListAsync(cancellationToken);
        foreach (var message in unread)
            message.MarkRead();
        if (unread.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        return await query.AsNoTracking()
            .OrderBy(x => x.SentUtc)
            .Select(x => new FamilyMessageListItem(
                x.Id,
                x.SenderUserId,
                x.SenderUser.DisplayName,
                x.RecipientUserId,
                x.RecipientUser.DisplayName,
                x.Subject,
                x.Body,
                x.SentUtc,
                x.ReadUtc,
                x.ArchivedUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> SendAsync(
        Guid senderUserId,
        SendFamilyMessageRequest request,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var senderExists = await db.FamilyUsers.AnyAsync(x => x.Id == senderUserId && x.IsActive && !x.IsLocked, cancellationToken);
        if (!senderExists)
            throw new InvalidOperationException("The sending member account is not active.");

        var recipient = await db.FamilyUsers.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == request.RecipientUserId && x.IsActive && !x.IsLocked,
            cancellationToken)
            ?? throw new InvalidOperationException("The selected recipient is not an active family member.");

        var message = new FamilyMessage(senderUserId, request.RecipientUserId, request.Subject, request.Body);
        db.FamilyMessages.Add(message);
        db.AuditEvents.Add(new AuditEvent(
            "Send private family message",
            nameof(FamilyMessage),
            message.Id.ToString(),
            actor,
            $"Private member message sent to account {recipient.Id}. Message content is excluded from the audit log.",
            source: "Family Messaging"));
        await db.SaveChangesAsync(cancellationToken);
        return message.Id;
    }

    public async Task<int> ArchiveExpiredAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-FamilyMessage.AutomaticArchiveDays);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var expired = await db.FamilyMessages
            .Where(x => x.ArchivedUtc == null && x.SentUtc <= cutoff)
            .ToListAsync(cancellationToken);

        var changed = 0;
        var now = DateTimeOffset.UtcNow;
        foreach (var message in expired)
            if (message.ArchiveIfExpired(now))
                changed++;

        if (changed > 0)
            await db.SaveChangesAsync(cancellationToken);
        return changed;
    }
}
