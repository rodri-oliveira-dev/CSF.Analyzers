using Swa.Analyzers.Core.Common;

namespace Swa.Analyzers.Tests.Common;

public sealed class WildcardPatternMatcherTests
{
    [Theory]
    [InlineData("Billing.Domain", "*.Domain")]
    [InlineData("Billing.Infrastructure.Persistence", "Billing.*.Persistence")]
    [InlineData("Microsoft.EntityFrameworkCore", "Microsoft.*")]
    public void Matches_preserves_ordinal_wildcard_matches(string value, string pattern)
    {
        Assert.True(WildcardPatternMatcher.Matches(value, pattern, StringComparison.Ordinal));
    }

    [Fact]
    public void Matches_preserves_ordinal_case_sensitivity()
    {
        Assert.False(WildcardPatternMatcher.Matches("Billing.Domain", "*.domain", StringComparison.Ordinal));
    }

    [Fact]
    public void Matches_preserves_ordinal_ignore_case_matching()
    {
        Assert.True(WildcardPatternMatcher.Matches(
            "tests/App.Tests/App.Tests.csproj",
            "*.TESTS.csproj",
            StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Billing.Domain", "*.Application")]
    [InlineData("Billing.Domain", "Billing.*.Application")]
    [InlineData("Billing.Domain", "Billing.Domain.*")]
    public void Matches_preserves_non_matching_edge_cases(string value, string pattern)
    {
        Assert.False(WildcardPatternMatcher.Matches(value, pattern, StringComparison.Ordinal));
    }
}
