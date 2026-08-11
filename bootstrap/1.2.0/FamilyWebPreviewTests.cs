using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Data.Services;
using LoperFamilyTreeBuilder.Infrastructure.Configuration;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class FamilyWebPreviewTests
{
    [Fact]
    public void Default_web_experience_remains_local_and_not_hosted()
    {
        var options=WebExperienceOptions.Create(null,null,null,null);
        Assert.False(options.HostedMode);
        Assert.Equal(WebAccessMode.LocalOwner,options.AccessMode);
    }

    [Fact]
    public void Public_profile_slug_is_normalized_without_touching_legacy_number()
    {
        var person=new Person("Robert","Loper");
        person.AddLegacyNumber("21313.00");
        Assert.Equal("robert-j-loper",PublicProfileService.NormalizeSlug(" Robert J. Loper "));
        Assert.Equal("21313.00",person.Identifiers.Single(x=>x.IdentifierType==PersonIdentifierType.LegacyNumber).Value);
    }

    [Fact]
    public void Hosted_mode_must_be_explicit()
    {
        var options=WebExperienceOptions.Create("false","family","https://loper.family","local-filesystem");
        Assert.False(options.HostedMode);
    }
}
