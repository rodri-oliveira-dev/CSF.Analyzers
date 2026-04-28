# ARCH003: Proiba NotBeNull() em testes

## Objetivo
Detectar o uso de `NotBeNull()` em testes e incentivar asserções mais específicas quando possível.

## Motivação
`NotBeNull()` costuma ser uma asserção fraca: confirma apenas a ausência de `null`, mas geralmente não comúnica *o que* é esperado (tipo, conteúdo, vazio, presença de valor etc.).

Assercoes mais específicas tendem a:

- tornar a intenção do teste mais clara
- produzir mensagens de falha melhores
- reduzir o risco de "asserir pouco demais"

## Exemplos de código não conforme

```csharp
using FluentAssertions;

[Fact]
public void Test()
{
    object? value = GetValue();
    value.Should().NotBeNull();
}
```

## Exemplos de código conforme

```csharp
using FluentAssertions;

[Fact]
public void Test()
{
    string? value = GetValue();
    value.Should().NotBeNullOrEmpty();
}
```

```csharp
using FluentAssertions;

[Fact]
public void Test()
{
    object? value = GetValue();
    value.Should().BeOfType<ExpectedType>();
}
```

## Configuração
Esta regra não expõe opções customizadas de `.editorconfig` na primeira versão.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH003.severity = info
```

## Limitações conhecidas
- O analyzer mira apenas `NotBeNull()` do **FluentAssertions**.
- O analyzer é intencionalmente limitado a **projetos de teste** (heurística: a compilação deve referenciar atributos conhecidos de frameworks de teste, como `Xunit.FactAttribute`, `NUnit.Framework.TestAttribute` ou `Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute`).
- O analyzer reporta quando a invocação está dentro de um método de teste conhecido (por exemplo `[Fact]` / `[Theory]`) **ou** dentro de um *tipo de teste* (um tipo que contém pelo menos um método de teste conhecido).

## Quando não usar
Se seu time padroniza intencionalmente `NotBeNull()` como a única asserção permitida de checagem de nulo, esta regra pode ser rigorosa demais. Prefira ajustar a severidade em vez de desabilitar a regra amplamente.

## Impacto esperado
- Testes mais expressivos
- Menos "asserir pouco demais"
- Melhores mensagens de falha e sinais de depuração

## Observações sobre falsos positivos, heurísticas ou exceções
- Esta regra intencionalmente **não** fornece code fix. Não há substituto universalmente seguro e determinístico para `NotBeNull()`.
- A detecção de projeto de teste é heurística para evitar ruído em projetos que não são de teste. Se um projeto de teste usa um framework diferente da lista embutida, esta regra não será executada.
