namespace LoperFamilyTreeBuilder.Core.Validation;

public sealed record TreeIntegrityCheckOptions
{
    public int MinimumPlausibleParentAgeYears { get; init; } = 12;

    public int UnusuallyHighParentAgeYears { get; init; } = 80;

    public int PosthumousBirthGraceDays { get; init; } = 300;

    internal void Validate()
    {
        if (MinimumPlausibleParentAgeYears < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MinimumPlausibleParentAgeYears));
        }

        if (UnusuallyHighParentAgeYears <= MinimumPlausibleParentAgeYears)
        {
            throw new ArgumentOutOfRangeException(
                nameof(UnusuallyHighParentAgeYears),
                "The unusually-high age threshold must be greater than the minimum plausible age threshold.");
        }

        if (PosthumousBirthGraceDays < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PosthumousBirthGraceDays));
        }
    }
}
