using LoperFamilyTreeBuilder.ImportExport.Gedcom;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class GedcomImportPreviewService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory,
    GedcomParser parser,
    GedcomDuplicateAnalyzer analyzer)
{
    public async Task<GedcomImportPreview> PreviewAsync(
        Stream gedcom,
        CancellationToken cancellationToken = default)
    {
        var document = await parser.ParseAsync(gedcom, cancellationToken: cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var acceptedPeople = await db.People.AsNoTracking()
            .Select(person => new AcceptedPersonMatchInput(
                person.Id,
                person.GivenName,
                person.Surname,
                person.BirthDate))
            .ToArrayAsync(cancellationToken);
        return analyzer.CreatePreview(document, acceptedPeople);
    }
}
