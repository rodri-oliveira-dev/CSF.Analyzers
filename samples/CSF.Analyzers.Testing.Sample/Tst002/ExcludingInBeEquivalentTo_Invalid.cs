using FluentAssertions;

namespace CSF.Analyzers.SampleApp.Tst002;

internal sealed class ExcludingInBeEquivalentTo_Invalid
{
    // Exemplos intencionais que DEVEM gerar diagnóstico TST002.

    [Xunit.Fact]
    public void ShouldAvoidExcluding_WhenAssertingEquivalency()
    {
        var actual = new CustomerDto("Ana", "123");
        var expected = new CustomerDto("Ana", "123");

        actual.Should().BeEquivalentTo(
            expected,
            options => options.Excluding(_ => true));
    }

    private sealed record CustomerDto(string Name, string Document);
}
