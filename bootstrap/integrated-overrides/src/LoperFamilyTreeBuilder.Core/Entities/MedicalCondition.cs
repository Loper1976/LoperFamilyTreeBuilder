namespace LoperFamilyTreeBuilder.Core.Entities;

public sealed class MedicalCondition
{
    private MedicalCondition()
    {
    }

    public MedicalCondition(Guid personId, string conditionName)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("A person is required.", nameof(personId));

        if (string.IsNullOrWhiteSpace(conditionName))
            throw new ArgumentException("A condition name is required.", nameof(conditionName));

        Id = Guid.NewGuid();
        PersonId = personId;
        ConditionName = conditionName.Trim();
        Status = MedicalConditionStatus.FamilyReported;
        Severity = MedicalConditionSeverity.Unknown;
        Visibility = MedicalRecordVisibility.MedicalAuthorized;
        CreatedUtc = DateTimeOffset.UtcNow;
        ModifiedUtc = CreatedUtc;
    }

    public Guid Id { get; private set; }

    public Guid PersonId { get; private set; }

    public Person? Person { get; private set; }

    public string ConditionName { get; private set; } = string.Empty;

    public MedicalConditionStatus Status { get; private set; }

    public MedicalConditionSeverity Severity { get; private set; }

    public bool IsHereditaryRelevant { get; private set; }

    public DateOnly? DiagnosisDate { get; private set; }

    public int? OnsetAgeYears { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string Facility { get; private set; } = string.Empty;

    public string Notes { get; private set; } = string.Empty;

    public string SourceCitation { get; private set; } = string.Empty;

    public MedicalRecordVisibility Visibility { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset ModifiedUtc { get; private set; }

    public void UpdateClinicalSummary(
        MedicalConditionStatus status,
        MedicalConditionSeverity severity,
        bool isHereditaryRelevant,
        DateOnly? diagnosisDate,
        int? onsetAgeYears)
    {
        if (onsetAgeYears is < 0 or > 125)
            throw new ArgumentOutOfRangeException(nameof(onsetAgeYears));

        Status = status;
        Severity = severity;
        IsHereditaryRelevant = isHereditaryRelevant;
        DiagnosisDate = diagnosisDate;
        OnsetAgeYears = onsetAgeYears;
        Touch();
    }

    public void UpdateProvenance(
        string? provider,
        string? facility,
        string? notes,
        string? sourceCitation)
    {
        Provider = (provider ?? string.Empty).Trim();
        Facility = (facility ?? string.Empty).Trim();
        Notes = notes ?? string.Empty;
        SourceCitation = sourceCitation ?? string.Empty;
        Touch();
    }

    public void SetVisibility(MedicalRecordVisibility visibility)
    {
        Visibility = visibility;
        Touch();
    }

    private void Touch()
    {
        ModifiedUtc = DateTimeOffset.UtcNow;
    }
}
