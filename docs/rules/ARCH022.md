# ARCH022: Evite materialização prematura em consultas

## Objetivo

Evitar que consultas EF Core sejam materializadas em memória antes de filtros, projeções, paginação ou ordenação que poderiam compor a query enviada ao banco.

## Motivacao

Chamadas como `ToList()` e `ToArray()` encerram a composição da consulta. Quando `Where`, `Select`, `Skip`, `Take` ou `OrderBy` aparecem depois da materialização, o trabalho passa a acontecer em memória, normalmente trazendo mais dados do que o necessário e aumentando custo de CPU, memória e rede.

## Código não conforme

```csharp
using Microsoft.EntityFrameworkCore;

public sealed class OrdersQuery
{
    private readonly OrdersDbContext _db;

    public OrdersQuery(OrdersDbContext db)
    {
        _db = db;
    }

    public IEnumerable<Order> Execute()
    {
        return _db.Orders
            .ToList()
            .Where(order => order.IsOpen);
    }
}
```

Também e considerado não conforme quando a materialização assincrona é imediatamente seguida de filtro em memória:

```csharp
var orders = await _db.Orders.ToListAsync();
var openOrders = orders.Where(order => order.IsOpen);
```

## Código conforme

```csharp
using Microsoft.EntityFrameworkCore;

public sealed class OrdersQuery
{
    private readonly OrdersDbContext _db;

    public OrdersQuery(OrdersDbContext db)
    {
        _db = db;
    }

    public List<Order> Execute()
    {
        return _db.Orders
            .Where(order => order.IsOpen)
            .ToList();
    }
}
```

Materializacao no fim da cadeia também é conforme:

```csharp
var orders = _db.Orders.ToList();
```

## Configuração

Esta regra não expõe opções customizadas de `.editorconfig` na primeira versão.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH022.severity = warning
```

Use supressao pontual ou reduza a severidade quando a materialização antes do filtro for intencional, por exemplo para reutilizar uma coleção em memória em multiplas enumeracoes.

## Heurística

O analyzer usa análise semântica e reporta apenas quando todos os pontos abaixo são verdadeiros:

- a cadeia parte de um `Microsoft.EntityFrameworkCore.DbSet<T>`;
- a materialização sincronica e `ToList()` ou `ToArray()`;
- a chamada imediatamente após a materialização e `Where`, `Select`, `Skip`, `Take`, `OrderBy`, `OrderByDescending`, `ThenBy` ou `ThenByDescending`;
- no caso assíncrono, a declaração local usa `await query.ToListAsync()` e o próximo statement usa a variável com um dos operadores listados.

Para reduzir falsos positivos, a regra não reporta quando:

- `ToList()` ou `ToArray()` aparece no fim da cadeia;
- a origem é LINQ to Objects ou uma coleção em memória;
- a origem não pode ser confirmada semanticamente como EF Core;
- o filtro em memória não está imediatamente ligado ao materializador;
- `OrderBy`/`ThenBy` usa comparer explícito, pois mover essa ordenação para o provedor pode mudar a semântica.

## Limitações conhecidas

- A regra não acompanha fluxo entre métodos, campos, propriedades ou statements distantes.
- A regra não tenta provar intenção de cache, multiplas enumeracoes ou alterações semânticas complexas.
- A regra não sugere code fix nem reescreve a consulta automaticamente.
- Pode haver falso negativo quando a consulta vem de uma abstração que retorna `IQueryable<T>` sem expor diretamente `DbSet<T>`.

## Impacto esperado

- Reduz consultas que trazem dados demais para memória.
- Mantem filtros, projeções e paginação no provedor de query quando isso é seguro.
- Evita ruído em coleções locais e em casos onde a regra não consegue confirmar origem EF Core.
