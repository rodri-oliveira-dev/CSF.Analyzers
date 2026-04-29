# ARCH004: Exija o nome _sut em testes unitários

## Objetivo
Exigir a convenção de nome `_sut` para o campo principal de *system under test* (SUT) em tipos de teste unitário.

## Motivação
Usar um nome consistente para o principal sujeito sob teste reduz a carga cognitiva ao ler testes:

- `_sut` é fácil de reconhecer rapidamente
- evita discussões sobre nomes de variáveis para o objeto principal sob teste
- torna os padrões de setup de testes mais uniformes em toda a base de código

## Exemplos de código não conforme

```csharp
public sealed class Calculator { }

public sealed class CalculatorTests
{
    private readonly Calculator _calculator = new();

    [Fact]
    public void Adds() { }
}
```

## Exemplos de código conforme

```csharp
public sealed class Calculator { }

public sealed class CalculatorTests
{
    private readonly Calculator _sut = new();

    [Fact]
    public void Adds() { }
}
```

## Configuração
Esta regra não expõe opções customizadas de `.editorconfig` na primeira versão.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH004.severity = info
```

## Limitações conhecidas
- O analyzer é intencionalmente limitado a **projetos de teste** (heurística: a compilação deve referenciar atributos conhecidos de frameworks de teste, como `Xunit.FactAttribute`, `NUnit.Framework.TestAttribute` ou `Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute`).
- O analyzer é intencionalmente conservador para evitar ruído:
  - analisa apenas **tipos de teste** (tipos que contêm pelo menos um método de teste conhecido)
  - reporta apenas quando consegue inferir um único campo candidato claro a SUT
- Heurística de identificação do SUT (comportamento atual):
  1. Inferir o nome esperado do tipo SUT a partir do nome do tipo de teste, removendo um sufixo suportado (`Tests`, `Test`, `Specs`, `Spec`).
     - Exemplo: `OrderServiceTests` -> nome de tipo SUT inferido `OrderService`.
  2. Dentro desse tipo de teste, encontrar campos de instância cujo **nome do tipo** corresponde ao nome de tipo SUT inferido.
  3. Se houver exatamente um desses campos e ele não se chamar `_sut`, reportar `ARCH004`.

## Quando não usar
- Se seu time usa uma convenção diferente de nomenclatura de SUT (por exemplo `sut` ou `subject`).
- Se seus testes envolvem intencionalmente vários sujeitos sob teste igualmente importantes por tipo de teste.

## Impacto esperado
- Código de teste mais uniforme e mais fácil de escanear
- Menos churn de nomenclatura em code reviews

## Observações sobre falsos positivos, heurísticas ou exceções
- Esta regra intencionalmente **não** fornece code fix. Renomear um campo pode exigir atualizar muitas referências e ser disruptivo.
- Se o nome da classe de teste não seguir uma convenção de sufixo suportada, o analyzer permanece silencioso.
