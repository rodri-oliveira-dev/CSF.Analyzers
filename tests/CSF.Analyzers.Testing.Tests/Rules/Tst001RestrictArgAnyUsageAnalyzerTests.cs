using CSF.Analyzers.Testing.Rules;

namespace CSF.Analyzers.Tests.Rules;

public sealed class Tst001RestrictArgAnyUsageAnalyzerTests
{
    private const string OptInEditorConfig = """
root = true

[*]
dotnet_diagnostic.TST001.severity = warning
""";

    [Fact]
    public async Task Reports_ArgAny_when_used_outside_allowed_convention()
    {
        const string source = """
using System;
using NSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public static class SubstituteExtensions
    {
        public static T Received<T>(this T substitute) where T : class => substitute;
        public static T DidNotReceive<T>(this T substitute) where T : class => substitute;
        public static T DidNotReceiveWithAnyArgs<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(int value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.Received().Do(NSubstitute.Arg.Any<int>());
    }
}
""";

        var expected = Verifier<Tst001RestrictArgAnyUsageAnalyzer>.Diagnostic("TST001")
            .WithSpan(35, 50, 35, 58)
            .WithMessage("Avoid permissive NSubstitute argument matching outside the allowed negative-assertion convention");

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_ArgAny_when_used_in_DidNotReceive_call_chain()
    {
        const string source = """
using System;
using NSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public static class SubstituteExtensions
    {
        public static T DidNotReceive<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(int value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.DidNotReceive().Do(NSubstitute.Arg.Any<int>());
    }
}
""";

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, OptInEditorConfig);
    }

    [Fact]
    public async Task Does_not_report_ArgAny_when_used_in_DidNotReceiveWithAnyArgs_call_chain()
    {
        const string source = """
using System;
using NSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public static class SubstituteExtensions
    {
        public static T DidNotReceiveWithAnyArgs<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(int value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.DidNotReceiveWithAnyArgs().Do(NSubstitute.Arg.Any<int>());
    }
}
""";

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, OptInEditorConfig);
    }

    [Fact]
    public async Task Does_not_report_outside_test_project()
    {
        const string source = """
namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }
}

public sealed class Sample
{
    public void Execute()
    {
        _ = NSubstitute.Arg.Any<int>();
    }
}
""";

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, OptInEditorConfig);
    }

    [Fact]
    public async Task Does_not_report_in_non_test_type_inside_test_project()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }
}

public sealed class Helper
{
    public void Execute()
    {
        _ = NSubstitute.Arg.Any<int>();
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test() { }
}
""";

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, OptInEditorConfig);
    }

    [Fact]
    public async Task Does_not_report_other_ArgAny_methods_when_not_NSubstitute()
    {
        const string source = """
using System;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace CustomSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        _ = CustomSubstitute.Arg.Any<int>();
    }
}
""";

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, OptInEditorConfig);
    }

    [Fact]
    public async Task Reports_ArgAny_when_used_in_DidNotReceive_chain_but_not_as_direct_argument_value()
    {
        const string source = """
using System;
using NSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public static class SubstituteExtensions
    {
        public static T DidNotReceive<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(int value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.DidNotReceive().Do(NSubstitute.Arg.Any<int>() + 1);
    }
}
""";

        var expected = Verifier<Tst001RestrictArgAnyUsageAnalyzer>.Diagnostic("TST001")
            .WithSpan(33, 55, 33, 63);

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_ArgAny_with_alias_in_allowed_chain()
    {
        const string source = """
using System;
using NSubstitute;
using A = NSubstitute.Arg;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public static class SubstituteExtensions
    {
        public static T DidNotReceive<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(int value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.DidNotReceive().Do(A.Any<int>());
    }
}
""";

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, OptInEditorConfig);
    }

    [Fact]
    public async Task Does_not_report_ArgAny_in_allowed_chain_with_conditional_access()
    {
        const string source = """
using System;
using NSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public static class SubstituteExtensions
    {
        public static T DidNotReceive<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(int value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency? substitute = null;
        substitute?.DidNotReceive()?.Do(NSubstitute.Arg.Any<int>());
    }
}
""";

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, OptInEditorConfig);
    }

    [Fact]
    public async Task Reports_ArgAny_when_exception_method_is_lookalike_not_from_NSubstitute()
    {
        const string source = """
