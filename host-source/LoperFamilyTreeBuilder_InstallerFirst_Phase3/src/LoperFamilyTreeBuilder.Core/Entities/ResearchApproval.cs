namespace LoperFamilyTreeBuilder.Core.Entities;

public sealed class ResearchApproval
{
    private ResearchApproval() { }

    public ResearchApproval(Guid id, string entityType, Guid entityId, string decision,
        string actor, string reason, DateTimeOffset createdUtc)
    {
        Id = id;
        EntityType = entityType;
        EntityId = entityId;
        Decision = decision;
        Actor = actor;
        Reason = reason;
        CreatedUtc = createdUtc;
    }

    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Decision { get; private set; } = string.Empty;
    public string Actor { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; private set; }
}
