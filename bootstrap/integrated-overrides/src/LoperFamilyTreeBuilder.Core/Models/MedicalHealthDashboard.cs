namespace LoperFamilyTreeBuilder.Core.Models;

public sealed record MedicalHealthDashboard(
    int VisibleRecords,
    int PeopleWithRecords,
    int HereditaryRelevantRecords,
    int ActiveOrConfirmedRecords,
    IReadOnlyList<MedicalFamilyPattern> FamilyPatterns);

public sealed record MedicalFamilyPattern(
    string ConditionName,
    int PeopleCount,
    int RecordCount);