using System;
using NSubstitute;
using CustomSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }
}

namespace CustomSubstitute
{
    public static class SubstituteExtensions
    {
        public static T DidNotReceive<T>(this T substitute) where T : class => substitute;
    }
}

public interface IDependency
{
    void Do(int value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        IDependency substitute = null!;
        substitute.DidNotReceive().Do(NSubstitute.Arg.Any<int>());
    }
}
""";

        var expected = Verifier<Tst001RestrictArgAnyUsageAnalyzer>.Diagnostic("TST001")
            .WithSpan(37, 55, 37, 63);

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_ReturnsForAnyArgs_from_NSubstitute()
    {
        const string source = """
using System;
using NSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public readonly struct ConfiguredCall { }

    public static class SubstituteExtensions
    {
        public static ConfiguredCall ReturnsForAnyArgs<T>(this T value, T returnThis, params T[] returnThese) => default;
    }
}

public interface ICalculator
{
    int Add(int left, int right);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        ICalculator calculator = null!;
        calculator.Add(1, 2).ReturnsForAnyArgs(100);
    }
}
""";

        var expected = Verifier<Tst001RestrictArgAnyUsageAnalyzer>.Diagnostic("TST001")
            .WithSpan(35, 30, 35, 47);

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_WhenForAnyArgs_from_NSubstitute()
    {
        const string source = """
using System;
using NSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public readonly struct ConfiguredCall
    {
        public void Do(Action<object> callback) { }
    }

    public static class SubstituteExtensions
    {
        public static ConfiguredCall WhenForAnyArgs<T>(this T substitute, Action<T> substituteCall) where T : class => default;
    }
}

public interface ICalculator
{
    void Add(int left, int right);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        ICalculator calculator = null!;
        calculator.WhenForAnyArgs(x => x.Add(0, 0)).Do(_ => { });
    }
}
""";

        var expected = Verifier<Tst001RestrictArgAnyUsageAnalyzer>.Diagnostic("TST001")
            .WithSpan(38, 20, 38, 34);

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_ReceivedWithAnyArgs_from_NSubstitute()
    {
        const string source = """
using System;
using NSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public static class SubstituteExtensions
    {
        public static T ReceivedWithAnyArgs<T>(this T substitute) where T : class => substitute;
    }
}

public interface ICalculator
{
    void Add(int left, int right);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        ICalculator calculator = null!;
        calculator.ReceivedWithAnyArgs().Add(default, default);
    }
}
""";

        var expected = Verifier<Tst001RestrictArgAnyUsageAnalyzer>.Diagnostic("TST001")
            .WithSpan(33, 20, 33, 39);

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Does_not_report_DidNotReceiveWithAnyArgs_from_NSubstitute()
    {
        const string source = """
using System;
using NSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public static class SubstituteExtensions
    {
        public static T DidNotReceiveWithAnyArgs<T>(this T substitute) where T : class => substitute;
    }
}

public interface ICalculator
{
    void Add(int left, int right);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        ICalculator calculator = null!;
        calculator.DidNotReceiveWithAnyArgs().Add(default, default);
    }
}
""";

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, OptInEditorConfig);
    }

    [Fact]
    public async Task Does_not_report_ReturnsForAll_from_NSubstitute()
    {
        const string source = """
using System;
using NSubstitute.Extensions;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }
}

namespace NSubstitute.Extensions
{
    public static class ReturnsForAllExtensions
    {
        public static void ReturnsForAll<T>(this object substitute, T returnThis) { }
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        object substitute = new();
        substitute.ReturnsForAll("value");
    }
}
""";

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, OptInEditorConfig);
    }

    [Fact]
    public async Task Does_not_report_custom_method_with_same_name()
    {
        const string source = """
using System;
using CustomSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }
}

namespace CustomSubstitute
{
    public static class SubstituteExtensions
    {
        public static T ReturnsForAnyArgs<T>(this T value, T returnThis) => value;
        public static T ReceivedWithAnyArgs<T>(this T substitute) where T : class => substitute;
    }
}

