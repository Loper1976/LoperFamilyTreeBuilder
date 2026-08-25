using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Research;

namespace ResearchAgent.Core.UI;

public sealed record StartResearchRequest(PersonResearchProfile Person, PrivacyClass PrivacyClass,
    ResearchBudget? Budget = null);
public sealed record StartResearchResult(ResearchPlan Plan, ResearchRunResult Run);

public sealed class ResearchCommandService(ResearchExecutor executor)
{
    public async Task<StartResearchResult> ResearchThisPersonAsync(StartResearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var plan = AutonomousResearchPlanner.Build(request.Person);
        var budget = request.Budget ?? new ResearchBudget();
        var run = await executor.ExecuteAsync(plan, request.Person, budget, request.PrivacyClass, cancellationToken);
        return new(plan, run);
    }
}
