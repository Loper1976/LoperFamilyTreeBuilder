using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Extraction;
using ResearchAgent.Core.Ingestion;
using ResearchAgent.Core.Persistence;
using ResearchAgent.Core.Workflow;

namespace ResearchAgent.Core.Tests;

public sealed class PersistenceAndIntakeTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "loper-agent-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task JsonStoreRoundTripsResearchClaim()
    {
        var store = new JsonResearchStore(Path.Combine(_root, "store"));
        var claim = new ResearchClaim(Guid.NewGuid(), Guid.NewGuid(), "birth-place", "Texas", "Texas",
            ClaimStatus.Unverified, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        await store.SaveClaimAsync(claim);
        var loaded = await store.GetClaimAsync(claim.Id);
        Assert.Equal(claim, loaded);
    }

    [Fact]
    public async Task IntakePreservesOriginalAndDoesNotCreateAcceptedFact()
    {
        Directory.CreateDirectory(_root);
        var input = Path.Combine(_root, "sample.txt");
        await File.WriteAllTextAsync(input, "Fictional genealogy test record only.");
        var archive = Path.Combine(_root, "archive");
        var store = new JsonResearchStore(Path.Combine(_root, "store"));
        var workflow = new DocumentIntakeWorkflow(new SourceIngestionService(), store, new TextDocumentExtractor());

        var result = await workflow.RunAsync(new SourceIngestionRequest(input, archive, "Fictional record", "test",
            "test-suite", null, PrivacyClass.PublicHistorical), Guid.NewGuid());

        var archived = Path.Combine(archive, result.Source.ArchivedRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(archived));
        Assert.True(result.Source.IsOriginalImmutable);
        Assert.Empty(result.ProposedClaims); // deterministic text extractor makes no genealogical inference
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);
                Directory.Delete(_root, true);
            }
        }
        catch { }
    }
}
