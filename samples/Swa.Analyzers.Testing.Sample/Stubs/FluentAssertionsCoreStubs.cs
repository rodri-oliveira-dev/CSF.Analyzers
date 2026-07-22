// Stubs mínimos para que TST002 detecte APIs parecidas com FluentAssertions
// sem referenciar o pacote real FluentAssertions.
//
// IMPORTANTE: estes NÃO são implementações de produção.

namespace FluentAssertions;

public static class AssertionExtensions
{
    public static ObjectAssertions Should(this object? subject) => new(subject);
}

public sealed class ObjectAssertions
{
    private readonly object? _subject;

    public ObjectAssertions(object? subject)
    {
        _subject = subject;
    }

    public void NotBeNullOrEmpty()
    {
    }

    public void BeOfType<T>()
    {
    }

    // TST002 mira chamadas BeEquivalentTo + Excluding* dentro do delegate de opções.
    public void BeEquivalentTo(
        object? expected,
        Func<global::FluentAssertions.Equivalency.EquivalencyAssertionOptions, global::FluentAssertions.Equivalency.EquivalencyAssertionOptions>? options = null)
    {
        _ = expected;
        _ = options;
    }
}
