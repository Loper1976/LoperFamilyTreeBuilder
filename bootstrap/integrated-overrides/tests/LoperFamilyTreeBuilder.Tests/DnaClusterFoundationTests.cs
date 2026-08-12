using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Genealogy;
using LoperFamilyTreeBuilder.Core.Models;
using LoperFamilyTreeBuilder.Core.Policies;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class DnaClusterFoundationTests
{
    [Fact]
    public void Dna_match_defaults_to_owner_only_and_unreviewed()
    {
        var match = new DnaMatch("TestProvider", "M-100", "Research Match", 84.2m, 5);

        Assert.Equal(DnaMatchVisibility.OwnerOnly, match.Visibility);
        Assert.Equal(DnaMatchReviewStatus.Imported, match.ReviewStatus);
        Assert.Empty(match.ManualAncestralLineLabel);
    }

    [Fact]
    public void Dna_research_does_not_modify_legacy_number()
    {
        var person = new Person("Test", "Ancestor");
        person.AddLegacyNumber(" 21314.00 ");
        var exactLegacy = person.Identifiers.Single().Value;

        var match = new DnaMatch("TestProvider", "M-101", "DNA Match", 125m);
        match.SaveResearchReview("Maternal research line", "Test review only");

        Assert.Equal(" 21314.00 ", exactLegacy);
        Assert.Equal(exactLegacy, person.Identifiers.Single().Value);
    }

    [Fact]
    public void Shared_match_edge_is_canonical_and_rejects_self_links()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var edge = new DnaSharedMatch(b, a, "Test evidence");

        Assert.True(edge.MatchAId.CompareTo(edge.MatchBId) < 0);
        Assert.Throws<ArgumentException>(() => new DnaSharedMatch(a, a));
    }

    [Fact]
    public void Cluster_engine_groups_connected_networks_and_leaves_singletons_unclustered()
    {
        var a = Match("A", 100m);
        var b = Match("B", 80m);
        var c = Match("C", 60m);
        var d = Match("D", 40m);
        var engine = new DnaClusterEngine();

        var result = engine.Build(
            new[] { a, b, c, d },
            new[]
            {
                new DnaSharedMatchSnapshot(a.Id, b.Id),
                new DnaSharedMatchSnapshot(b.Id, c.Id)
            });

        var cluster = Assert.Single(result.Clusters);
        Assert.Equal(3, cluster.MatchCount);
        Assert.Equal(2, cluster.EvidenceLinkCount);
        Assert.Equal("Cluster 1", cluster.DisplayLabel);
        Assert.Equal(d.Id, Assert.Single(result.UnclusteredMatches).MatchId);
    }

    [Fact]
    public void Cluster_engine_deduplicates_evidence_links()
    {
        var a = Match("A", 100m);
        var b = Match("B", 80m);
        var engine = new DnaClusterEngine();

        var result = engine.Build(
            new[] { a, b },
            new[]
            {
                new DnaSharedMatchSnapshot(a.Id, b.Id),
                new DnaSharedMatchSnapshot(b.Id, a.Id),
                new DnaSharedMatchSnapshot(a.Id, b.Id)
            });

        Assert.Equal(1, Assert.Single(result.Clusters).EvidenceLinkCount);
    }

    [Fact]
    public void Cluster_line_is_not_inferred_from_only_one_reviewed_member()
    {
        var a = new DnaMatchSnapshot(Guid.NewGuid(), "A", 100m, "Loper line");
        var b = new DnaMatchSnapshot(Guid.NewGuid(), "B", 80m, string.Empty);
        var engine = new DnaClusterEngine();

        var result = engine.Build(
            new[] { a, b },
            new[] { new DnaSharedMatchSnapshot(a.Id, b.Id) });

        Assert.Equal("Cluster 1", Assert.Single(result.Clusters).DisplayLabel);
    }

    [Fact]
    public void Cluster_line_can_display_after_all_members_share_same_manual_review_label()
    {
        var a = new DnaMatchSnapshot(Guid.NewGuid(), "A", 100m, "Loper line");
        var b = new DnaMatchSnapshot(Guid.NewGuid(), "B", 80m, "Loper line");
        var engine = new DnaClusterEngine();

        var result = engine.Build(
            new[] { a, b },
            new[] { new DnaSharedMatchSnapshot(a.Id, b.Id) });

        Assert.Equal("Reviewed line: Loper line", Assert.Single(result.Clusters).DisplayLabel);
    }

    [Theory]
    [InlineData(DnaMatchVisibility.DnaAuthorized, DnaAccessScope.DnaAuthorized, true)]
    [InlineData(DnaMatchVisibility.OwnerOnly, DnaAccessScope.DnaAuthorized, false)]
    [InlineData(DnaMatchVisibility.OwnerOnly, DnaAccessScope.OwnerAdmin, true)]
    [InlineData(DnaMatchVisibility.DnaAuthorized, DnaAccessScope.None, false)]
    public void Dna_privacy_policy_requires_explicit_scope(
        DnaMatchVisibility visibility,
        DnaAccessScope scope,
        bool expected)
    {
        Assert.Equal(expected, DnaPrivacyPolicy.CanView(visibility, scope));
    }

    [Fact]
    public void Only_owner_admin_can_edit_dna_records()
    {
        Assert.True(DnaPrivacyPolicy.CanEdit(DnaAccessScope.OwnerAdmin));
        Assert.False(DnaPrivacyPolicy.CanEdit(DnaAccessScope.DnaAuthorized));
        Assert.False(DnaPrivacyPolicy.CanEdit(DnaAccessScope.None));
    }

    private static DnaMatchSnapshot Match(string displayName, decimal totalCm) =>
        new(Guid.NewGuid(), displayName, totalCm, string.Empty);
}
