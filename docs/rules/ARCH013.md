# ARCH013: Restrinjá frameworks de mock ao NSubstitute

## Objetivo
Detectar e desencorajar o uso de frameworks de mock diferentes de **NSubstitute** (por exemplo **Moq** e **FakeItEasy**) quando a política do projeto padroniza NSubstitute.

## Motivação
Permitir vários frameworks de mock na mesma base de código tende a:

- aumentar a carga cognitiva para desenvolvedores e revisores
- dificultar o reuso de utilitários de teste
- fragmentar convenções (nomenclatura, match de argumentos, estilos de verificacao)
- aumentar o custo de manutenção ao atualizar dependências de teste

Padronizar em um único framework (NSubstitute) mantém os testes mais consistentes.

## Não conforme

### Moq

```csharp
using Moq;

public sealed class Tests
{
    public void Test()
    {
        var mock = new Moq.Mock<IMyService>();
        _ = Moq.It.IsAny<int>();
    }
}
```

### FakeItEasy

```csharp
public sealed class Tests
{
    public void Test()
    {
        var fake = FakeItEasy.A.Fake<IMyService>();
    }
}
```

## Conforme

```csharp
public sealed class Tests
{
    public void Test()
    {
        var substitute = NSubstitute.Substitute.For<IMyService>();
    }
}
```

## Configuração
Esta regra não expõe opções customizadas de `.editorconfig` na primeira versão.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH013.severity = info
```

Versóes futuras podem introduzir uma configuração de allow-list / deny-list (por exemplo, adicionar outros frameworks de mock para detectar).

## Limitações conhecidas
- **O escopo inicial de detecção é intencionalmente restrito**. A versão 1 detecta apenas estes frameworks:
  - Moq (namespace raiz: `Moq`)
  - FakeItEasy (namespace raiz: `FakeItEasy`)
- O analyzer depende de símbolos semânticos e do **namespace raiz** do símbolo referenciado para evitar falsos positivos de APIs parecidas.
- Nenhum code fix é fornecido porque trocar um framework de mock não é determinístico e muitas vezes exige reescrever a lógica do teste.

## Quando não usar
- Você permite intencionalmente vários frameworks de mock (por exemplo, durante um periodo de migração).
- Você mantém bibliotecas compartilhádas destinadas a projetos que usam frameworks de mock diferentes.

Nesses casos, considere suprimir o diagnóstico ou desabilita-lo via `.editorconfig`.

## Impacto esperado
- Testes mais consistentes entre repositórios e times.
- Menos fragmentação em utilitários e convenções de teste.
- Orientacao mais clara para código novo: use NSubstitute.

## Observações sobre falsos positivos / heurísticas
- O analyzer foi desenhado para evitar falsos positivos verificando **namespaces semânticos** (não apenas matching de texto).
- Ele reporta em locais comuns de uso (using directives, invocações, criação de objetos e declarações de tipo), incluindo tipos compostos como genéricos, arrays, tuplas, delegates e tipos anuláveis.
- Ele intencionalmente não reporta dentro do namespace do próprio framework de mock (util para testes de analyzer que criam stubs de APIs de framework em código-fonte).
