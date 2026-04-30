# ARCH025: ILogger<T> deve usar o tipo da classe atual

## Objetivo

Garantir que `ILogger<TCategoryName>` use como categoria o proprio tipo da classe onde o logger esta declarado ou injetado.

## Motivacao

O tipo generico de `ILogger<T>` define a categoria de log usada pelos providers. Quando uma classe injeta `ILogger<OutroTipo>`, os eventos passam a aparecer associados ao componente errado, dificultando filtros, dashboards, alertas e investigacao de incidentes.

Manter a categoria alinhada com o tipo que emite o log deixa a observabilidade mais previsivel e evita que refactors ou copias de codigo contaminem os nomes de categoria.

## Codigo nao conforme

```csharp
using Microsoft.Extensions.Logging;

public sealed class CustomerService
{
    private readonly ILogger<OrderService> _logger;

    public CustomerService(ILogger<OrderService> logger)
    {
        _logger = logger;
    }

    public ILogger<OrderService> Logger => _logger;
}

public sealed class OrderService
{
}
```

## Codigo conforme

```csharp
using Microsoft.Extensions.Logging;

public sealed class CustomerService
{
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ILogger<CustomerService> logger)
    {
        _logger = logger;
    }

    public ILogger<CustomerService> Logger => _logger;
}
```

Classes genericas tambem sao aceitas quando a categoria corresponde ao tipo construido da classe:

```csharp
using Microsoft.Extensions.Logging;

public sealed class Repository<TEntity>
{
    private readonly ILogger<Repository<TEntity>> _logger;

    public Repository(ILogger<Repository<TEntity>> logger)
    {
        _logger = logger;
    }
}
```

## Configuracao

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH025.severity = warning
```

A regra nao possui opcoes proprias.

## Heuristica

O analyzer usa analise semantica e reporta `Microsoft.Extensions.Logging.ILogger<TCategoryName>` quando todos os pontos abaixo sao verdadeiros:

- o logger aparece em campo, propriedade ou parametro de construtor;
- o tipo esta dentro de uma classe;
- `TCategoryName` nao e o simbolo da classe atual;
- o contexto nao e reconhecido como teste por atributos comuns de xUnit, NUnit ou MSTest.

`ILogger` sem tipo generico nao e reportado. Comparacoes sao feitas por simbolo, nao por nome textual, para reduzir falsos positivos com namespaces, classes genericas e tipos aninhados.

## Limitacoes conhecidas

- A regra cobre apenas declaracoes diretas de `ILogger<T>` em campos, propriedades e parametros de construtor.
- A regra nao valida factories customizadas nem categorias criadas manualmente por `ILoggerFactory.CreateLogger`.
- Quando uma categoria diferente for intencional, configure a severidade por arquivo ou trecho conforme a politica do projeto.

## Impacto esperado

- Categorias de log alinhadas ao componente que emite os eventos.
- Filtros e dashboards por categoria mais confiaveis.
- Menos ruido causado por copias de codigo que mantem `ILogger<T>` apontando para outro tipo.
