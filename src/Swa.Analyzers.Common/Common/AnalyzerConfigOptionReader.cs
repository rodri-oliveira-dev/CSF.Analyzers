using System.Collections.Immutable;

using Microsoft.CodeAnalysis.Diagnostics;

namespace Swa.Analyzers.Common.Common;

internal static class AnalyzerConfigOptionReader
{
    public static bool ReadBooleanOption(
        AnalyzerConfigOptions options,
        string optionName,
        bool defaultValue)
    {
        if (!options.TryGetValue(optionName, out var configuredValue)
            || string.IsNullOrWhiteSpace(configuredValue))
        {
            return defaultValue;
        }

        return bool.TryParse(configuredValue, out var parsedValue)
            ? parsedValue
            : defaultValue;
    }

    public static IEnumerable<string> ReadStringArrayOption(
        AnalyzerConfigOptions options,
        string optionName,
        ImmutableArray<string> defaultValue,
        Func<string, string> normalize)
    {
        if (!options.TryGetValue(optionName, out var configuredValue)
            || !JsonStringArrayOptionParser.TryParse(configuredValue, out var parsedValues))
        {
            return defaultValue;
        }

        return parsedValues
            .Select(normalize)
            .Where(static value => value.Length > 0);
    }
}
