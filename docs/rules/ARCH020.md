# ARCH020: Exija autorizacao explicita em endpoints HTTP

## Objetivo

Garantir que cada endpoint HTTP declare uma decisao explicita de autorizacao: protegido por `[Authorize]`/`RequireAuthorization()` ou intencionalmente publico por `[AllowAnonymous]`/`AllowAnonymous()`.

A regra evita endpoints novos sem metadado de autorizacao por esquecimento, mas e conservadora para nao bloquear endpoints tecnicos comuns.

## Codigo nao conforme

```csharp
using Microsoft.AspNetCore.Mvc;

public sealed class OrdersController : ControllerBase
{
    [HttpGet("orders")]
    public IActionResult Get() => Ok();
}

app.MapGet("/orders", () => Results.Ok());
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

public sealed class LoginController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login() => Ok();
}

app.MapGet("/orders", () => Results.Ok())
    .RequireAuthorization();

app.MapPost("/login", () => Results.Ok())
    .AllowAnonymous();
```

## Configuracao

`allowed_routes` permite rotas publicas tecnicas sem metadado explicito. O valor deve ser um array JSON de strings. Rotas sao comparadas sem diferenciar maiusculas de minusculas; valores terminados em `*` funcionam como prefixo:

```ini
[*.cs]
dotnet_diagnostic.ARCH020.allowed_routes = ["/internal/status", "/diagnostics/*"]
```

`allowed_methods` permite nomes de actions ou metodos `Map*` sem metadado explicito:

```ini
[*.cs]
dotnet_diagnostic.ARCH020.allowed_methods = ["Ping"]
```

`ignored_namespaces` ignora controllers e Minimal APIs declarados em namespaces inteiros:

```ini
[*.cs]
dotnet_diagnostic.ARCH020.ignored_namespaces = ["Sample.PublicEndpoints"]
```

Se um array JSON estiver malformado, a opcao e ignorada e a regra continua usando apenas os padroes seguros.

## Excecoes padrao

Para reduzir falsos positivos em endpoints tecnicos, a regra ignora rotas literais que contenham estes segmentos:

```text
health, healthz, swagger, metrics, ready, readiness, live, liveness
```

Endpoints tecnicos fora dessa lista devem usar `[AllowAnonymous]`, `.AllowAnonymous()` ou uma excecao configurada em `.editorconfig`.

## Heuristica

O analyzer usa analise semantica e reconhece apenas simbolos ASP.NET Core conhecidos:

- controllers derivados de `Microsoft.AspNetCore.Mvc.ControllerBase` ou `Controller`;
- actions com atributos HTTP de `Microsoft.AspNetCore.Mvc`, como `HttpGetAttribute`, `HttpPostAttribute`, `HttpPutAttribute`, `HttpPatchAttribute`, `HttpDeleteAttribute`, `HttpHeadAttribute`, `HttpOptionsAttribute` ou `RouteAttribute`;
- atributos `AuthorizeAttribute` e `AllowAnonymousAttribute` de `Microsoft.AspNetCore.Authorization`;
- Minimal APIs mapeadas por `MapGet`, `MapPost`, `MapPut`, `MapPatch`, `MapDelete` ou `MapMethods` em `IEndpointRouteBuilder`;
- metadados fluentes `RequireAuthorization()` e `AllowAnonymous()` em `IEndpointConventionBuilder`.

Para reduzir falsos positivos, a regra ignora:

- endpoints que declaram autorizacao ou anonimato no proprio metodo/action;
- actions que herdam `[Authorize]` ou `[AllowAnonymous]` do controller ou de uma base class;
- controllers abstratos;
- atributos e metodos customizados com nomes parecidos, mas fora dos namespaces ASP.NET Core esperados;
- Minimal APIs cuja rota tecnica esta na lista padrao ou em `allowed_routes`;
- namespaces configurados em `ignored_namespaces`.

## Limitacoes conhecidas

- Minimal APIs sao consideradas conformes apenas quando `.RequireAuthorization()` ou `.AllowAnonymous()` aparecem no mesmo encadeamento estatico da chamada `Map*`.
- A regra nao infere autorizacao aplicada por grupos, filtros, conventions, variaveis intermediarias ou extensoes customizadas.
- Actions sem atributo HTTP explicito sao ignoradas nesta versao para evitar diagnosticos em massa sobre controllers convencionais.
- Rotas nao literais sao analisadas apenas para a decisao de autorizacao; excecoes por rota dependem de strings literais.

## Impacto esperado

- Endpoints protegidos e publicos ficam mais faceis de auditar.
- Novos endpoints sem decisao de seguranca explicita aparecem no build.
- Excecoes tecnicas podem ser mantidas em `.editorconfig` sem reduzir a exigencia para endpoints de negocio.
