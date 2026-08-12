using LoperFamilyTreeBuilder.Core.Entities;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class TreeIntegrityTests
{
    [Fact]
    public void Integrity_issue_never_changes_protected_legacy_number()
    {
        var person = new Person("Robert", "Loper");
        person.AddLegacyNumber("21313.00");
        _ = new TreeIntegrityIssue(
            "test:legacy",
            TreeIntegrityIssueType.MissingBirthCitation,
            TreeIntegritySeverity.Low,
            "Missing source",
            "Review the evidence.",
            "Legacy # 21313.00",
            person.Id,
            null,
            null);

        Assert.Equal("21313.00", person.Identifiers.Single(x => x.IdentifierType == PersonIdentifierType.LegacyNumber).Value);
        Assert.True(person.Identifiers.Single(x => x.IdentifierType == PersonIdentifierType.LegacyNumber).IsProtected);
    }

    [Fact]
    public void Dismissed_finding_requires_and_preserves_review_reason()
    {
        var issue = CreateIssue();
        Assert.Throws<InvalidOperationException>(() => issue.Dismiss("", "Owner"));
        issue.Dismiss("Known historical exception supported by original record.", "Owner");
        Assert.Equal(TreeIntegrityIssueStatus.Dismissed, issue.Status);
        Assert.Equal("Known historical exception supported by original record.", issue.ReviewReason);
        Assert.Equal("Owner", issue.ReviewedBy);
    }

    [Fact]
    public void Resolved_issue_reopens_when_same_condition_is_detected_again()
    {
        var issue = CreateIssue();
        issue.Resolve("Corrected after source review.", "Owner");
        Assert.Equal(TreeIntegrityIssueStatus.Resolved, issue.Status);
        issue.Refresh(TreeIntegritySeverity.Critical, issue.Title, issue.Description, issue.EvidenceSummary, issue.PersonId, issue.RelatedPersonId, issue.RelationshipId);
        Assert.Equal(TreeIntegrityIssueStatus.Open, issue.Status);
        Assert.True(issue.IsActive);
    }

    [Fact]
    public void Dismissed_issue_stays_dismissed_when_redetected()
    {
        var issue = CreateIssue();
        issue.Dismiss("Verified historical exception.", "Owner");
        issue.Refresh(TreeIntegritySeverity.High, issue.Title, issue.Description, issue.EvidenceSummary, issue.PersonId, issue.RelatedPersonId, issue.RelationshipId);
        Assert.Equal(TreeIntegrityIssueStatus.Dismissed, issue.Status);
        Assert.Equal("Verified historical exception.", issue.ReviewReason);
    }

    [Fact]
    public void Scan_run_records_severity_totals()
    {
        var run = new TreeIntegrityScanRun("Owner", "1.2.3");
        run.Complete(2, 3, 4, 5, 6, 1250);
        Assert.Equal(20, run.TotalFindings);
        Assert.Equal(2, run.CriticalCount);
        Assert.Equal(1250, run.DurationMilliseconds);
        Assert.Equal("Completed", run.Status);
    }

    private static TreeIntegrityIssue CreateIssue() => new(
        "test:issue",
        TreeIntegrityIssueType.DeathBeforeBirth,
        TreeIntegritySeverity.Critical,
        "Death before birth",
        "Recorded death date is earlier than birth date.",
        "Birth 1900; death 1890.",
        Guid.NewGuid(),
        null,
        null);
}
