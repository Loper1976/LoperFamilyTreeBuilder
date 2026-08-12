namespace LoperFamilyTreeBuilder.Core.Entities;

public enum FamilyUnionType
{
    Marriage = 0,
    Partnership = 1,
    CommonLaw = 2,
    Other = 3
}

public enum FamilyUnionStatus
{
    Active = 0,
    Divorced = 1,
    Separated = 2,
    Widowed = 3,
    Historical = 4
}

public sealed class FamilyUnion
{
    private FamilyUnion() { }

    public FamilyUnion(Guid person1Id, Guid person2Id, FamilyUnionType unionType)
    {
        if (person1Id == Guid.Empty || person2Id == Guid.Empty)
            throw new ArgumentException("Both people are required.");
        if (person1Id == person2Id)
            throw new ArgumentException("A person cannot form a union with themselves.");

        Id = Guid.NewGuid();
        Person1Id = person1Id;
        Person2Id = person2Id;
        UnionType = unionType;
        Status = FamilyUnionStatus.Active;
        CreatedUtc = DateTimeOffset.UtcNow;
        ModifiedUtc = CreatedUtc;
    }

    public Guid Id { get; private set; }
    public Guid Person1Id { get; private set; }
    public Guid Person2Id { get; private set; }
    public FamilyUnionType UnionType { get; private set; }
    public FamilyUnionStatus Status { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string PlaceText { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public string SourceCitation { get; private set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; private set; }
    public DateTimeOffset ModifiedUtc { get; private set; }

    public void Update(FamilyUnionStatus status, DateOnly? startDate, DateOnly? endDate, string? placeText, string? notes, string? sourceCitation)
    {
        if (startDate.HasValue && endDate.HasValue && endDate < startDate)
            throw new ArgumentException("Union end date cannot precede its start date.");
        Status = status;
        StartDate = startDate;
        EndDate = endDate;
        PlaceText = (placeText ?? string.Empty).Trim();
        Notes = notes ?? string.Empty;
        SourceCitation = sourceCitation ?? string.Empty;
        ModifiedUtc = DateTimeOffset.UtcNow;
    }
}
