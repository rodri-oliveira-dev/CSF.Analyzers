# ARCH012: Prefira DateTimeOffset em vez de DateTime

## Objetivo
Incentivar o uso de `DateTimeOffset` em vez de `DateTime` em declarações de tipo controladas pelo projeto, reduzindo ambiguidade sobre a intenção de fuso horário.

## Motivação
`System.DateTime` é ambíguo: pode representar horário local, UTC ou não especificado, e sua propriedade `Kind` muitas vezes é ignorada ou interpretada incorretamente. Isso causa bugs reais em serialização, persistência e sistemas distribuídos.

`DateTimeOffset` sempre carrega um offset relativo ao UTC, tornando a intenção explícita e eliminando uma classe comum de defeitos relacionados a fuso horário.

## Não conforme

```csharp
using System;
using System.Collections.Generic;

public sealed class Order
{
    // Ambiguous: is this local, UTC, or unspecified?
    public DateTime PlacedAt { get; set; }
}

public sealed class Processor
{
    public void Process(DateTime timestamp) { }

    public DateTime GetTimestamp() => DateTime.UtcNow;

    public IEnumerable<DateTime> GetTimestamps() => [];
}
```

## Conforme

```csharp
using System;
using System.Collections.Generic;

public sealed class Order
{
    // Explicit offset; always unambiguous
    public DateTimeOffset PlacedAt { get; set; }
}

public sealed class Processor
{
    public void Process(DateTimeOffset timestamp) { }

    public DateTimeOffset GetTimestamp() => DateTimeOffset.UtcNow;

    public IEnumerable<DateTimeOffset> GetTimestamps() => [];
}
```

## Configuração
Esta regra não expõe opções customizadas de `.editorconfig` na primeira versão. Versóes futuras podem suportar uma allow-list para nomes de tipos ou namespaces específicos em que `DateTime` é usado intencionalmente.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH012.severity = info
```

## Limitações conhecidas
- O analyzer não reporta `DateTime` quando usado com `var` (por exemplo, `var dt = DateTime.UtcNow;`). Isso é intencional: sem uma anotação de tipo explícita, não há um local claro de declaração para sinalizar.
- `DateTime` dentro de tipos que derivam de `System.Attribute` não é reportado, porque atributos costumam serializar valores por mecanismos do framework que podem exigir `DateTime`.
- `DateTime` em implementações explícitas de interface, implementações implícitas de interface e overrides não é reportado, porque o tipo é imposto por um contrato externo.
- `DateTime` em parâmetros `this` de extension methods não é reportado quando o parâmetro `this` é `DateTime` (por exemplo, `public static void DoWork(this DateTime dt)`), porque o analyzer mira a declaração do parâmetro apenas quando o time controla a escolha do tipo.
- Nenhum code fix é fornecido porque alterar `DateTime` para `DateTimeOffset` pode quebrar chamadores, contratos de serialização ou conversões implícitas.

## Quando não usar
Você pode manter `DateTime` intencionalmente quando:

- Uma API de framework ou contrato externo exige explícitamente `DateTime` (por exemplo, mapeamentos legados de Entity Framework, algumas configurações de serializadores).
- Você precisa de `DateTime` para interoperabilidade com código não gerenciado ou formatos específicos de serialização.
- A base de código tem uma convenção documentada de que `DateTime` sempre é UTC e essa convenção é estritamente aplicada por outros meios.

Nesses casos, suprima o diagnóstico com um comentário claro de justificativa.

## Impacto esperado
- Menos bugs relacionados a fuso horário causados por valores `DateTime` ambíguos.
- Contratos de dados mais claros ao persistir ou transmitir timestamps.
- Corretude maior em sistemas distribuídos em que valores com offset são essenciais.

## Observações sobre falsos positivos / heurísticas
O analyzer é intencionalmente conservador:

- Ignora implementações de interface e overrides porque o tipo é ditado pelo contrato.
- Ignora tipos derivados de atributos porque atributos são fortemente acoplados a serialização em runtime.
- Ignora declarações com `var` para evitar sinalizar usos inferidos em que o desenvolvedor não escolheu explícitamente o tipo.
- Sinaliza `DateTime` dentro de tipos compostos como `DateTime[]`, `DateTime?`, `List<DateTime>`, `Dictionary<string, DateTime>` e tuplas, porque a mesma ambiguidade se propaga ao contrato declarado.
