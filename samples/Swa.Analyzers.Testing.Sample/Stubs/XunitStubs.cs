// Stubs mínimos para que analyzers escopados a contextos de teste (TST001-TST002)
// possam rodar neste SampleApp sem adicionar dependências externas.
//
// IMPORTANTE: estes NÃO são implementações de produção.

namespace Xunit;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class FactAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class TheoryAttribute : Attribute
{
}
