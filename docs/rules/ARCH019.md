# ARCH019: Evite Authorize e AllowAnonymous no mesmo endpoint

## Objetivo

Detectar endpoints ASP.NET que combinam metadados de autorização conflitantes, como `[Authorize]` e `[AllowAnonymous]`, na mesma action ou na composição entre controller e action.

`[AllowAnonymous]` tem precedencia efetiva sobre autorização em cenários comuns do ASP.NET Core. Isso pode fazer uma action parecer protegida por `[Authorize]`, mas continuar acessível anonimamente por causa de metadado declarado em outro nível. Mesmo quando a exposicao anonima e intencional, a combinacao deve ser revisada explicitamente pelo time.

## Código não conforme

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersController : ControllerBase
{
    [Authorize]
    [AllowAnonymous]
    [HttpGet("orders")]
    public IActionResult Get() => Ok();
}

[AllowAnonymous]
public sealed class CustomersController : ControllerBase
{
    [Authorize]
    [HttpGet("customers")]
    public IActionResult Get() => Ok();
}

[Authorize]
public sealed class HealthController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("health")]
    public IActionResult Get() => Ok();
}

app.MapGet("/orders", () => Results.Ok())
    .RequireAuthorization()
    .AllowAnonymous();
```

## Código conforme

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public sealed class OrdersController : ControllerBase
{
    [HttpGet("orders")]
    public IActionResult Get() => Ok();
}

public sealed class HealthController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("health")]
    public IActionResult Get() => Ok();
}

app.MapGet("/orders", () => Results.Ok())
    .RequireAuthorization();

app.MapGet("/health", () => Results.Ok())
    .AllowAnonymous();
```

## Heurística

O analyzer usa análise semântica e reconhece apenas símbolos de `Microsoft.AspNetCore.Authorization`:

- `AuthorizeAttribute`;
- `AllowAnonymousAttribute`.

A regra reporta quando encontra:

- method/action com `[Authorize]` e `[AllowAnonymous]` diretamente no mesmo método;
- controller com `[AllowAnonymous]` e action com `[Authorize]`;
- controller com `[Authorize]` e action com `[AllowAnonymous]`;
- cadeia de Minimal API com `RequireAuthorization()` e `AllowAnonymous()` no mesmo endpoint mapeado por `MapGet`, `MapPost`, `MapPut`, `MapPatch`, `MapDelete` ou `MapMethods`.

Para reduzir falsos positivos, a regra ignora:

- endpoints apenas com `[Authorize]`;
- endpoints apenas com `[AllowAnonymous]`;
- controller `[Authorize]` com action que apenas herda a protecao;
- atributos customizados com os mesmos nomes fora do namespace ASP.NET Core;
- métodos customizados chamados `RequireAuthorization` ou `AllowAnonymous` fora das extensões de Minimal API reconhecidas;
- casos em que não há metadado suficiente para concluir com segurança.

## Configuração

Esta regra não expõe opções customizadas de `.editorconfig` na primeira versão.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH019.severity = warning
```

## Relacao com analyzers nativos

O ASP.NET Core possui a regra nativa `ASP0026`, que alerta quando `[Authorize]` pode ser sobrescrito por `[AllowAnonymous]` declarado mais longe. `ARCH019` existe para manter uma convenção arquitetural própria do pacote, com categoria `Security`, mensagem e documentação alinhadas ao projeto, alem de cobrir de forma conservadora cadeias de Minimal API quando os dois metadados aparecem no mesmo endpoint.

## Limitações conhecidas

- A regra não tenta reimplementar toda a ordenação interna de metadados do ASP.NET Core; ela foca combinações claras e revisáveis.
- Minimal APIs são analisadas apenas quando `RequireAuthorization()` e `AllowAnonymous()` aparecem no mesmo encadeamento estático de chamada.
- Metadados aplicados indiretamente por filtros, conventions, grupos ou variáveis fora do chain analisado não são inferidos nesta versão.

## Impacto esperado

- Menos risco de endpoints ficarem anonimos por engano.
- Revisao explícita de exceções públicas em controllers protegidos.
- Politica de autorização mais facil de auditar em controllers e Minimal APIs.
