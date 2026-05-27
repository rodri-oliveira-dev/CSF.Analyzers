# ARCH027: Evite dependências de infraestrutura em camadas core

## Objetivo

Detectar dependências diretas de frameworks ou adaptadores de infraestrutura em namespaces configurados como camadas core, normalmente domínio e aplicação.

Em uma arquitetura hexagonal/clean architecture, código de domínio e aplicação deve depender de abstrações próprias. Detalhes como EF Core, ASP.NET Core, Redis, PostgreSQL ou clientes HTTP concretos devem ficar em adapters, Infrastructure, Api ou composition root, conforme a política do projeto.

## Código não conforme

```csharp
using Microsoft.EntityFrameworkCore;

namespace Billing.Domain;

public sealed class Invoice
{
    private readonly DbContext _dbContext;
}
```

Também há diagnóstico quando o tipo é referênciado diretamente, mesmo sem `using`:

```csharp
namespace Billing.Application;

public sealed class CustomerQuery
{
    private readonly Npgsql.NpgsqlConnection _connection;
}
```

## Código conforme

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

## Configuração

A regra é configurável por `.editorconfig`. Os valores são listas separadas por `;` e aceitam `*` como wildcard. Quando houver mais de um item, coloque o valor entre aspas para preservar os `;` no `.editorconfig`.

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

Para permitir uma abstração específica de framework em core:

```ini
[*.cs]
dotnet_diagnostic.ARCH027.allowed_namespace_patterns = Microsoft.AspNetCore.Http
```

Valores ausentes usam uma configuração conservadora:

- core: `*.Domain;*.Application`
- proibidos: `Microsoft.EntityFrameworkCore;Microsoft.AspNetCore;StackExchange.Redis;Npgsql`
- permitidos: vazio
- testes ignorados: `true`

Listas configuráveis são normalizadas e possuem limites defensivos de quantidade e tamanho para evitar custo excessivo durante build/IDE. Entradas vazias, duplicadas ou acima do limite são ignoradas.

### Fallback das opções

- `core_namespace_patterns`: lista de padrões separada por `;`; default `*.Domain;*.Application` quando ausente. Valor configurado vazio resulta em lista vazia. Entradas vazias, duplicadas ou acima do limite são ignoradas. Padrões são comparados com casing exato. JSON não se aplica.
- `forbidden_namespace_patterns`: lista de padrões separada por `;`; default `Microsoft.EntityFrameworkCore;Microsoft.AspNetCore;StackExchange.Redis;Npgsql` quando ausente. Valor configurado vazio resulta em lista vazia. Entradas vazias, duplicadas ou acima do limite são ignoradas. Padrões são comparados com casing exato. JSON não se aplica.
- `allowed_namespace_patterns`: lista de padrões separada por `;`; default vazio. Entradas vazias, duplicadas ou acima do limite são ignoradas. Padrões são comparados com casing exato. JSON não se aplica.
- `ignore_tests`: booleano; default `true`. Valores booleanos aceitam casing variado; valor ausente, vazio ou inválido usa `true`.

O fallback das listas padrão é restritivo quando a opção está ausente. Um valor explicitamente vazio troca a lista por vazio e pode reduzir a análise daquela dimensao.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH027.severity = warning
```

## Heurística

O analyzer reporta quando um arquivo em namespace core configurado contém:

- `using` para namespace proibido;
- referência semântica a tipo cujo namespace corresponde a um padrão proibido, inclusive nomes totalmente qualificados.

Um padrão sem `*` também cobre subnamespaces. Por exemplo, `Microsoft.AspNetCore` cobre `Microsoft.AspNetCore.Mvc`.

`allowed_namespace_patterns` tem precedencia sobre `forbidden_namespace_patterns`, permitindo exceções pequenas sem liberar todo o framework.

Com `ignore_tests = true`, a regra ignora namespaces e paths de teste comuns, alem de contextos de teste reconhecidos por atributos xUnit, NUnit ou MSTest nas referências disponíveis.

## Limitações conhecidas

- A regra não infere dependências transitivas vindas de outros assemblies; ela analisa apenas imports e tipos usados no código fonte.
- Aliases de `using` não são reportados nesta versão.
- A classificacao de camadas depende dos namespaces configurados. Projetos com nomes diferentes devem ajustar `core_namespace_patterns`.
- `System.Net.Http` não é proibido por padrão, porque alguns projetos permitem interfaces ou adapters finos na aplicação. Adicione-o a `forbidden_namespace_patterns` quando essa for a política do time.

## Impacto esperado

- Mantém domínio e aplicação isolados de detalhes de infraestrutura.
- Ajuda a preservar inversão de dependência em arquiteturas hexagonais/clean architecture.
- Torna exceções arquiteturais explicitas e revisáveis via `.editorconfig`.
