using System.Text.Json.Serialization;

namespace LoperFamilyTreeBuilder.Core.Models;

public sealed record ArchiveSearchRequest(
    string? Query = null,
    string? Branch = null,
    string? LegacyNumber = null,
    LivingFilter Living = LivingFilter.Any,
    int? BirthYearFrom = null,
    int? BirthYearTo = null,
    int? DeathYearFrom = null,
    int? DeathYearTo = null,
    bool MissingParent = false,
    bool MissingSource = false,
    bool MissingCemetery = false,
    bool HasMilitary = false,
    bool HasMedical = false);

public enum LivingFilter
{
    Any,
    Living,
    Deceased
}

public sealed record ArchiveSearchHit(
    string Category,
    string Title,
    string Snippet,
    Guid? PersonId,
    string? PersonName,
    string? LegacyNumber,
    string? Route,
    string EntityType,
    double Score = 1.0);

public sealed record PersonAdvancedSearchHit(
    Guid PersonId,
    string DisplayName,
    string? LegacyNumber,
    string? Lifespan,
    string? Branch,
    bool IsLiving,
    IReadOnlyList<string> Flags);

public sealed record DataQualityIssue(
    Guid? PersonId,
    string? PersonName,
    string Severity,
    string Category,
    string Message,
    string? Route = null);

public sealed record ResearchQualitySummary(
    int People,
    int MissingBirth,
    int DeceasedMissingDeath,
    int MissingParent,
    int MissingSources,
    int MissingCemetery,
    int MissingPrimaryPhoto,
    int DuplicateLegacyNumbers,
    int OpenProofWorkspaces,
    int RepositorySearches,
    IReadOnlyList<DataQualityIssue> Issues);

public sealed class ResearchIntelligenceState
{
    public int SchemaVersion { get; set; } = 1;
    public List<SavedArchiveSearch> SavedSearches { get; set; } = [];
    public List<PersonNameAliasRecord> Aliases { get; set; } = [];
    public List<ProofWorkspaceRecord> ProofWorkspaces { get; set; } = [];
    public List<RepositorySearchRecord> RepositorySearches { get; set; } = [];
    public List<ResearchPlanRecord> ResearchPlans { get; set; } = [];
}

public sealed class SavedArchiveSearch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public ArchiveSearchRequest Request { get; set; } = new();
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PersonNameAliasRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PersonId { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string AliasType { get; set; } = "Alternate Name";
    public string? Notes { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ProofWorkspaceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? PersonId { get; set; }
    public string ResearchQuestion { get; set; } = string.Empty;
    public string Status { get; set; } = "Under Review";
    public string? ResearcherReasoning { get; set; }
    public string? Conclusion { get; set; }
    public string? Reviewer { get; set; }
    public DateTimeOffset? ReviewDateUtc { get; set; }
    public List<ProofCandidateRecord> Candidates { get; set; } = [];
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ProofCandidateRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CandidateAnswer { get; set; } = string.Empty;
    public string? EvidenceFor { get; set; }
    public string? EvidenceAgainst { get; set; }
    public string? ConflictingEvidence { get; set; }
    public string? NegativeSearches { get; set; }
    public string? SourceReferences { get; set; }
}

public sealed class RepositorySearchRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? PersonId { get; set; }
    public string Repository { get; set; } = string.Empty;
    public string? Collection { get; set; }
    public string SearchQuery { get; set; } = string.Empty;
    public DateTimeOffset SearchDateUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? Results { get; set; }
    public bool NegativeSearch { get; set; }
    public string? Notes { get; set; }
    public string? FollowUpAction { get; set; }
}

public sealed class ResearchPlanRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ExistingTaskId { get; set; }
    public Guid? PersonId { get; set; }
    public string ResearchQuestion { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public DateOnly? DueDate { get; set; }
    public string? AssignedResearcher { get; set; }
    public string? Repository { get; set; }
    public string? RelatedEvidence { get; set; }
    public string? FollowUpAction { get; set; }
    public string Status { get; set; } = "Open";
    public bool Archived { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
