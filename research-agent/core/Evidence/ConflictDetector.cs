using ResearchAgent.Core.Domain;

namespace ResearchAgent.Core.Evidence;

public sealed record DetectedConflict(Guid ClaimAId, Guid ClaimBId, string ClaimType, string Reason, string Severity);

public static class ConflictDetector
{
    public static IReadOnlyList<DetectedConflict> Detect(IEnumerable<ResearchClaim> claims)
    {
        var result = new List<DetectedConflict>();
        foreach (var group in claims.GroupBy(c => new { c.SubjectPersonId, Type = c.ClaimType.Trim().ToLowerInvariant() }))
        {
            var active = group.Where(c => c.Status != ClaimStatus.Rejected).ToList();
            for (var i = 0; i < active.Count; i++)
            for (var j = i + 1; j < active.Count; j++)
            {
                var a = active[i];
                var b = active[j];
                var av = Normalize(a.NormalizedValue ?? a.ProposedValue);
                var bv = Normalize(b.NormalizedValue ?? b.ProposedValue);
                if (av == bv) continue;
                result.Add(new(a.Id, b.Id, group.Key.Type,
                    $"Competing values '{a.ProposedValue}' and '{b.ProposedValue}' are recorded for the same claim type.",
                    IsMajor(group.Key.Type) ? "High" : "Review"));
            }
        }
        return result;
    }

    private static string Normalize(string value) => string.Join(' ', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    private static bool IsMajor(string type) => type.Contains("parent") || type.Contains("birth") || type.Contains("death") || type.Contains("spouse");
}
