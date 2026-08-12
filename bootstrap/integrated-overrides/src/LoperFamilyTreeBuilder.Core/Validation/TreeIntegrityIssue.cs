namespace LoperFamilyTreeBuilder.Core.Validation;

public sealed record TreeIntegrityIssue(
    TreeIntegrityIssueCode Code,
    TreeIntegrityIssueSeverity Severity,
    string Message,
    Guid PrimaryPersonId,
    Guid? RelatedPersonId = null,
    Guid? RelationshipId = null);
