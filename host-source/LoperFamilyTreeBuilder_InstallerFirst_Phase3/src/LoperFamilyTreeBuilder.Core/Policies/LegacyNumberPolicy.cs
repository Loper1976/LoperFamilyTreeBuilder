namespace LoperFamilyTreeBuilder.Core.Policies;

public static class LegacyNumberPolicy
{
    public static bool IsExactMatch(string storedValue, string proposedValue)
    {
        return string.Equals(
            storedValue,
            proposedValue,
            StringComparison.Ordinal);
    }

    public static void EnsureUnchanged(string storedValue, string proposedValue)
    {
        if (!IsExactMatch(storedValue, proposedValue))
        {
            throw new InvalidOperationException(
                "Robert J. Loper Legacy Numbers are immutable historical data and cannot be changed, normalized, or reformatted.");
        }
    }

    public static string PreserveExact(string historicalValue)
    {
        if (string.IsNullOrEmpty(historicalValue))
        {
            throw new ArgumentException(
                "A Legacy Number cannot be empty.",
                nameof(historicalValue));
        }

        // Return the exact string that was supplied.
        return historicalValue;
    }
}
