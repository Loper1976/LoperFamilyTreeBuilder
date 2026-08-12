using LoperFamilyTreeBuilder.Core.Models;

namespace LoperFamilyTreeBuilder.Core.Genealogy;

public sealed class DnaClusterEngine
{
    public DnaClusterResult Build(
        IReadOnlyCollection<DnaMatchSnapshot> matches,
        IReadOnlyCollection<DnaSharedMatchSnapshot> sharedMatches)
    {
        var matchById = matches
            .GroupBy(match => match.Id)
            .ToDictionary(group => group.Key, group => group.First());

        var adjacency = matchById.Keys.ToDictionary(id => id, _ => new HashSet<Guid>());
        var evidenceEdges = new HashSet<(Guid A, Guid B)>();

        foreach (var edge in sharedMatches)
        {
            if (edge.MatchAId == edge.MatchBId ||
                !matchById.ContainsKey(edge.MatchAId) ||
                !matchById.ContainsKey(edge.MatchBId))
            {
                continue;
            }

            var canonical = edge.MatchAId.CompareTo(edge.MatchBId) < 0
                ? (edge.MatchAId, edge.MatchBId)
                : (edge.MatchBId, edge.MatchAId);

            if (!evidenceEdges.Add(canonical))
                continue;

            adjacency[canonical.Item1].Add(canonical.Item2);
            adjacency[canonical.Item2].Add(canonical.Item1);
        }

        var visited = new HashSet<Guid>();
        var components = new List<List<Guid>>();

        foreach (var start in matchById.Values
                     .OrderByDescending(match => match.TotalCentimorgans)
                     .ThenBy(match => match.DisplayName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(match => match.Id)
                     .Select(match => match.Id))
        {
            if (!visited.Add(start))
                continue;

            var component = new List<Guid>();
            var queue = new Queue<Guid>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                component.Add(current);

                foreach (var neighbor in adjacency[current].OrderBy(id => id))
                {
                    if (visited.Add(neighbor))
                        queue.Enqueue(neighbor);
                }
            }

            components.Add(component);
        }

        var clusteredComponents = components
            .Where(component => component.Count >= 2)
            .OrderByDescending(component => component.Count)
            .ThenByDescending(component => component.Sum(id => matchById[id].TotalCentimorgans))
            .ToList();

        var clusters = new List<DnaClusterGroup>();
        for (var index = 0; index < clusteredComponents.Count; index++)
        {
            var component = clusteredComponents[index];
            var componentSet = component.ToHashSet();
            var edgeCount = evidenceEdges.Count(edge =>
                componentSet.Contains(edge.A) && componentSet.Contains(edge.B));
            var possibleEdgeCount = component.Count * (component.Count - 1) / 2m;
            var density = possibleEdgeCount == 0m
                ? 0m
                : Math.Round(edgeCount / possibleEdgeCount, 4);

            var members = component
                .Select(id => new DnaClusterMember(
                    id,
                    matchById[id].DisplayName,
                    matchById[id].TotalCentimorgans,
                    adjacency[id].Count(neighbor => componentSet.Contains(neighbor)),
                    matchById[id].ManualAncestralLineLabel))
                .OrderByDescending(member => member.SharedConnections)
                .ThenByDescending(member => member.TotalCentimorgans)
                .ThenBy(member => member.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var reviewedLabels = members
                .Select(member => member.ManualAncestralLineLabel)
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var displayLabel = reviewedLabels.Count == 1 &&
                               members.All(member => !string.IsNullOrWhiteSpace(member.ManualAncestralLineLabel))
                ? $"Reviewed line: {reviewedLabels[0]}"
                : $"Cluster {index + 1}";

            clusters.Add(new DnaClusterGroup(
                index + 1,
                displayLabel,
                members.Count,
                edgeCount,
                density,
                members));
        }

        var unclustered = components
            .Where(component => component.Count == 1)
            .Select(component => component[0])
            .Select(id => new DnaClusterMember(
                id,
                matchById[id].DisplayName,
                matchById[id].TotalCentimorgans,
                0,
                matchById[id].ManualAncestralLineLabel))
            .OrderByDescending(member => member.TotalCentimorgans)
            .ThenBy(member => member.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new DnaClusterResult(clusters, unclustered);
    }
}
