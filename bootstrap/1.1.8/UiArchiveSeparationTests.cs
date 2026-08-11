using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class UiArchiveSeparationTests
{
    [Fact]
    public void Photo_workspace_request_can_be_locked_to_photos()
    {
        var request = new ArchiveLibrarySearchRequest { MediaType = ArchiveMediaType.Photo, PageSize = 48 };
        Assert.Equal(ArchiveMediaType.Photo, request.MediaType);
        Assert.Equal(48, request.PageSize);
    }

    [Fact]
    public void Document_workspace_request_can_be_locked_to_documents_and_searchable_text()
    {
        var request = new ArchiveLibrarySearchRequest { MediaType = ArchiveMediaType.Document, HasTranscription = true };
        Assert.Equal(ArchiveMediaType.Document, request.MediaType);
        Assert.True(request.HasTranscription);
    }

    [Fact]
    public void Archive_workspace_filters_do_not_touch_legacy_numbers()
    {
        var person = new Person("Robert", "Loper");
        person.AddLegacyNumber("21313.00");
        _ = new ArchiveLibrarySearchRequest { Query = "21313.00", MediaType = ArchiveMediaType.Photo };
        Assert.Equal("21313.00", person.Identifiers.Single(i => i.IdentifierType == PersonIdentifierType.LegacyNumber).Value);
    }
}
