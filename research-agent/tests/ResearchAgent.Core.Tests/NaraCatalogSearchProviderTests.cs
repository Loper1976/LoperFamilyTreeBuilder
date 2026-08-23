using System.Text;
using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Research;

namespace ResearchAgent.Core.Tests;

public sealed class NaraCatalogSearchProviderTests
{
    [Fact]
    public async Task ParsesPublicCatalogCandidatesAsCitationOnlyResults()
    {
        const string json = """
            {"body":{"hits":{"hits":[{"_source":{"record":{
              "naId":12345,
              "title":"Fictional Muster Roll",
              "levelOfDescription":"item",
              "generalRecordsTypes":["Textual Records"],
              "inclusiveStartDate":{"year":1944}
            }}}]}}}
            """;
        var provider = new NaraCatalogSearchProvider(
            new HttpClient(new JsonHandler(json)));
        var query = new SourceSearchQuery(
            Guid.NewGuid(),
            "Fictional Person",
            "Military",
            null,
            null,
            "Texas",
            PrivacyClass.PublicHistorical);

        var results = await provider.SearchAsync(query);

        var result = Assert.Single(results);
        Assert.Equal("National Archives Catalog", result.Provider);
        Assert.Equal("https://catalog.archives.gov/id/12345", result.Url!.ToString());
        Assert.False(result.RequiresAuthentication);
        Assert.False(result.CanArchiveOriginal);
        Assert.Equal("12345", result.Metadata!["NaraId"]);
    }

    [Fact]
    public void RefusesLivingPersonSensitiveSearches()
    {
        var provider = new NaraCatalogSearchProvider(new HttpClient());
        var query = new SourceSearchQuery(
            Guid.NewGuid(),
            "Private Living Person",
            "Birth",
            null,
            null,
            null,
            PrivacyClass.LivingPersonSensitive);

        Assert.False(provider.Supports(query));
    }

    private sealed class JsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }
}
