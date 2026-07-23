using CSF.Analyzers.Common.Common;

namespace Swa.Analyzers.Tests.Common;

public sealed class JsonStringArrayOptionParserTests
{
    public static TheoryData<string> InvalidJsonValues => new()
    {
        """["unterminated]""",
        """["value",]""",
        """"value"""",
        """["value", 1]""",
        """[{}]""",
        """["value\""",
        """["value"] trailing""",
    };

    [Fact]
    public void Parses_empty_array()
    {
        var parsed = JsonStringArrayOptionParser.TryParse("[]", out var items);

        Assert.True(parsed);
        Assert.Empty(items);
    }

    [Fact]
    public void Parses_single_string()
    {
        var parsed = JsonStringArrayOptionParser.TryParse("""["orders"]""", out var items);

        Assert.True(parsed);
        var item = Assert.Single(items);
        Assert.Equal("orders", item);
    }

    [Fact]
    public void Parses_multiple_strings()
    {
        var parsed = JsonStringArrayOptionParser.TryParse("""["orders", "customers", "invoices"]""", out var items);

        Assert.True(parsed);
        Assert.Collection(
            items,
            item => Assert.Equal("orders", item),
            item => Assert.Equal("customers", item),
            item => Assert.Equal("invoices", item));
    }

    [Fact]
    public void Preserves_strings_with_spaces()
    {
        var parsed = JsonStringArrayOptionParser.TryParse("""[" value with spaces "]""", out var items);

        Assert.True(parsed);
        var item = Assert.Single(items);
        Assert.Equal(" value with spaces ", item);
    }

    [Fact]
    public void Parses_common_escapes()
    {
        var parsed = JsonStringArrayOptionParser.TryParse("""["quote \" backslash \\ slash \/ backspace \b form \f newline \n return \r tab \t"]""", out var items);

        Assert.True(parsed);
        var item = Assert.Single(items);
        Assert.Equal("quote \" backslash \\ slash / backspace \b form \f newline \n return \r tab \t", item);
    }

    [Fact]
    public void Parses_unicode_escape()
    {
        var parsed = JsonStringArrayOptionParser.TryParse("""["\u0041pp"]""", out var items);

        Assert.True(parsed);
        var item = Assert.Single(items);
        Assert.Equal("App", item);
    }

    [Theory]
    [MemberData(nameof(InvalidJsonValues))]
    public void Rejects_invalid_json_values(string value)
    {
        var parsed = JsonStringArrayOptionParser.TryParse(value, out var items);

        Assert.False(parsed);
        Assert.Empty(items);
    }
}
