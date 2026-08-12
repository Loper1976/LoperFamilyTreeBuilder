using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;

namespace LoperFamilyTreeBuilder.Core.Genealogy;

public sealed class FamilyTreeGraphBuilder
{
    public FamilyTreeView Build(
        Guid rootPersonId,
        FamilyTreeDirection direction,
        int maxDepth,
        IReadOnlyCollection<FamilyTreePersonSnapshot> people,
        IReadOnlyCollection<FamilyTreeRelationshipSnapshot> relationships,
        int maxNodes = 500)
    {
        ArgumentNullException.ThrowIfNull(people);
        ArgumentNullException.ThrowIfNull(relationships);

        maxDepth = Math.Clamp(maxDepth, 1, 8);
        maxNodes = Math.Clamp(maxNodes, 25, 5000);

        var peopleById = people.ToDictionary(person => person.Id);
        if (!peopleById.TryGetValue(rootPersonId, out var rootPerson))
        {
            throw new InvalidOperationException("The selected root person does not exist in the family archive.");
        }

        var outgoing = relationships
            .GroupBy(relationship => relationship.ParentPersonId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var incoming = relationships
            .GroupBy(relationship => relationship.ChildPersonId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var count = 0;
        var limitReached = false;

        FamilyTreeNode BuildNode(
            FamilyTreePersonSnapshot person,
            ParentChildRelationshipType? relationshipType,
            int generation,
            HashSet<Guid> path)
        {
            count++;
            if (generation >= maxDepth || count >= maxNodes)
            {
                if (count >= maxNodes) limitReached = true;
                return new FamilyTreeNode(person, relationshipType, generation, false, []);
            }

            IEnumerable<FamilyTreeRelationshipSnapshot> nextRelationships =
                direction == FamilyTreeDirection.Descendants
                    ? outgoing.GetValueOrDefault(person.Id) ?? []
                    : incoming.GetValueOrDefault(person.Id) ?? [];

            var next = new List<FamilyTreeNode>();
            foreach (var relationship in nextRelationships
                .OrderBy(item => item.RelationshipType)
                .ThenBy(item => item.ParentPersonId)
                .ThenBy(item => item.ChildPersonId))
            {
                if (count >= maxNodes)
                {
                    limitReached = true;
                    break;
                }

                var nextPersonId = direction == FamilyTreeDirection.Descendants
                    ? relationship.ChildPersonId
                    : relationship.ParentPersonId;
                if (!peopleById.TryGetValue(nextPersonId, out var nextPerson)) continue;

                if (path.Contains(nextPersonId))
                {
                    count++;
                    next.Add(new FamilyTreeNode(
                        nextPerson,
                        relationship.RelationshipType,
                        generation + 1,
                        true,
                        []));
                    continue;
                }

                var childPath = new HashSet<Guid>(path) { nextPersonId };
                next.Add(BuildNode(
                    nextPerson,
                    relationship.RelationshipType,
                    generation + 1,
                    childPath));
            }

            next = next
                .OrderBy(node => node.Person.BirthDate ?? DateOnly.MaxValue)
                .ThenBy(node => node.Person.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new FamilyTreeNode(person, relationshipType, generation, false, next);
        }

        var root = BuildNode(rootPerson, null, 0, new HashSet<Guid> { rootPersonId });
        return new FamilyTreeView(direction, maxDepth, count, limitReached, root);
    }
}
