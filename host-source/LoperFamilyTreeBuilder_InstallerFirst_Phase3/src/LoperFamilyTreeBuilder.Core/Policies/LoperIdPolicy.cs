namespace LoperFamilyTreeBuilder.Core.Policies;

public static class LoperIdPolicy
{
    public const string Prefix = "LOPER-";

    public static string Format(long sequence)
    {
        if (sequence < 1)
            throw new ArgumentOutOfRangeException(nameof(sequence));

        return $"{Prefix}{sequence:D6}";
    }
}
