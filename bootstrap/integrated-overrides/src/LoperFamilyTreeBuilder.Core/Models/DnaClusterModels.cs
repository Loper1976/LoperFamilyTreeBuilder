using LoperFamilyTreeBuilder.Core.Entities;

namespace LoperFamilyTreeBuilder.Core.Models;

public sealed record DnaMatchImportRow(
    string ProviderName,
    string ExternalMatchId,
    string DisplayName,
    decimal TotalCentimorgans,
    int? SharedSegments = null);

public sealed record DnaImportResult(
    int Added,
    int DuplicateRows,
    IReadOnlyList<Guid> AddedMatchIds);

public sealed record DnaMatchListItem(
    Guid Id,
    string ProviderName,
    string ExternalMatchId,
    string DisplayName,
    decimal TotalCentimorgans,
    int? SharedSegments,
    DnaMatchVisibility Visibility,
    DnaMatchReviewStatus ReviewStatus,
    string ManualAncestralLineLabel,
    string ResearchNotes,
    DateTimeOffset ModifiedUtc);

public sealed record DnaMatchSnapshot(
    Guid Id,
    string DisplayName,
    decimal TotalCentimorgans,
    string ManualAncestralLineLabel);

public sealed record DnaSharedMatchSnapshot(Guid MatchAId, Guid MatchBId);

public sealed record DnaClusterMember(
    Guid MatchId,
    string DisplayName,
    decimal TotalCentimorgans,
    int SharedConnections,
    string ManualAncestralLineLabel);

public sealed record DnaClusterGroup(
    int ClusterNumber,
    string DisplayLabel,
    int MatchCount,
    int EvidenceLinkCount,
    decimal NetworkDensity,
    IReadOnlyList<DnaClusterMember> Members);

public sealed record DnaClusterResult(
    IReadOnlyList<DnaClusterGroup> Clusters,
    IReadOnlyList<DnaClusterMember> UnclusteredMatches);

public sealed record DnaClusterDashboard(
    int VisibleMatches,
    int SharedMatchLinks,
    int NetworkClusters,
    int ReviewedMatches,
    int UnclusteredMatches);
