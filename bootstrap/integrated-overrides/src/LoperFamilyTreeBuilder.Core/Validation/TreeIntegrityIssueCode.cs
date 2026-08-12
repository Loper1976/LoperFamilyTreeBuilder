namespace LoperFamilyTreeBuilder.Core.Validation;

public enum TreeIntegrityIssueCode
{
    DeathBeforeBirth = 1,
    MissingPersonReference = 2,
    SelfParentRelationship = 3,
    DuplicateParentChildRelationship = 4,
    ConflictingParentChildRelationshipTypes = 5,
    ParentBornAfterChild = 6,
    ParentTooYoungAtChildBirth = 7,
    ParentUnusuallyOldAtChildBirth = 8,
    ChildBornLongAfterParentDeath = 9,
    PosthumousBirthForReview = 10
}
