using ResearchAgent.Core.Domain;

namespace ResearchAgent.Core.Research;

public sealed record PersonResearchProfile(Guid PersonId, string DisplayName, DateOnly? BirthDate, string? BirthPlace,
    DateOnly? DeathDate, string? DeathPlace, bool ParentsResolved, bool SpouseResolved,
    IReadOnlyList<string> KnownSourceTypes, IReadOnlyList<string> OpenConflictTypes);

public sealed record ResearchPlan(Guid Id, Guid PersonId, DateTimeOffset CreatedUtc,
    IReadOnlyList<ResearchTask> Tasks, string StopRule);

public static class AutonomousResearchPlanner
{
    public static ResearchPlan Build(PersonResearchProfile p)
    {
        var now = DateTimeOffset.UtcNow;
        var tasks = new List<ResearchTask>();
        void Add(string q, string type, decimal importance, decimal info, decimal strength, decimal availability, decimal cost) =>
            tasks.Add(new(Guid.NewGuid(), p.PersonId, null, q, type, importance, info, strength, availability, cost, ResearchTaskStatus.Open, now));

        if (p.BirthDate is null || string.IsNullOrWhiteSpace(p.BirthPlace))
            Add($"Establish birth date and place for {p.DisplayName}.", "vital-record", 95, 90, 95, 65, 15);
        if (!p.ParentsResolved)
            Add($"Identify and document the parents of {p.DisplayName}.", "parentage", 100, 100, 95, 70, 20);
        if (!p.SpouseResolved)
            Add($"Identify and document spouse or spouses of {p.DisplayName}.", "marriage", 80, 75, 90, 75, 15);
        if (!p.KnownSourceTypes.Contains("census", StringComparer.OrdinalIgnoreCase))
            Add($"Locate census or household records for {p.DisplayName}.", "census", 85, 95, 80, 90, 5);
        if (!p.KnownSourceTypes.Contains("death", StringComparer.OrdinalIgnoreCase) && p.DeathDate is not null)
            Add($"Locate a death record or equivalent contemporary record for {p.DisplayName}.", "death", 90, 80, 90, 75, 10);
        if (!p.KnownSourceTypes.Contains("obituary", StringComparer.OrdinalIgnoreCase) && p.DeathDate is not null)
            Add($"Search newspapers and obituaries for {p.DisplayName}.", "newspaper", 65, 75, 65, 70, 15);
        if (!p.KnownSourceTypes.Contains("probate", StringComparer.OrdinalIgnoreCase) && p.DeathDate is not null)
            Add($"Search probate and estate records for {p.DisplayName}.", "probate", 75, 90, 85, 45, 25);
        foreach (var conflict in p.OpenConflictTypes.Distinct(StringComparer.OrdinalIgnoreCase))
            Add($"Resolve the {conflict} conflict for {p.DisplayName} with independent evidence.", "conflict-resolution", 100, 95, 95, 60, 20);

        return new(Guid.NewGuid(), p.PersonId, now, ResearchPriorityEngine.Rank(tasks).Select(x => x.Task).ToArray(),
            "Stop when no open task can be advanced with permitted sources, when the configured research budget is reached, or when human approval is required.");
    }
}
