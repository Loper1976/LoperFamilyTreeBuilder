using ResearchAgent.Core.AI;
using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Extraction;

namespace ResearchAgent.Core.Tests;

public sealed class StructuredExtractionTests
{
    [Fact]
    public void CitationProposalStartsWithZeroEvidenceWeight()
    {
        var source = new ResearchSource(Guid.NewGuid(), "Test", "census", null, null, DateTimeOffset.UtcNow,
            "test.txt", "aa/bb/test.txt", "hash", "text/plain", 1, PrivacyClass.PublicHistorical);
        var extraction = new DocumentExtractionResult(source.Id, "Census", "John Example age 40", [],
            [new ProposedFact("age", "40", "40", 99, "line 1", "age 40")], [], "test", "test", DateTimeOffset.UtcNow);
        var proposal = CitationProposalService.Build(Guid.NewGuid(), source, extraction).Single();
        Assert.Equal(EvidenceDirection.Neutral, proposal.Evidence.Direction);
        Assert.Equal(0m, proposal.Evidence.Weight);
        Assert.Equal(ClaimStatus.Unverified, proposal.Claim.Status);
    }

    [Fact]
    public async Task CloudModelIsBlockedForSensitiveDataByDefault()
    {
        var client = new FakeClient(ProviderTier.FreeCloud, "{\"documentType\":\"Text\",\"people\":[],\"proposedFacts\":[],\"warnings\":[]}");
        var extractor = new StructuredModelExtractor(client, new RoutingPolicy());
        var source = new ResearchSource(Guid.NewGuid(), "Sensitive", "text", null, null, DateTimeOffset.UtcNow,
            "x.txt", "x.txt", "hash", "text/plain", 1, PrivacyClass.LivingPersonSensitive);
        var file = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(file, "test");
            await Assert.ThrowsAsync<InvalidOperationException>(() => extractor.ExtractAsync(source, file));
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task ModelCannotQuoteTextNotInSource()
    {
        var json = "{\"documentType\":\"Census\",\"people\":[],\"proposedFacts\":[{\"claimType\":\"age\",\"proposedValue\":\"40\",\"normalizedValue\":\"40\",\"modelConfidence\":99,\"evidenceLocator\":\"line 1\",\"supportingText\":\"invented words\"}],\"warnings\":[]}";
        var extractor = new StructuredModelExtractor(new FakeClient(ProviderTier.LocalFree, json), new RoutingPolicy());
        var source = new ResearchSource(Guid.NewGuid(), "Test", "text", null, null, DateTimeOffset.UtcNow,
            "x.txt", "x.txt", "hash", "text/plain", 1, PrivacyClass.PublicHistorical);
        var file = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(file, "actual source words");
            await Assert.ThrowsAsync<InvalidDataException>(() => extractor.ExtractAsync(source, file));
        }
        finally { File.Delete(file); }
    }

    private sealed class FakeClient(ProviderTier tier, string json) : IStructuredModelClient
    {
        public string Provider => "fake";
        public string Model => "fake";
        public ProviderTier Tier => tier;
        public Task<string> CompleteJsonAsync(string systemPrompt, string userContent, CancellationToken cancellationToken = default) => Task.FromResult(json);
    }
}
