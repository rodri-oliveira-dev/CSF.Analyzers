# ARCH025: ILogger<T> deve usar o tipo da classe atual

## Objetivo

Garantir que `ILogger<TCategoryName>` use como categoria o proprio tipo da classe onde o logger está declarado ou injetado.

## Motivacao

O tipo genérico de `ILogger<T>` define a categoria de log usada pelos providers. Quando uma classe injeta `ILogger<OutroTipo>`, os eventos passam a aparecer associados ao componente errado, dificultando filtros, dashboards, alertas e investigação de incidentes.

Manter a categoria alinhada com o tipo que emite o log deixa a observabilidade mais previsível e evita que refactors ou cópias de código contaminem os nomes de categoria.

## Código não conforme

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

## Código conforme

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

Classes genéricas também são aceitas quando a categoria corresponde ao tipo construído da classe:

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

## Configuração

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH025.severity = warning
```

A regra não possui opções próprias.

## Heurística

O analyzer usa análise semântica e reporta `Microsoft.Extensions.Logging.ILogger<TCategoryName>` quando todos os pontos abaixo são verdadeiros:

- o logger aparece em campo, propriedade ou parâmetro de construtor;
- o tipo está dentro de uma classe;
- `TCategoryName` não é o símbolo da classe atual;
- o contexto não é reconhecido como teste por atributos comuns de xUnit, NUnit ou MSTest.

`ILogger` sem tipo genérico não é reportado. Comparações são feitas por símbolo, não por nome textual, para reduzir falsos positivos com namespaces, classes genéricas e tipos aninhados.

## Limitações conhecidas

- A regra cobre apenas declarações diretas de `ILogger<T>` em campos, propriedades e parâmetros de construtor.
- A regra não valida factories customizadas nem categorias criadas manualmente por `ILoggerFactory.CreateLogger`.
- Quando uma categoria diferente for intencional, configure a severidade por arquivo ou trecho conforme a política do projeto.

## Impacto esperado

- Categorias de log alinhadas ao componente que emite os eventos.
- Filtros e dashboards por categoria mais confiáveis.
- Menos ruído causado por cópias de código que mantém `ILogger<T>` apontando para outro tipo.
