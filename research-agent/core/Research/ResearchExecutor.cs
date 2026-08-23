namespace ResearchAgent.Core.Research;

public sealed record ResearchTaskExecution(ResearchTask Task, IReadOnlyList<SourceSearchResult> Results, string Status, string? StopReason = null);
public sealed record ResearchRunResult(Guid PlanId, IReadOnlyList<ResearchTaskExecution> Executions, ResearchRunState FinalState, string StopReason);

public sealed class ResearchExecutor(IEnumerable<ISourceSearchProvider> providers)
{
    private readonly ISourceSearchProvider[] _providers = providers.ToArray();

    public async Task<ResearchRunResult> ExecuteAsync(ResearchPlan plan, PersonResearchProfile person,
        ResearchBudget budget, Domain.PrivacyClass privacy, CancellationToken cancellationToken = default)
    {
        var executions = new List<ResearchTaskExecution>();
        var state = new ResearchRunState(0, 0, 0m, 0);
        var stopReason = "Plan exhausted.";

        foreach (var task in plan.Tasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ResearchExecutionPolicy.CanContinue(budget, state, out stopReason)) break;

            var query = SearchQueryFactory.FromTask(person, task, privacy);
            var supported = _providers.Where(p => p.Supports(query)).ToArray();
            if (supported.Length == 0)
            {
                executions.Add(new(task, [], "Blocked", "No configured source provider supports this task."));
                state = state with { TasksAttempted = state.TasksAttempted + 1 };
                continue;
            }

            var results = new List<SourceSearchResult>();
            foreach (var provider in supported)
            {
                try { results.AddRange(await provider.SearchAsync(query, cancellationToken)); }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (Exception ex) { executions.Add(new(task, [], "ProviderError", $"{provider.Name}: {ex.Message}")); }
            }
            var ordered = results.OrderByDescending(r => r.MatchHint).ToArray();
            executions.Add(new(task, ordered, ordered.Length == 0 ? "NoResults" : "CandidatesFound"));
            state = state with { TasksAttempted = state.TasksAttempted + 1 };
        }

        return new(plan.Id, executions, state, stopReason);
    }
}
