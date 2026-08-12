namespace LoperFamilyTreeBuilder.Core.Entities;

public enum TreeIntegritySeverity
{
    Critical = 1,
    High = 2,
    Medium = 3,
    Low = 4,
    Informational = 5
}

public enum TreeIntegrityIssueStatus
{
    Open = 1,
    Dismissed = 2,
    Resolved = 3
}

public enum TreeIntegrityIssueType
{
    DeathBeforeBirth = 1,
    ParentBornAfterChild = 2,
    ParentTooYoung = 3,
    ParentImplausiblyOld = 4,
    ChildBornAfterParentDeath = 5,
    PossiblePosthumousBirth = 6,
    CircularAncestry = 7,
    DuplicateParentChildRelationship = 8,
    RelationshipStartBeforeBirth = 9,
    RelationshipStartAfterDeath = 10,
    RelationshipEndBeforeStart = 11,
    BurialBeforeDeath = 12,
    MilitaryServiceBeforeBirth = 13,
    MilitaryServiceAfterDeath = 14,
    MilitaryEndBeforeStart = 15,
    DuplicateTimelineEvent = 16,
    ConflictingTimelineEvent = 17,
    PossibleDuplicatePerson = 18,
    DuplicateLegacyNumber = 19,
    MissingBirthDate = 20,
    MissingDeathDateForDeceasedPerson = 21,
    OnlyOneParentLinked = 22,
    MissingBirthCitation = 23,
    MissingDeathCitation = 24
}

public sealed class TreeIntegrityIssue
{
    private TreeIntegrityIssue() { }

    public TreeIntegrityIssue(
        string issueKey,
        TreeIntegrityIssueType issueType,
        TreeIntegritySeverity severity,
        string title,
        string description,
        string evidenceSummary,
        Guid? personId,
        Guid? relatedPersonId,
        Guid? relationshipId)
    {
        if (string.IsNullOrWhiteSpace(issueKey))
            throw new ArgumentException("Issue key is required.", nameof(issueKey));

        Id = Guid.NewGuid();
        IssueKey = issueKey.Trim();
        IssueType = issueType;
        Severity = severity;
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
        EvidenceSummary = evidenceSummary ?? string.Empty;
        PersonId = personId;
        RelatedPersonId = relatedPersonId;
        RelationshipId = relationshipId;
        Status = TreeIntegrityIssueStatus.Open;
        IsActive = true;
        FirstDetectedUtc = DateTimeOffset.UtcNow;
        LastDetectedUtc = FirstDetectedUtc;
    }

    public Guid Id { get; private set; }
    public string IssueKey { get; private set; } = string.Empty;
    public TreeIntegrityIssueType IssueType { get; private set; }
    public TreeIntegritySeverity Severity { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string EvidenceSummary { get; private set; } = string.Empty;
    public Guid? PersonId { get; private set; }
    public Guid? RelatedPersonId { get; private set; }
    public Guid? RelationshipId { get; private set; }
    public TreeIntegrityIssueStatus Status { get; private set; }
    public bool IsActive { get; private set; }
    public string ReviewReason { get; private set; } = string.Empty;
    public string ReviewedBy { get; private set; } = string.Empty;
    public DateTimeOffset FirstDetectedUtc { get; private set; }
    public DateTimeOffset LastDetectedUtc { get; private set; }
    public DateTimeOffset? ReviewedUtc { get; private set; }
    public DateTimeOffset? ResolvedUtc { get; private set; }

    public void Refresh(
        TreeIntegritySeverity severity,
        string title,
        string description,
        string evidenceSummary,
        Guid? personId,
        Guid? relatedPersonId,
        Guid? relationshipId)
    {
        Severity = severity;
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
        EvidenceSummary = evidenceSummary ?? string.Empty;
        PersonId = personId;
        RelatedPersonId = relatedPersonId;
        RelationshipId = relationshipId;
        IsActive = true;
        LastDetectedUtc = DateTimeOffset.UtcNow;

        if (Status == TreeIntegrityIssueStatus.Resolved)
        {
            Status = TreeIntegrityIssueStatus.Open;
            ReviewReason = string.Empty;
            ReviewedBy = string.Empty;
            ReviewedUtc = null;
            ResolvedUtc = null;
        }
    }

    public void MarkCleared()
    {
        IsActive = false;
        if (Status == TreeIntegrityIssueStatus.Open)
        {
            Status = TreeIntegrityIssueStatus.Resolved;
            ReviewedBy = "Automated Tree Error Checker";
            ReviewReason = "The condition was no longer detected in the latest scan.";
            ReviewedUtc = DateTimeOffset.UtcNow;
            ResolvedUtc = ReviewedUtc;
        }
    }

    public void Dismiss(string reason, string actor)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("A dismissal reason is required so the historical review decision is preserved.");

        Status = TreeIntegrityIssueStatus.Dismissed;
        ReviewReason = reason.Trim();
        ReviewedBy = actor ?? string.Empty;
        ReviewedUtc = DateTimeOffset.UtcNow;
    }

    public void Resolve(string reason, string actor)
    {
        Status = TreeIntegrityIssueStatus.Resolved;
        ReviewReason = reason ?? string.Empty;
        ReviewedBy = actor ?? string.Empty;
        ReviewedUtc = DateTimeOffset.UtcNow;
        ResolvedUtc = ReviewedUtc;
    }

    public void Reopen(string actor)
    {
        Status = TreeIntegrityIssueStatus.Open;
        ReviewReason = "Reopened for additional research.";
        ReviewedBy = actor ?? string.Empty;
        ReviewedUtc = DateTimeOffset.UtcNow;
        ResolvedUtc = null;
    }
}

public sealed class TreeIntegrityScanRun
{
    private TreeIntegrityScanRun() { }

    public TreeIntegrityScanRun(string actor, string rulesVersion)
    {
        Id = Guid.NewGuid();
        StartedBy = actor ?? string.Empty;
        RulesVersion = rulesVersion ?? string.Empty;
        StartedUtc = DateTimeOffset.UtcNow;
        Status = "Running";
    }

    public Guid Id { get; private set; }
    public string StartedBy { get; private set; } = string.Empty;
    public string RulesVersion { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public int CriticalCount { get; private set; }
    public int HighCount { get; private set; }
    public int MediumCount { get; private set; }
    public int LowCount { get; private set; }
    public int InformationalCount { get; private set; }
    public int TotalFindings { get; private set; }
    public long DurationMilliseconds { get; private set; }
    public DateTimeOffset StartedUtc { get; private set; }
    public DateTimeOffset? CompletedUtc { get; private set; }

    public void Complete(int critical, int high, int medium, int low, int informational, long durationMilliseconds)
    {
        CriticalCount = critical;
        HighCount = high;
        MediumCount = medium;
        LowCount = low;
        InformationalCount = informational;
        TotalFindings = critical + high + medium + low + informational;
        DurationMilliseconds = durationMilliseconds;
        Status = "Completed";
        CompletedUtc = DateTimeOffset.UtcNow;
    }

    public void Fail(long durationMilliseconds)
    {
        DurationMilliseconds = durationMilliseconds;
        Status = "Failed";
        CompletedUtc = DateTimeOffset.UtcNow;
    }
}
