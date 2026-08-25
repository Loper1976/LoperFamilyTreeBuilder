using ResearchAgent.Core.Research;

namespace ResearchAgent.Core.Tests;

public sealed class AutonomousPlannerTests
{
    [Fact]
    public void MissingParentsCreatesHighPriorityParentageTask()
    {
        var p = new PersonResearchProfile(Guid.NewGuid(), "Fictional Person", new DateOnly(1880, 1, 1), "Texas",
            new DateOnly(1940, 1, 1), "Texas", false, true, ["census"], []);
        var plan = AutonomousResearchPlanner.Build(p);
        Assert.Contains(plan.Tasks, t => t.TaskType == "parentage");
    }

    [Fact]
    public void OpenConflictCreatesResolutionTask()
    {
        var p = new PersonResearchProfile(Guid.NewGuid(), "Fictional Person", null, null, null, null,
            true, true, [], ["birth-date"]);
        Assert.Contains(AutonomousResearchPlanner.Build(p).Tasks, t => t.TaskType == "conflict-resolution");
    }

    [Fact]
    public void AutonomousRunStopsAtTaskLimit()
    {
        var allowed = ResearchExecutionPolicy.CanContinue(new ResearchBudget(MaxTasksPerRun: 3),
            new ResearchRunState(3, 0, 0, 0), out var reason);
        Assert.False(allowed);
        Assert.Contains("Task limit", reason);
    }

    [Fact]
    public void DefaultBudgetDoesNotPermitPaidSpend()
    {
        var allowed = ResearchExecutionPolicy.CanContinue(new ResearchBudget(),
            new ResearchRunState(0, 0, .01m, 0), out var reason);
        Assert.False(allowed);
        Assert.Contains("Cost limit", reason);
    }
}
