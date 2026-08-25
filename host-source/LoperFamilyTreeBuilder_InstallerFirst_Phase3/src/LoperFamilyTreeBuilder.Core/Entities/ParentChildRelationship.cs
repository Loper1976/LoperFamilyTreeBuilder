namespace LoperFamilyTreeBuilder.Core.Entities;

public sealed class ParentChildRelationship
{
    private ParentChildRelationship()
    {
    }

    public ParentChildRelationship(
        Guid parentPersonId,
        Guid childPersonId,
        ParentChildRelationshipType relationshipType)
    {
        if (parentPersonId == childPersonId)
        {
            throw new InvalidOperationException("A person cannot be their own parent.");
        }

        Id = Guid.NewGuid();
        ParentPersonId = parentPersonId;
        ChildPersonId = childPersonId;
        RelationshipType = relationshipType;
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid ParentPersonId { get; private set; }

    public Guid ChildPersonId { get; private set; }

    public ParentChildRelationshipType RelationshipType { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public Person ParentPerson { get; private set; } = null!;

    public Person ChildPerson { get; private set; } = null!;
}
