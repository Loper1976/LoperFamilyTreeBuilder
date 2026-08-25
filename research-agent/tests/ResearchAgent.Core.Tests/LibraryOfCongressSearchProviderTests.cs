using System.Text;
using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Research;

namespace ResearchAgent.Core.Tests;

public sealed class LibraryOfCongressSearchProviderTests
{
    [Fact]
    public async Task ParsesPublicCandidatesAsCitationOnlyResults()
    {
        const string json = """
            {"results":[{
              "id":"http://www.loc.gov/item/123/",
              "title":"Fictional Passenger List",
              "date":"1901-01-01",
              "original_format":["manuscript/mixed material"],
              "description":["A fictional test description."]
            }]}
            """;
        var provider = new LibraryOfCongressSearchProvider(
            new HttpClient(new JsonHandler(json)));
        var query = new SourceSearchQuery(
            Guid.NewGuid(), "Fictional Person", "Immigration", null, null,
            "New York", PrivacyClass.PublicHistorical);

        var result = Assert.Single(await provider.SearchAsync(query));

        Assert.Equal("Library of Congress", result.Provider);
        Assert.Equal("https://www.loc.gov/item/123/", result.Url!.ToString());
        Assert.False(result.RequiresAuthentication);
        Assert.False(result.CanArchiveOriginal);
    }

    [Fact]
    public void RefusesLivingPersonSensitiveSearches()
    {
        var provider = new LibraryOfCongressSearchProvider(new HttpClient());
        var query = new SourceSearchQuery(
            Guid.NewGuid(), "Private Living Person", "Birth", null, null,
            null, PrivacyClass.LivingPersonSensitive);

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
