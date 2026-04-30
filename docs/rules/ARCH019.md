# ARCH019: Evite Authorize e AllowAnonymous no mesmo endpoint

## Objetivo

Detectar endpoints ASP.NET que combinam metadados de autorizacao conflitantes, como `[Authorize]` e `[AllowAnonymous]`, na mesma action ou na composicao entre controller e action.

`[AllowAnonymous]` tem precedencia efetiva sobre autorizacao em cenarios comuns do ASP.NET Core. Isso pode fazer uma action parecer protegida por `[Authorize]`, mas continuar acessivel anonimamente por causa de metadado declarado em outro nivel. Mesmo quando a exposicao anonima e intencional, a combinacao deve ser revisada explicitamente pelo time.

## Codigo nao conforme

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

## Codigo conforme

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

## Heuristica

O analyzer usa analise semantica e reconhece apenas simbolos de `Microsoft.AspNetCore.Authorization`:

- `AuthorizeAttribute`;
- `AllowAnonymousAttribute`.

A regra reporta quando encontra:

- method/action com `[Authorize]` e `[AllowAnonymous]` diretamente no mesmo metodo;
- controller com `[AllowAnonymous]` e action com `[Authorize]`;
- controller com `[Authorize]` e action com `[AllowAnonymous]`;
- cadeia de Minimal API com `RequireAuthorization()` e `AllowAnonymous()` no mesmo endpoint mapeado por `MapGet`, `MapPost`, `MapPut`, `MapPatch`, `MapDelete` ou `MapMethods`.

Para reduzir falsos positivos, a regra ignora:

- endpoints apenas com `[Authorize]`;
- endpoints apenas com `[AllowAnonymous]`;
- controller `[Authorize]` com action que apenas herda a protecao;
- atributos customizados com os mesmos nomes fora do namespace ASP.NET Core;
- metodos customizados chamados `RequireAuthorization` ou `AllowAnonymous` fora das extensoes de Minimal API reconhecidas;
- casos em que nao ha metadado suficiente para concluir com seguranca.

## Configuracao

Esta regra nao expoe opcoes customizadas de `.editorconfig` na primeira versao.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH019.severity = warning
```

## Relacao com analyzers nativos

O ASP.NET Core possui a regra nativa `ASP0026`, que alerta quando `[Authorize]` pode ser sobrescrito por `[AllowAnonymous]` declarado mais longe. `ARCH019` existe para manter uma convencao arquitetural propria do pacote, com categoria `Security`, mensagem e documentacao alinhadas ao projeto, alem de cobrir de forma conservadora cadeias de Minimal API quando os dois metadados aparecem no mesmo endpoint.

## Limitacoes conhecidas

- A regra nao tenta reimplementar toda a ordenacao interna de metadados do ASP.NET Core; ela foca combinacoes claras e revisaveis.
- Minimal APIs sao analisadas apenas quando `RequireAuthorization()` e `AllowAnonymous()` aparecem no mesmo encadeamento estatico de chamada.
- Metadados aplicados indiretamente por filtros, conventions, grupos ou variaveis fora do chain analisado nao sao inferidos nesta versao.

## Impacto esperado

- Menos risco de endpoints ficarem anonimos por engano.
- Revisao explicita de excecoes publicas em controllers protegidos.
- Politica de autorizacao mais facil de auditar em controllers e Minimal APIs.
