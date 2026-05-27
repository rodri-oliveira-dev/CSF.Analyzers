// Stubs mínimos para que ARCH013 detecte APIs parecidas com Moq
// sem referenciar o pacote real Moq.
//
// IMPORTANTE: estes NÃO são implementações de produção.

namespace Moq;

public sealed class Mock<T>
{
}

public static class It
{
    public static T IsAny<T>() => default!;
}
