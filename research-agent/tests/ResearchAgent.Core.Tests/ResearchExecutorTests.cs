using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Research;
using ResearchAgent.Core.UI;

namespace ResearchAgent.Core.Tests;

public sealed class ResearchExecutorTests
{
    [Fact]
    public async Task ResearchThisPersonBuildsAndExecutesPlanWithoutPaidBudget()
    {
        var provider = new FakeProvider();
        var service = new ResearchCommandService(new ResearchExecutor([provider]));
        var person = new PersonResearchProfile(Guid.NewGuid(), "Fictional Person", null, "Texas", null, null,
            false, false, [], []);
        var result = await service.ResearchThisPersonAsync(new(person, PrivacyClass.PublicHistorical,
            new ResearchBudget(MaxTasksPerRun: 2, MaxEstimatedCost: 0)));
        Assert.NotEmpty(result.Plan.Tasks);
        Assert.True(result.Run.FinalState.TasksAttempted <= 2);
    }

    private sealed class FakeProvider : ISourceSearchProvider
    {
        public string Name => "fake-public-archive";
        public bool Supports(SourceSearchQuery query) => true;
        public Task<IReadOnlyList<SourceSearchResult>> SearchAsync(SourceSearchQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SourceSearchResult>>([
                new(Name, "Fictional record", new Uri("https://example.invalid/record"), "Test archive",
                    query.RecordType, "Fictional test fixture", 80, false, true)
            ]);
    }
}
