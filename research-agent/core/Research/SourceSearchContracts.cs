using ResearchAgent.Core.Domain;

namespace ResearchAgent.Core.Research;

public sealed record SourceSearchQuery(Guid PersonId, string QueryText, string RecordType,
    DateOnly? FromDate, DateOnly? ToDate, string? Place, PrivacyClass PrivacyClass);

public sealed record SourceSearchResult(string Provider, string Title, Uri? Url, string? Repository,
    string? RecordType, string? Summary, decimal MatchHint, bool RequiresAuthentication,
    bool CanArchiveOriginal, IReadOnlyDictionary<string, string>? Metadata = null);

public interface ISourceSearchProvider
{
    string Name { get; }
    bool Supports(SourceSearchQuery query);
    Task<IReadOnlyList<SourceSearchResult>> SearchAsync(SourceSearchQuery query,
        CancellationToken cancellationToken = default);
}

public static class SearchQueryFactory
{
    public static SourceSearchQuery FromTask(PersonResearchProfile person, ResearchTask task, PrivacyClass privacy)
    {
        var place = person.BirthPlace ?? person.DeathPlace;
        return new(person.PersonId, task.Question, task.TaskType,
            person.BirthDate?.AddYears(-2), person.DeathDate?.AddYears(2), place, privacy);
    }
}
