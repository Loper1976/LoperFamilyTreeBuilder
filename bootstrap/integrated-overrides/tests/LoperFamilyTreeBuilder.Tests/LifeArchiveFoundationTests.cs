using LoperFamilyTreeBuilder.Core.Entities;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class LifeArchiveFoundationTests
{
    [Fact]
    public void FamilyUnion_rejects_self_union()
    {
        var personId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new FamilyUnion(personId, personId, FamilyUnionType.Marriage));
    }

    [Fact]
    public void FamilyUnion_rejects_end_before_start()
    {
        var union = new FamilyUnion(Guid.NewGuid(), Guid.NewGuid(), FamilyUnionType.Marriage);
        Assert.Throws<ArgumentException>(() => union.Update(FamilyUnionStatus.Divorced, new DateOnly(2000, 1, 1), new DateOnly(1999, 1, 1), null, null, null));
    }

    [Fact]
    public void LifeEvent_preserves_original_place_text_separately_from_coordinates()
    {
        var item = new LifeEvent(Guid.NewGuid(), LifeEventType.Residence, "Residence");
        item.UpdateLocation("Old Tomball Road, Texas", 30.0972m, -95.6161m);
        Assert.Equal("Old Tomball Road, Texas", item.OriginalPlaceText);
        Assert.Equal(30.0972m, item.Latitude);
        Assert.Equal(-95.6161m, item.Longitude);
    }

    [Fact]
    public void LifeEvent_rejects_invalid_coordinates()
    {
        var item = new LifeEvent(Guid.NewGuid(), LifeEventType.Travel, "Trip");
        Assert.Throws<ArgumentOutOfRangeException>(() => item.UpdateLocation("Somewhere", 91m, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => item.UpdateLocation("Somewhere", 0m, -181m));
    }

    [Fact]
    public void LifeEvent_preserves_approximate_date_flag()
    {
        var item = new LifeEvent(Guid.NewGuid(), LifeEventType.Census, "Census household");
        item.UpdateChronology(new DateOnly(1940, 4, 1), null, true);
        Assert.True(item.IsDateApproximate);
        Assert.Equal(new DateOnly(1940, 4, 1), item.StartDate);
    }

    [Fact]
    public void ArchiveItem_requires_preserved_original_path()
    {
        Assert.Throws<ArgumentException>(() => new ArchiveItem(ArchiveItemType.Photo, "Portrait", " "));
    }

    [Fact]
    public void ArchiveItem_can_store_exif_ready_location_without_replacing_place_text()
    {
        var item = new ArchiveItem(ArchiveItemType.Photo, "Family picnic", @"D:\Family Archive\photo.jpg");
        item.UpdateMetadata("ABC123", DateTimeOffset.Parse("1978-07-04T12:00:00-05:00"), "Spring Creek Park", 30.1m, -95.6m, "Picnic", "Original family photograph", "{\"camera\":\"unknown\"}");
        Assert.Equal("Spring Creek Park", item.OriginalPlaceText);
        Assert.Equal("ABC123", item.Sha256);
        Assert.Contains("camera", item.MetadataJson);
    }

    [Fact]
    public void SourceRecord_requires_citation_and_keeps_it_verbatim_after_metadata_update()
    {
        const string citation = "U.S. Census, 1950; enumeration district 1-2; page 4.";
        var source = new SourceRecord("1950 Census", citation);
        source.Update(Guid.NewGuid(), "National Archives", "ED 1-2", "Reviewed");
        Assert.Equal(citation, source.Citation);
    }
}
