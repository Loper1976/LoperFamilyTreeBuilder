using LoperFamilyTreeBuilder.Core.Entities;

namespace LoperFamilyTreeBuilder.Core.Models;

public sealed record FamilyTreeRelationshipSnapshot(
    Guid ParentPersonId,
    Guid ChildPersonId,
    ParentChildRelationshipType RelationshipType);
