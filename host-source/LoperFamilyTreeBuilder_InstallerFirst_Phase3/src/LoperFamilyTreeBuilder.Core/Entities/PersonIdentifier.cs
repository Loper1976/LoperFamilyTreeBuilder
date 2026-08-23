namespace LoperFamilyTreeBuilder.Core.Entities;

public sealed class PersonIdentifier
{
    private PersonIdentifier()
    {
    }

    private PersonIdentifier(
        Guid personId,
        PersonIdentifierType identifierType,
        string value,
        bool isProtected)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("Identifier value is required.", nameof(value));
        }

        Id = Guid.NewGuid();
        PersonId = personId;
        IdentifierType = identifierType;
        Value = value;
        IsProtected = isProtected;
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid PersonId { get; private set; }

    public PersonIdentifierType IdentifierType { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public bool IsProtected { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public Person Person { get; private set; } = null!;

    public static PersonIdentifier CreateLegacyNumber(Guid personId, string exactHistoricalValue)
    {
        if (string.IsNullOrEmpty(exactHistoricalValue))
        {
            throw new ArgumentException(
                "A Legacy Number cannot be empty.",
                nameof(exactHistoricalValue));
        }

        // Deliberately do not trim, normalize, parse, renumber, or reformat.
        return new PersonIdentifier(
            personId,
            PersonIdentifierType.LegacyNumber,
            exactHistoricalValue,
            isProtected: true);
    }

    public static PersonIdentifier CreateLoperId(Guid personId, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A LOPER ID cannot be empty.", nameof(value));

        return new PersonIdentifier(
            personId,
            PersonIdentifierType.LoperId,
            value,
            isProtected: true);
    }

    public static PersonIdentifier Create(
        Guid personId,
        PersonIdentifierType identifierType,
        string value,
        bool isProtected = false)
    {
        if (identifierType == PersonIdentifierType.LegacyNumber)
        {
            return CreateLegacyNumber(personId, value);
        }

        if (identifierType == PersonIdentifierType.LoperId)
        {
            return CreateLoperId(personId, value);
        }

        return new PersonIdentifier(personId, identifierType, value, isProtected);
    }

    public void ChangeValue(string newValue)
    {
        if (IsProtected)
        {
            throw new InvalidOperationException(
                "This identifier is protected and cannot be changed through normal editing.");
        }

        if (string.IsNullOrEmpty(newValue))
        {
            throw new ArgumentException("Identifier value is required.", nameof(newValue));
        }

        Value = newValue;
    }
}
