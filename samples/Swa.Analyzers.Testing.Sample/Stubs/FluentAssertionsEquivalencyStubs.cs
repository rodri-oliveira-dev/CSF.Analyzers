// Stubs mínimos para que TST002 detecte exclusões de equivalência (Excluding*)
// sem referenciar o pacote real FluentAssertions.
//
// IMPORTANTE: estes NÃO são implementações de produção.

namespace FluentAssertions.Equivalency;

public sealed class EquivalencyAssertionOptions
{
    public EquivalencyAssertionOptions Excluding(Func<IMemberInfo, bool> predicate)
    {
        _ = predicate;
        return this;
    }

    public EquivalencyAssertionOptions ExcludingMissingMembers()
    {
        return this;
    }
}

public interface IMemberInfo
{
}
