using LoperFamilyTreeBuilder.Core.Entities;

namespace LoperFamilyTreeBuilder.Core.Models;

public sealed class TreeIntegrityFilterRequest
{
    public string Search { get; set; } = string.Empty;
    public TreeIntegritySeverity? Severity { get; set; }
    public TreeIntegrityIssueStatus? Status { get; set; }
    public TreeIntegrityIssueType? IssueType { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed record TreeIntegrityIssueListItem(
    Guid IssueId,
    TreeIntegrityIssueType IssueType,
    TreeIntegritySeverity Severity,
    TreeIntegrityIssueStatus Status,
    bool IsActive,
    string Title,
    string Description,
    string EvidenceSummary,
    Guid? PersonId,
    string PersonName,
    string LegacyNumber,
    Guid? RelatedPersonId,
    string RelatedPersonName,
    string RelatedLegacyNumber,
    DateTimeOffset FirstDetectedUtc,
    DateTimeOffset LastDetectedUtc,
    DateTimeOffset? ReviewedUtc,
    string ReviewedBy,
    string ReviewReason);

public sealed record TreeIntegritySummary(
    int ActiveTotal,
    int Critical,
    int High,
    int Medium,
    int Low,
    int Informational,
    int Dismissed,
    int Resolved,
    DateTimeOffset? LastScanUtc,
    long? LastScanDurationMilliseconds,
    string RulesVersion);

public sealed record TreeIntegrityDashboard(
    TreeIntegritySummary Summary,
    PagedResult<TreeIntegrityIssueListItem> Issues,
    IReadOnlyList<TreeIntegrityScanRunSummary> RecentScans);

public sealed record TreeIntegrityScanRunSummary(
    Guid RunId,
    string StartedBy,
    string RulesVersion,
    string Status,
    int TotalFindings,
    int CriticalCount,
    int HighCount,
    int MediumCount,
    int LowCount,
    int InformationalCount,
    long DurationMilliseconds,
    DateTimeOffset StartedUtc,
    DateTimeOffset? CompletedUtc);

public sealed record TreeIntegrityScanResult(
    Guid RunId,
    int TotalFindings,
    int Critical,
    int High,
    int Medium,
    int Low,
    int Informational,
    long DurationMilliseconds,
    string RulesVersion);
