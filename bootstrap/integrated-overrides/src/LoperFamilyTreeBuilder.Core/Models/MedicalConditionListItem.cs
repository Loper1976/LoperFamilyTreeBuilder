using LoperFamilyTreeBuilder.Core.Entities;

namespace LoperFamilyTreeBuilder.Core.Models;

public sealed record MedicalConditionListItem(
    Guid Id,
    Guid PersonId,
    string PersonName,
    string? LegacyNumber,
    string ConditionName,
    MedicalConditionStatus Status,
    MedicalConditionSeverity Severity,
    bool IsHereditaryRelevant,
    DateOnly? DiagnosisDate,
    int? OnsetAgeYears,
    MedicalRecordVisibility Visibility);
