// Stubs mínimos para que TST001 detecte APIs parecidas com NSubstitute
// sem referenciar o pacote real NSubstitute.
//
// IMPORTANTE: estes NÃO são implementações de produção.

namespace NSubstitute;

public static class Substitute
{
    public static T For<T>() where T : class
    {
        return default!;
    }
}

public static class Arg
{
    // TST001 mira este nome de método no tipo NSubstitute.Arg.
    public static T Any<T>()
    {
        return default!;
    }

}

public static class SubstituteExtensions
{
    // TST001 permite Arg.Any() apenas quando a chamada receptora é precedida por um destes métodos.
    public static T DidNotReceive<T>(this T substitute) where T : class
    {
        return substitute;
    }

    public static T DidNotReceiveWithAnyArgs<T>(this T substitute) where T : class
    {
        return substitute;
    }
}
