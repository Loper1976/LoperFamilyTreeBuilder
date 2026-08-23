namespace LoperFamilyTreeBuilder.Core.Entities;

public sealed class Person
{
    private readonly List<PersonIdentifier> _identifiers = new();
    private readonly List<BranchMembership> _branchMemberships = new();

    private Person()
    {
    }

    public Person(string givenName, string surname)
    {
        Id = Guid.NewGuid();
        GivenName = givenName ?? string.Empty;
        Surname = surname ?? string.Empty;
        CreatedUtc = DateTimeOffset.UtcNow;
        ModifiedUtc = CreatedUtc;
    }

    public Guid Id { get; private set; }

    public string GivenName { get; private set; } = string.Empty;

    public string MiddleName { get; private set; } = string.Empty;

    public string Surname { get; private set; } = string.Empty;

    public string Suffix { get; private set; } = string.Empty;

    public DateOnly? BirthDate { get; private set; }

    public DateOnly? DeathDate { get; private set; }

    public bool IsLiving { get; private set; } = true;

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset ModifiedUtc { get; private set; }

    public IReadOnlyCollection<PersonIdentifier> Identifiers => _identifiers.AsReadOnly();

    public IReadOnlyCollection<BranchMembership> BranchMemberships => _branchMemberships.AsReadOnly();

    public string DisplayName
    {
        get
        {
            var parts = new[]
            {
                GivenName,
                MiddleName,
                Surname,
                Suffix
            }
            .Where(value => !string.IsNullOrWhiteSpace(value));

            return string.Join(" ", parts);
        }
    }

    public void UpdateName(string givenName, string middleName, string surname, string suffix)
    {
        GivenName = givenName ?? string.Empty;
        MiddleName = middleName ?? string.Empty;
        Surname = surname ?? string.Empty;
        Suffix = suffix ?? string.Empty;
        Touch();
    }

    public void SetBirthDate(DateOnly? birthDate)
    {
        BirthDate = birthDate;
        Touch();
    }

    public void SetDeathDate(DateOnly? deathDate)
    {
        DeathDate = deathDate;

        if (deathDate.HasValue)
        {
            IsLiving = false;
        }

        Touch();
    }

    public void SetLivingStatus(bool isLiving)
    {
        IsLiving = isLiving;
        Touch();
    }

    public PersonIdentifier AddLegacyNumber(string exactHistoricalValue)
    {
        var existing = _identifiers.SingleOrDefault(identifier =>
            identifier.IdentifierType == PersonIdentifierType.LegacyNumber);

        if (existing is not null)
        {
            throw new InvalidOperationException(
                "This person already has a Robert J. Loper Legacy Number. Protected Legacy Numbers cannot be replaced.");
        }

        var identifier = PersonIdentifier.CreateLegacyNumber(Id, exactHistoricalValue);
        _identifiers.Add(identifier);
        Touch();
        return identifier;
    }

    public PersonIdentifier AddIdentifier(
        PersonIdentifierType identifierType,
        string value,
        bool isProtected = false)
    {
        if (identifierType == PersonIdentifierType.LegacyNumber)
        {
            return AddLegacyNumber(value);
        }

        if (identifierType == PersonIdentifierType.LoperId)
        {
            return AddLoperId(value);
        }

        var identifier = PersonIdentifier.Create(Id, identifierType, value, isProtected);
        _identifiers.Add(identifier);
        Touch();
        return identifier;
    }

    public PersonIdentifier AddLoperId(string value)
    {
        if (_identifiers.Any(identifier =>
            identifier.IdentifierType == PersonIdentifierType.LoperId))
        {
            throw new InvalidOperationException(
                "This person already has a protected LOPER ID.");
        }

        var identifier = PersonIdentifier.CreateLoperId(Id, value);
        _identifiers.Add(identifier);
        Touch();
        return identifier;
    }

    private void Touch()
    {
        ModifiedUtc = DateTimeOffset.UtcNow;
    }
}
