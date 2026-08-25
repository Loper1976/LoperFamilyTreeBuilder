namespace ResearchAgent.Core.Research;

public enum ResearchTaskStatus { Open, InProgress, Blocked, Completed, Rejected }

public sealed record ResearchTask(
    Guid Id, Guid? SubjectPersonId, Guid? RelatedClaimId, string Question,
    string TaskType, decimal EvidentiaryImportance, decimal ExpectedInformationGain,
    decimal ExpectedEvidenceStrength, decimal Availability, decimal EstimatedCost,
    ResearchTaskStatus Status, DateTimeOffset CreatedUtc, DateTimeOffset? CompletedUtc = null);

public sealed record PrioritizedResearchTask(ResearchTask Task, decimal PriorityScore, string Explanation);

public static class ResearchPriorityEngine
{
    public static PrioritizedResearchTask Prioritize(ResearchTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        var importance = Clamp(task.EvidentiaryImportance);
        var information = Clamp(task.ExpectedInformationGain);
        var strength = Clamp(task.ExpectedEvidenceStrength);
        var availability = Clamp(task.Availability);
        var costPenalty = Clamp(task.EstimatedCost);

        var score = (importance * .30m) + (information * .28m) + (strength * .24m) +
                    (availability * .18m) - (costPenalty * .12m);
        score = Math.Clamp(score, 0m, 100m);

        var explanation = $"importance={importance:0}; informationGain={information:0}; " +
                          $"evidenceStrength={strength:0}; availability={availability:0}; costPenalty={costPenalty:0}";
        return new(task, score, explanation);
    }

    public static IReadOnlyList<PrioritizedResearchTask> Rank(IEnumerable<ResearchTask> tasks) =>
        tasks.Where(t => t.Status == ResearchTaskStatus.Open)
             .Select(Prioritize)
             .OrderByDescending(x => x.PriorityScore)
             .ThenBy(x => x.Task.CreatedUtc)
             .ToArray();

    private static decimal Clamp(decimal value) => Math.Clamp(value, 0m, 100m);
}
