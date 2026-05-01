# ARCH027: Evite dependencias de infraestrutura em camadas core

## Objetivo

Detectar dependencias diretas de frameworks ou adaptadores de infraestrutura em namespaces configurados como camadas core, normalmente dominio e aplicacao.

Em uma arquitetura hexagonal/clean architecture, codigo de dominio e aplicacao deve depender de abstracoes proprias. Detalhes como EF Core, ASP.NET Core, Redis, PostgreSQL ou clientes HTTP concretos devem ficar em adapters, Infrastructure, Api ou composition root, conforme a politica do projeto.

## Codigo nao conforme

```csharp
using Microsoft.EntityFrameworkCore;

namespace Billing.Domain;

public sealed class Invoice
{
    private readonly DbContext _dbContext;
}
```

Tambem ha diagnostico quando o tipo e referenciado diretamente, mesmo sem `using`:

```csharp
namespace Billing.Application;

public sealed class CustomerQuery
{
    private readonly Npgsql.NpgsqlConnection _connection;
}
```

## Codigo conforme

```csharp
namespace Billing.Application;

public interface IInvoiceReadStore
{
    Task<Invoice?> FindAsync(Guid id, CancellationToken cancellationToken);
}
```

```csharp
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Persistence;

public sealed class InvoiceDbContext : DbContext
{
}
```

## Configuracao

A regra e configuravel por `.editorconfig`. Os valores sao listas separadas por `;` e aceitam `*` como wildcard. Quando houver mais de um item, coloque o valor entre aspas para preservar os `;` no `.editorconfig`.

```ini
[*.cs]
dotnet_diagnostic.ARCH027.core_namespace_patterns = "*.Domain;*.Application"
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = "Microsoft.EntityFrameworkCore;Microsoft.AspNetCore;StackExchange.Redis;Npgsql"
dotnet_diagnostic.ARCH027.allowed_namespace_patterns =
dotnet_diagnostic.ARCH027.ignore_tests = true
```

Para projetos que tratam `HttpClient` como detalhe de infraestrutura em Application:

```ini
[*.cs]
dotnet_diagnostic.ARCH027.core_namespace_patterns = Company.Product.Application
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = "System.Net.Http;Microsoft.EntityFrameworkCore;Npgsql"
```

Para permitir uma abstracao especifica de framework em core:

```ini
[*.cs]
dotnet_diagnostic.ARCH027.allowed_namespace_patterns = Microsoft.AspNetCore.Http
```

Valores ausentes usam uma configuracao conservadora:

- core: `*.Domain;*.Application`
- proibidos: `Microsoft.EntityFrameworkCore;Microsoft.AspNetCore;StackExchange.Redis;Npgsql`
- permitidos: vazio
- testes ignorados: `true`

Listas configuraveis sao normalizadas e possuem limites defensivos de quantidade e tamanho para evitar custo excessivo durante build/IDE. Entradas vazias, duplicadas ou acima do limite sao ignoradas.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH027.severity = warning
```

## Heuristica

O analyzer reporta quando um arquivo em namespace core configurado contem:

- `using` para namespace proibido;
- referencia semantica a tipo cujo namespace corresponde a um padrao proibido, inclusive nomes totalmente qualificados.

Um padrao sem `*` tambem cobre subnamespaces. Por exemplo, `Microsoft.AspNetCore` cobre `Microsoft.AspNetCore.Mvc`.

`allowed_namespace_patterns` tem precedencia sobre `forbidden_namespace_patterns`, permitindo excecoes pequenas sem liberar todo o framework.

Com `ignore_tests = true`, a regra ignora namespaces e paths de teste comuns, alem de contextos de teste reconhecidos por atributos xUnit, NUnit ou MSTest nas referencias disponiveis.

## Limitacoes conhecidas

- A regra nao infere dependencias transitivas vindas de outros assemblies; ela analisa apenas imports e tipos usados no codigo fonte.
- Aliases de `using` nao sao reportados nesta versao.
- A classificacao de camadas depende dos namespaces configurados. Projetos com nomes diferentes devem ajustar `core_namespace_patterns`.
- `System.Net.Http` nao e proibido por padrao, porque alguns projetos permitem interfaces ou adapters finos na aplicacao. Adicione-o a `forbidden_namespace_patterns` quando essa for a politica do time.

## Impacto esperado

- Mantem dominio e aplicacao isolados de detalhes de infraestrutura.
- Ajuda a preservar inversao de dependencia em arquiteturas hexagonais/clean architecture.
- Torna excecoes arquiteturais explicitas e revisaveis via `.editorconfig`.
