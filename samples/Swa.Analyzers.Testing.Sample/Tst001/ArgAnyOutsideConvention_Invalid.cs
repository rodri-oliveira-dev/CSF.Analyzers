namespace Swa.Analyzers.SampleApp.Tst001;

internal sealed class ArgAnyOutsideConvention_Invalid
{
    // Exemplos intencionais que DEVEM gerar diagnóstico TST001.

    private readonly IMessageSender _sender = NSubstitute.Substitute.For<IMessageSender>();

    [Xunit.Fact]
    public void ShouldAvoidArgAny_InGeneralAssertions()
    {
        // TST001: Arg.Any() fora da convenção permitida.
        _sender.Send(NSubstitute.Arg.Any<string>());
    }

    internal interface IMessageSender
    {
        void Send(string message);
    }
}
