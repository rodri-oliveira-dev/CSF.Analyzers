# ARCH023: Prefira TimeProvider para obter data e hora

## Objetivo

Evitar acesso direto ao relógio do sistema em código de domínio, aplicação e serviços, preferindo `TimeProvider` ou uma abstração equivalente quando a hora atual participa de regra de negócio.

## Motivacao

Chamadas diretas a `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now` e `DateTimeOffset.UtcNow` acoplam o código ao tempo real. Isso torna testes menos determinísticos, dificulta simular vencimentos, janelas de tempo e mudanças de fuso, e pode espalhar decisões de relógio por classes que deveriam receber essa dependência.

`TimeProvider` permite injetar uma fonte de tempo controlável em testes e usar `TimeProvider.System` na composição da aplicação.

## Código não conforme

```csharp
public sealed class InvoiceService
{
    public Invoice Create()
    {
        return new Invoice(DateTimeOffset.UtcNow);
    }
}
```

Também são considerados não conformes:

```csharp
var localNow = DateTime.Now;
var utcNow = DateTime.UtcNow;
var localOffset = DateTimeOffset.Now;
var utcOffset = DateTimeOffset.UtcNow;
```

## Código conforme

```csharp
public sealed class InvoiceService
{
    private readonly TimeProvider _timeProvider;

    public InvoiceService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Invoice Create()
    {
        return new Invoice(_timeProvider.GetUtcNow());
    }
}
```

Implementações centralizadas de relógio também são aceitas:

```csharp
public sealed class SystemClock
{
    public DateTimeOffset GetUtcNow()
    {
        return DateTimeOffset.UtcNow;
    }
}
```

## Relacao com ARCH012

ARCH023 não substitui ARCH012.

- ARCH012 trata contratos e declarações de tipo, sugerindo `DateTimeOffset` em vez de `DateTime` para reduzir ambiguidade de fuso horário.
- ARCH023 trata obtencao da hora atual, sugerindo uma fonte de tempo injetavel em vez de acesso direto ao relógio do sistema.

Um código pode estar correto para ARCH012 por usar `DateTimeOffset` e ainda assim disparar ARCH023 se chamar `DateTimeOffset.UtcNow` diretamente em regra de negócio.

## Configuração

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH023.severity = warning
```

Namespaces permitidos podem ser configurados quando uma camada de infraestrutura centraliza o relógio:

```ini
[*.cs]
dotnet_diagnostic.ARCH023.allowed_namespaces = ["MyApp.Infrastructure.Time"]
```

Tipos permitidos também podem ser configurados:

```ini
[*.cs]
dotnet_diagnostic.ARCH023.allowed_types = ["MachineTimeSource"]
```

Usos simples dentro de argumentos de logging podem ser ignorados quando o time aceitar essa exceção:

```ini
[*.cs]
dotnet_diagnostic.ARCH023.ignore_simple_logging = true
```

As opções em formato JSON aceitam arrays de strings e escapes JSON comuns, incluindo unicode escapado. Valores inválidos de configuração são ignorados de forma conservadora, sem suprimir diagnósticos.

### Fallback das opções

- `allowed_namespaces`: array JSON de strings; default vazio. Namespaces são aparados e comparados com casing exato. Entradas vazias são ignoradas. JSON vazio, inválido ou malformado e ignorado, sem criar exceções.
- `allowed_types`: array JSON de strings; default vazio. Nomes de tipo são aparados e comparados com casing exato. Entradas vazias são ignoradas. JSON vazio, inválido ou malformado e ignorado, sem criar exceções.
- `ignore_simple_logging`: booleano; default `false`. Somente `true` habilita a exceção. Valores booleanos aceitam casing variado; valor ausente, vazio ou inválido usa `false`.

O fallback é restritivo: configuração inválida não suprime diagnósticos.

## Heurística

O analyzer usa análise semântica e reporta acessos a propriedades estáticas quando todos os pontos abaixo são verdadeiros:

- o membro acessado e `Now` ou `UtcNow`;
- o tipo do membro e `System.DateTime` ou `System.DateTimeOffset`;
- o código não está em contexto de teste reconhecido;
- o arquivo não é `Program.cs`;
- o contexto não está em namespace ou tipo permitido por `.editorconfig`;
- o tipo atual não parece uma implementação centralizada de relógio, como tipos terminados em `Clock` ou `TimeProvider`, nem deriva de `System.TimeProvider`;
- o uso não está em logging simples quando `ignore_simple_logging = true`.

## Limitações conhecidas

- A regra não acompanha fluxo para descobrir se o valor foi recebido de outro método ou campo.
- A detecção de implementações de relógio por nome e propositalmente estreita para reduzir falsos positivos.
- A exceção de logging e sintática/semântica simples: considera chamadas cujo método começa com `Log` e cujo acesso aparece como argumento da chamada.
- `Program.cs` é ignorado para permitir composição e bootstrap, mas regras de negócio devem continuar recebendo `TimeProvider`.

## Impacto esperado

- Aumenta testabilidade de regras dependentes de data e hora.
- Reduz testes instaveis por dependerem do relógio real.
- Centraliza decisões sobre tempo local, UTC e simulação de relógio.
