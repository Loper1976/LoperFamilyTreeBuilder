namespace LoperFamilyTreeBuilder.Core.Entities;

public sealed class AuditEvent
{
    private AuditEvent()
    {
    }

    public AuditEvent(
        string action,
        string entityType,
        string entityId,
        string actor,
        string summary,
        string? previousValueJson = null,
        string? newValueJson = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Audit action is required.", nameof(action));

        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Audit entity type is required.", nameof(entityType));

        if (string.IsNullOrWhiteSpace(entityId))
            throw new ArgumentException("Audit entity ID is required.", nameof(entityId));

        Id = Guid.NewGuid();
        OccurredUtc = DateTimeOffset.UtcNow;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Actor = string.IsNullOrWhiteSpace(actor) ? "Local Application" : actor;
        Summary = summary ?? string.Empty;
        PreviousValueJson = previousValueJson;
        NewValueJson = newValueJson;
    }

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredUtc { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public string Actor { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;

    public string? PreviousValueJson { get; private set; }

    public string? NewValueJson { get; private set; }
}
