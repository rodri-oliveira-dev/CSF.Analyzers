using NSubstitute;

namespace CSF.Analyzers.SampleApp.Tst001;

internal sealed class ArgAnyOutsideConvention_Invalid
{
    private readonly IMessageSender _sender = Substitute.For<IMessageSender>();

    [Xunit.Fact]
    public void ShouldAvoidArgAny_InGeneralAssertions()
    {
        _sender.Send(Arg.Any<string>());
    }

    [Xunit.Fact]
    public void ShouldAvoidArgAny_InPositiveReceivedAssertions()
    {
        _sender.Received().Send(Arg.Any<string>());
    }

    [Xunit.Fact]
    public void ShouldAvoidReceivedWithAnyArgs_InPositiveAssertions()
    {
        _sender.ReceivedWithAnyArgs().Send(default!);
    }

    [Xunit.Fact]
    public void ShouldAvoidReturnsForAnyArgs_InSetups()
    {
        IMessageFormatter formatter = Substitute.For<IMessageFormatter>();

        formatter.Format("hello").ReturnsForAnyArgs("formatted");
    }

    [Xunit.Fact]
    public void ShouldAvoidWhenForAnyArgs_InSetups()
    {
        _sender.WhenForAnyArgs(sender => sender.Send(default!)).Do(_ => { });
    }

    internal interface IMessageSender
    {
        void Send(string message);
    }

    internal interface IMessageFormatter
    {
        string Format(string message);
    }
}
