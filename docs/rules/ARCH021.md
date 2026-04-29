# ARCH021: Prefira AsNoTracking em consultas EF Core somente leitura

## Objetivo

Sugerir `AsNoTracking()` em consultas EF Core materializadas para leitura quando ha evidencia segura de que a entidade nao sera alterada e persistida no mesmo metodo.

## Motivacao

Por padrao, o EF Core rastreia entidades retornadas por consultas. Esse rastreamento e necessario para comandos de escrita, mas adiciona custo de memoria e CPU em fluxos somente leitura. Em consultas de leitura, `AsNoTracking()` reduz esse custo e deixa a intencao mais clara.

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

    public async Task<IReadOnlyList<Order>> ExecuteAsync()
    {
        return await _db.Orders
            .Where(order => order.IsOpen)
            .ToListAsync();
    }
}
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

    public async Task<IReadOnlyList<Order>> ExecuteAsync()
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(order => order.IsOpen)
            .ToListAsync();
    }
}
```

Consultas que optam explicitamente por tracking tambem sao consideradas conformes:

```csharp
var order = await _db.Orders
    .AsTracking()
    .FirstOrDefaultAsync();
```

Metodos de escrita tambem sao ignorados quando a regra encontra alteracao de membro e persistencia no mesmo metodo:

```csharp
var order = await _db.Orders.FirstOrDefaultAsync();
order!.Status = "Processed";
await _db.SaveChangesAsync();
```

## Configuracao

Esta regra nao expoe opcoes customizadas de `.editorconfig` na primeira versao.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH021.severity = warning
```

Suprima pontualmente quando uma consulta precisa manter tracking por uma razao que a heuristica nao consegue inferir. Quando a intencao for tracking, prefira `AsTracking()` para documentar isso no proprio codigo.

## Heuristica

O analyzer usa analise semantica e reporta apenas quando todos os pontos abaixo sao verdadeiros:

- a materializacao e uma chamada EF Core conhecida em `Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions`;
- o metodo chamado e `ToListAsync`, `FirstOrDefaultAsync` ou `SingleOrDefaultAsync`;
- a cadeia da consulta parte de um `Microsoft.EntityFrameworkCore.DbSet<T>`;
- a cadeia nao contem `AsNoTracking()` nem `AsTracking()`;
- o codigo nao esta dentro de contexto de teste reconhecido pelo projeto.

Para reduzir falsos positivos, a regra nao reporta quando:

- o metodo contem chamada a `SaveChanges()` ou `SaveChangesAsync()` e tambem atribuicao a membro/propriedade;
- a regra encontra configuracao global `UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)` no mesmo tipo;
- a consulta nao pode ser confirmada semanticamente como EF Core.

## Limitacoes conhecidas

- A deteccao de configuracao global `NoTracking` e propositalmente estreita: ela considera apenas configuracoes visiveis no mesmo tipo analisado.
- A regra nao acompanha fluxo entre metodos, repositorios, variaveis intermediarias complexas ou configuracoes registradas em outro arquivo.
- A regra nao infere que uma entidade sera modificada em outro metodo chamado posteriormente.
- A regra cobre apenas materializadores assincronos listados na heuristica inicial.
- Pode haver falso negativo quando a consulta vem de uma abstracao que retorna `IQueryable<T>` em vez de expor diretamente `DbSet<T>`.

## Impacto esperado

- Reduz custo de tracking em consultas de leitura.
- Torna a intencao de leitura explicita.
- Evita ruido em comandos de escrita e em consultas que ja declaram uma decisao de tracking.
