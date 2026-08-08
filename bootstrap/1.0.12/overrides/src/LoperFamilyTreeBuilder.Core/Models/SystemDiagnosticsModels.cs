namespace LoperFamilyTreeBuilder.Core.Models;

public sealed record SystemDiagnosticCheck(
    string Category,
    string Name,
    bool Passed,
    bool Required,
    string Detail);

public sealed record SystemDiagnosticsReport(
    string ApplicationVersion,
    DateTimeOffset GeneratedUtc,
    int People,
    int FamilyBranches,
    int ParentChildRelationships,
    int CoupleRelationships,
    int LegacyNumbers,
    int DuplicateLegacyNumbers,
    int AuditEvents,
    int ManagedBackups,
    string LatestAppliedMigration,
    IReadOnlyList<SystemDiagnosticCheck> Checks)
{
    public bool IsReady => Checks.Where(x => x.Required).All(x => x.Passed);
}
