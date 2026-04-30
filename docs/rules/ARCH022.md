# ARCH022: Evite materializacao prematura em consultas

## Objetivo

Evitar que consultas EF Core sejam materializadas em memoria antes de filtros, projecoes, paginacao ou ordenacao que poderiam compor a query enviada ao banco.

## Motivacao

Chamadas como `ToList()` e `ToArray()` encerram a composicao da consulta. Quando `Where`, `Select`, `Skip`, `Take` ou `OrderBy` aparecem depois da materializacao, o trabalho passa a acontecer em memoria, normalmente trazendo mais dados do que o necessario e aumentando custo de CPU, memoria e rede.

## Codigo nao conforme

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

Tambem e considerado nao conforme quando a materializacao assincrona e imediatamente seguida de filtro em memoria:

```csharp
var orders = await _db.Orders.ToListAsync();
var openOrders = orders.Where(order => order.IsOpen);
```

## Codigo conforme

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

Materializacao no fim da cadeia tambem e conforme:

```csharp
var orders = _db.Orders.ToList();
```

## Configuracao

Esta regra nao expoe opcoes customizadas de `.editorconfig` na primeira versao.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH022.severity = warning
```

Use supressao pontual ou reduza a severidade quando a materializacao antes do filtro for intencional, por exemplo para reutilizar uma colecao em memoria em multiplas enumeracoes.

## Heuristica

O analyzer usa analise semantica e reporta apenas quando todos os pontos abaixo sao verdadeiros:

- a cadeia parte de um `Microsoft.EntityFrameworkCore.DbSet<T>`;
- a materializacao sincronica e `ToList()` ou `ToArray()`;
- a chamada imediatamente apos a materializacao e `Where`, `Select`, `Skip`, `Take`, `OrderBy`, `OrderByDescending`, `ThenBy` ou `ThenByDescending`;
- no caso assincrono, a declaracao local usa `await query.ToListAsync()` e o proximo statement usa a variavel com um dos operadores listados.

Para reduzir falsos positivos, a regra nao reporta quando:

- `ToList()` ou `ToArray()` aparece no fim da cadeia;
- a origem e LINQ to Objects ou uma colecao em memoria;
- a origem nao pode ser confirmada semanticamente como EF Core;
- o filtro em memoria nao esta imediatamente ligado ao materializador;
- `OrderBy`/`ThenBy` usa comparer explicito, pois mover essa ordenacao para o provedor pode mudar a semantica.

## Limitacoes conhecidas

- A regra nao acompanha fluxo entre metodos, campos, propriedades ou statements distantes.
- A regra nao tenta provar intencao de cache, multiplas enumeracoes ou alteracoes semanticas complexas.
- A regra nao sugere code fix nem reescreve a consulta automaticamente.
- Pode haver falso negativo quando a consulta vem de uma abstracao que retorna `IQueryable<T>` sem expor diretamente `DbSet<T>`.

## Impacto esperado

- Reduz consultas que trazem dados demais para memoria.
- Mantem filtros, projecoes e paginacao no provedor de query quando isso e seguro.
- Evita ruido em colecoes locais e em casos onde a regra nao consegue confirmar origem EF Core.
