// Stubs mínimos para que ARCH013 detecte APIs parecidas com FakeItEasy
// sem referenciar o pacote real FakeItEasy.
//
// IMPORTANTE: estes NÃO são implementações de produção.

namespace FakeItEasy;

public static class A
{
    public static T Fake<T>() => default!;
}
