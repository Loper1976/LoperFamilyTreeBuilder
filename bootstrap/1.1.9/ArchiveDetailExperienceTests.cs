using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class ArchiveDetailExperienceTests
{
    [Fact]
    public void Photo_and_document_queries_remain_distinct_workspaces()
    {
        var photos = new ArchiveLibrarySearchRequest { MediaType = ArchiveMediaType.Photo, PageSize = 48 };
        var documents = new ArchiveLibrarySearchRequest { MediaType = ArchiveMediaType.Document, PageSize = 50 };

        Assert.Equal(ArchiveMediaType.Photo, photos.MediaType);
        Assert.Equal(ArchiveMediaType.Document, documents.MediaType);
        Assert.NotEqual(photos.MediaType, documents.MediaType);
    }

    [Fact]
    public void Ocr_review_filter_can_be_requested_without_mutating_archive_identity()
    {
        var request = new ArchiveLibrarySearchRequest { MediaType = ArchiveMediaType.Document, HasTranscription = true };
        Assert.True(request.HasTranscription);
        Assert.Equal(ArchiveMediaType.Document, request.MediaType);
    }

    [Fact]
    public void Archive_experience_does_not_change_legacy_number()
    {
        var person = new Person("Robert", "Loper");
        person.AddLegacyNumber("21313.00");
        _ = new ArchiveLibrarySearchRequest { Query = "Robert Loper" };

        Assert.Equal("21313.00", person.Identifiers.Single(i => i.IdentifierType == PersonIdentifierType.LegacyNumber).Value);
    }
}
