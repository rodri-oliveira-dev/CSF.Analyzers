namespace CSF.Analyzers.Common.Common;

internal static class WildcardPatternMatcher
{
    public static bool Matches(string value, string pattern, StringComparison comparison)
    {
        var valueIndex = 0;
        var patternIndex = 0;
        var starIndex = -1;
        var matchIndex = 0;

        while (valueIndex < value.Length)
        {
            if (patternIndex < pattern.Length
                && AreEqual(value[valueIndex], pattern[patternIndex], comparison))
            {
                valueIndex++;
                patternIndex++;
                continue;
            }

            if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                starIndex = patternIndex++;
                matchIndex = valueIndex;
                continue;
            }

            if (starIndex != -1)
            {
                patternIndex = starIndex + 1;
                valueIndex = ++matchIndex;
                continue;
            }

            return false;
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }

    private static bool AreEqual(char left, char right, StringComparison comparison)
    {
        return comparison switch
        {
            StringComparison.Ordinal => left == right,
            StringComparison.OrdinalIgnoreCase => char.ToUpperInvariant(left) == char.ToUpperInvariant(right),
            _ => string.Equals(left.ToString(), right.ToString(), comparison),
        };
    }
}
