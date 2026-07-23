using NSubstitute;

namespace CSF.Analyzers.SampleApp.Tst001;

internal sealed class ArgAnyAllowedConvention_Valid
{
    private readonly IMessageSender _sender = Substitute.For<IMessageSender>();

    [Xunit.Fact]
    public void PreferConcreteValues_WhenPossible()
    {
        _sender.Send("hello");
    }

    [Xunit.Fact]
    public void ArgAny_IsAllowed_InDidNotReceiveConvention()
    {
        _sender.DidNotReceive().Send(Arg.Any<string>());
    }

    [Xunit.Fact]
    public void ArgAny_IsAllowed_InDidNotReceiveWithAnyArgsConvention()
    {
        _sender.DidNotReceiveWithAnyArgs().Send(Arg.Any<string>());
    }

    [Xunit.Fact]
    public void DidNotReceiveWithAnyArgs_IsAllowed_AsDeliberateNegativeAssertion()
    {
        _sender.DidNotReceiveWithAnyArgs().Send(default!);
    }

    internal interface IMessageSender
    {
        void Send(string message);
    }
}