public interface ICalculator
{
    int Add(int left, int right);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        ICalculator calculator = null!;
        calculator.Add(1, 2).ReturnsForAnyArgs(100);
        calculator.ReceivedWithAnyArgs().Add(default, default);
    }
}
""";

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, OptInEditorConfig);
    }

    [Fact]
    public async Task Reports_AnyArgs_with_alias_and_received_extensions_overload()
    {
        const string source = """
using System;
using SE = NSubstitute.SubstituteExtensions;
using NSubstitute.ReceivedExtensions;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public readonly struct ConfiguredCall { }

    public static class SubstituteExtensions
    {
        public static ConfiguredCall ReturnsForAnyArgs<T>(this T value, T returnThis, params T[] returnThese) => default;
    }
}

namespace NSubstitute.ReceivedExtensions
{
    public readonly struct Quantity { }

    public static class ReceivedExtensions
    {
        public static T ReceivedWithAnyArgs<T>(this T substitute, Quantity requiredQuantity) where T : class => substitute;
    }
}

public interface ICalculator
{
    int Add(int left, int right);
    void Store(int value);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        ICalculator calculator = null!;
        SE.ReturnsForAnyArgs(calculator.Add(1, 2), 100);
        calculator.ReceivedWithAnyArgs(default).Store(default);
    }
}
""";

        var returns = Verifier<Tst001RestrictArgAnyUsageAnalyzer>.Diagnostic("TST001")
            .WithSpan(47, 12, 47, 29);
        var received = Verifier<Tst001RestrictArgAnyUsageAnalyzer>.Diagnostic("TST001")
            .WithSpan(48, 20, 48, 39);

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, returns, received);
    }

    [Fact]
    public async Task Reports_ArgAny_with_static_import()
    {
        const string source = """
using System;
using static NSubstitute.Arg;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        _ = Any<int>();
    }
}
""";

        var expected = Verifier<Tst001RestrictArgAnyUsageAnalyzer>.Diagnostic("TST001")
            .WithSpan(22, 13, 22, 16);

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task Reports_multiple_AnyArgs_calls_in_same_expression()
    {
        const string source = """
using System;
using NSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public readonly struct ConfiguredCall { }

    public static class SubstituteExtensions
    {
        public static ConfiguredCall ReturnsForAnyArgs<T>(this T value, T returnThis, params T[] returnThese) => default;
    }
}

public interface ICalculator
{
    int Add(int left, int right);
    int Multiply(int left, int right);
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test()
    {
        ICalculator calculator = null!;
        Accept(
            calculator.Add(1, 2).ReturnsForAnyArgs(100),
            calculator.Multiply(2, 3).ReturnsForAnyArgs(200));
    }

    private static void Accept(NSubstitute.ConfiguredCall first, NSubstitute.ConfiguredCall second) { }
}
""";

        var first = Verifier<Tst001RestrictArgAnyUsageAnalyzer>.Diagnostic("TST001")
            .WithSpan(37, 34, 37, 51);
        var second = Verifier<Tst001RestrictArgAnyUsageAnalyzer>.Diagnostic("TST001")
            .WithSpan(38, 39, 38, 56);

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, first, second);
    }

    [Fact]
    public async Task Does_not_report_AnyArgs_outside_test_context()
    {
        const string source = """
using System;
using NSubstitute;

namespace Xunit
{
    public sealed class FactAttribute : Attribute { }
}

namespace NSubstitute
{
    public static class Arg
    {
        public static T Any<T>() => default!;
    }

    public readonly struct ConfiguredCall { }

    public static class SubstituteExtensions
    {
        public static ConfiguredCall ReturnsForAnyArgs<T>(this T value, T returnThis, params T[] returnThese) => default;
    }
}

public interface ICalculator
{
    int Add(int left, int right);
}

public sealed class Helper
{
    public void Execute()
    {
        ICalculator calculator = null!;
        calculator.Add(1, 2).ReturnsForAnyArgs(100);
    }
}

public sealed class SampleTests
{
    [Xunit.Fact]
    public void Test() { }
}
""";

        await Verifier<Tst001RestrictArgAnyUsageAnalyzer>.VerifyAnalyzerAsync(source, OptInEditorConfig);
    }
}
