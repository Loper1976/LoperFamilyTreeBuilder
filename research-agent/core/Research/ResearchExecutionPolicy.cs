namespace ResearchAgent.Core.Research;

public sealed record ResearchBudget(int MaxTasksPerRun = 20, int MaxNewPeoplePerRun = 5,
    decimal MaxEstimatedCost = 0m, int MaxDepthExpansion = 1);

public sealed record ResearchRunState(int TasksAttempted, int NewPeopleProposed, decimal EstimatedCostUsed, int DepthExpanded);

public static class ResearchExecutionPolicy
{
    public static bool CanContinue(ResearchBudget budget, ResearchRunState state, out string reason)
    {
        if (state.TasksAttempted >= budget.MaxTasksPerRun) { reason = "Task limit reached."; return false; }
        if (state.NewPeopleProposed >= budget.MaxNewPeoplePerRun) { reason = "New-person proposal limit reached."; return false; }
        if (state.EstimatedCostUsed >= budget.MaxEstimatedCost && budget.MaxEstimatedCost >= 0m) { reason = "Cost limit reached."; return false; }
        if (state.DepthExpanded >= budget.MaxDepthExpansion) { reason = "Generation-depth expansion limit reached."; return false; }
        reason = "Within configured autonomous research limits.";
        return true;
    }
}
