# ARCH023: Prefira TimeProvider para obter data e hora

## Objetivo

Evitar acesso direto ao relogio do sistema em codigo de dominio, aplicacao e servicos, preferindo `TimeProvider` ou uma abstracao equivalente quando a hora atual participa de regra de negocio.

## Motivacao

Chamadas diretas a `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now` e `DateTimeOffset.UtcNow` acoplam o codigo ao tempo real. Isso torna testes menos deterministicos, dificulta simular vencimentos, janelas de tempo e mudancas de fuso, e pode espalhar decisoes de relogio por classes que deveriam receber essa dependencia.

`TimeProvider` permite injetar uma fonte de tempo controlavel em testes e usar `TimeProvider.System` na composicao da aplicacao.

## Codigo nao conforme

```csharp
public sealed class InvoiceService
{
    public Invoice Create()
    {
        return new Invoice(DateTimeOffset.UtcNow);
    }
}
```

Tambem sao considerados nao conformes:

```csharp
var localNow = DateTime.Now;
var utcNow = DateTime.UtcNow;
var localOffset = DateTimeOffset.Now;
var utcOffset = DateTimeOffset.UtcNow;
```

## Codigo conforme

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

Implementacoes centralizadas de relogio tambem sao aceitas:

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

ARCH023 nao substitui ARCH012.

- ARCH012 trata contratos e declaracoes de tipo, sugerindo `DateTimeOffset` em vez de `DateTime` para reduzir ambiguidade de fuso horario.
- ARCH023 trata obtencao da hora atual, sugerindo uma fonte de tempo injetavel em vez de acesso direto ao relogio do sistema.

Um codigo pode estar correto para ARCH012 por usar `DateTimeOffset` e ainda assim disparar ARCH023 se chamar `DateTimeOffset.UtcNow` diretamente em regra de negocio.

## Configuracao

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH023.severity = warning
```

Namespaces permitidos podem ser configurados quando uma camada de infraestrutura centraliza o relogio:

```ini
[*.cs]
dotnet_diagnostic.ARCH023.allowed_namespaces = ["MyApp.Infrastructure.Time"]
```

Tipos permitidos tambem podem ser configurados:

```ini
[*.cs]
dotnet_diagnostic.ARCH023.allowed_types = ["MachineTimeSource"]
```

Usos simples dentro de argumentos de logging podem ser ignorados quando o time aceitar essa excecao:

```ini
[*.cs]
dotnet_diagnostic.ARCH023.ignore_simple_logging = true
```

As opcoes em formato JSON aceitam arrays de strings e escapes JSON comuns, incluindo unicode escapado. Valores invalidos de configuracao sao ignorados de forma conservadora, sem suprimir diagnosticos.

## Heuristica

O analyzer usa analise semantica e reporta acessos a propriedades estaticas quando todos os pontos abaixo sao verdadeiros:

- o membro acessado e `Now` ou `UtcNow`;
- o tipo do membro e `System.DateTime` ou `System.DateTimeOffset`;
- o codigo nao esta em contexto de teste reconhecido;
- o arquivo nao e `Program.cs`;
- o contexto nao esta em namespace ou tipo permitido por `.editorconfig`;
- o tipo atual nao parece uma implementacao centralizada de relogio, como tipos terminados em `Clock` ou `TimeProvider`, nem deriva de `System.TimeProvider`;
- o uso nao esta em logging simples quando `ignore_simple_logging = true`.

## Limitacoes conhecidas

- A regra nao acompanha fluxo para descobrir se o valor foi recebido de outro metodo ou campo.
- A deteccao de implementacoes de relogio por nome e propositalmente estreita para reduzir falsos positivos.
- A excecao de logging e sintatica/semantica simples: considera chamadas cujo metodo comeca com `Log` e cujo acesso aparece como argumento da chamada.
- `Program.cs` e ignorado para permitir composicao e bootstrap, mas regras de negocio devem continuar recebendo `TimeProvider`.

## Impacto esperado

- Aumenta testabilidade de regras dependentes de data e hora.
- Reduz testes instaveis por dependerem do relogio real.
- Centraliza decisoes sobre tempo local, UTC e simulacao de relogio.
