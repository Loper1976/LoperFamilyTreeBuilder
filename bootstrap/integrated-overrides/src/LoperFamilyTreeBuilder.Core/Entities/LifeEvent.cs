namespace LoperFamilyTreeBuilder.Core.Entities;

public enum LifeEventType
{
    Birth = 0,
    Marriage = 1,
    Residence = 2,
    Census = 3,
    Education = 4,
    Employment = 5,
    Military = 6,
    Travel = 7,
    Medical = 8,
    Death = 9,
    Burial = 10,
    Custom = 11
}

public sealed class LifeEvent
{
    private LifeEvent() { }

    public LifeEvent(Guid personId, LifeEventType eventType, string title)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("A person is required.", nameof(personId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("An event title is required.", nameof(title));

        Id = Guid.NewGuid();
        PersonId = personId;
        EventType = eventType;
        Title = title.Trim();
        CreatedUtc = DateTimeOffset.UtcNow;
        ModifiedUtc = CreatedUtc;
    }

    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }
    public LifeEventType EventType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsDateApproximate { get; private set; }
    public string OriginalPlaceText { get; private set; } = string.Empty;
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public string Notes { get; private set; } = string.Empty;
    public string SourceCitation { get; private set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; private set; }
    public DateTimeOffset ModifiedUtc { get; private set; }

    public void UpdateChronology(DateOnly? startDate, DateOnly? endDate, bool isDateApproximate)
    {
        if (startDate.HasValue && endDate.HasValue && endDate < startDate)
            throw new ArgumentException("Event end date cannot precede its start date.");
        StartDate = startDate;
        EndDate = endDate;
        IsDateApproximate = isDateApproximate;
        Touch();
    }

    public void UpdateLocation(string? originalPlaceText, decimal? latitude, decimal? longitude)
    {
        if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
        OriginalPlaceText = (originalPlaceText ?? string.Empty).Trim();
        Latitude = latitude;
        Longitude = longitude;
        Touch();
    }

    public void UpdateEvidence(string? notes, string? sourceCitation)
    {
        Notes = notes ?? string.Empty;
        SourceCitation = sourceCitation ?? string.Empty;
        Touch();
    }

    private void Touch() => ModifiedUtc = DateTimeOffset.UtcNow;
}
