using Microsoft.CodeAnalysis.Diagnostics;

using Swa.Analyzers.Common.Common;

namespace Swa.Analyzers.Tests.Common;

public sealed class AnalyzerConfigOptionReaderTests
{
    private const string BooleanOption = "dotnet_diagnostic.TEST.boolean";

    [Fact]
    public void ReadBooleanOption_returns_default_when_option_is_absent()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        var value = AnalyzerConfigOptionReader.ReadBooleanOption(options, BooleanOption, defaultValue: true);

        Assert.True(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ReadBooleanOption_returns_default_when_option_is_empty(string configuredValue)
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [BooleanOption] = configuredValue,
        });

        var value = AnalyzerConfigOptionReader.ReadBooleanOption(options, BooleanOption, defaultValue: true);

        Assert.True(value);
    }

    [Fact]
    public void ReadBooleanOption_returns_default_when_option_is_invalid()
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [BooleanOption] = "maybe",
        });

        var value = AnalyzerConfigOptionReader.ReadBooleanOption(options, BooleanOption, defaultValue: false);

        Assert.False(value);
    }

    [Theory]
    [InlineData("TRUE", true)]
    [InlineData("False", false)]
    [InlineData(" true ", true)]
    public void ReadBooleanOption_accepts_bool_casing_supported_by_try_parse(string configuredValue, bool expected)
    {
        var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [BooleanOption] = configuredValue,
        });

        var value = AnalyzerConfigOptionReader.ReadBooleanOption(options, BooleanOption, defaultValue: !expected);

        Assert.Equal(expected, value);
    }

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly IReadOnlyDictionary<string, string> _values;

        public TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values)
        {
            _values = values;
        }

        public override bool TryGetValue(string key, out string value)
        {
            return _values.TryGetValue(key, out value!);
        }
    }
}
