# ARCH015: Prohibit verbs in HTTP routes

## Objective

Detect command-like verbs in literal HTTP route segments declared with MVC/Web API attributes and Minimal APIs.

The rule focuses only on the route path. It does not validate whether the endpoint is RESTful and does not inspect controller names, action names, method names or the HTTP method itself.

## Non-compliant Code

```csharp
[HttpGet("get/{id}")]
public IActionResult GetById() => Ok();

[HttpPost("create")]
public IActionResult Create() => Ok();

[Route("customers/create")]
public IActionResult CreateCustomer() => Ok();

[HttpPut("orders/{id}/cancel")]
public IActionResult CancelOrder() => Ok();

app.MapPost("/customers/create", () => Results.Ok());
app.MapGet("/orders/get/{id}", () => Results.Ok());
```

With `route_language = pt-BR`:

```csharp
app.MapPost("/apolices/emitir", () => Results.Ok());
```

With `route_language = en-US`:

```csharp
app.MapPost("/policies/issue", () => Results.Ok());
```

## Compliant Code

```csharp
[HttpGet("{id}")]
public IActionResult GetById() => Ok();

[HttpPost("")]
public IActionResult Create() => Ok();

[Route("customers")]
public IActionResult ListCustomers() => Ok();

[HttpPut("orders/{id}")]
public IActionResult UpdateOrder() => Ok();

app.MapGet("/customers/{id}", () => Results.Ok());
app.MapPost("/orders", () => Results.Ok());
app.MapGet("/orders/{id}/items", () => Results.Ok());
app.MapGet("/posts/{id}", () => Results.Ok());
app.MapGet("/approval-status/{id}", () => Results.Ok());
app.MapGet("/created-at/{id}", () => Results.Ok());
```

## Configuration

`route_language` selects the native verb list:

```ini
[*.cs]
dotnet_diagnostic.ARCH015.route_language = pt-BR
```

Accepted values are:

- `en-US`
- `pt-BR`

When `route_language` is missing, the rule uses `en-US`. When it has an invalid value, the rule also falls back to `en-US` and does not fail the build just because the configuration is invalid.

`additional_verbs` adds team-specific verbs to the native list. The value must be a JSON string array:

```ini
[*.cs]
dotnet_diagnostic.ARCH015.route_language = en-US
dotnet_diagnostic.ARCH015.additional_verbs = ["approve", "reject", "recalculate"]
```

```ini
[*.cs]
dotnet_diagnostic.ARCH015.route_language = pt-BR
dotnet_diagnostic.ARCH015.additional_verbs = ["ativar", "inativar", "recalcular"]
```

Additional verbs are trimmed, empty entries are ignored, and comparison is case-insensitive. If the JSON array is malformed, the rule ignores `additional_verbs` and uses only the native verbs for the configured language.

## Native Verb Lists

`en-US`:

```text
create, update, change, delete, remove, get, fetch, find, search, list, issue,
cancel, approve, reject, validate, process, recalculate, generate, send, resend,
import, export
```

`pt-BR`:

```text
criar, atualizar, alterar, excluir, deletar, remover, buscar, obter, consultar,
listar, emitir, cancelar, aprovar, reprovar, validar, processar, recalcular,
gerar, enviar, reenviar, importar, exportar
```

The lists are intentionally conservative. Ambiguous terms are better configured by each team through `additional_verbs`.

## Heuristic

The analyzer:

- splits the path by `/`;
- ignores query strings;
- evaluates only literal route strings;
- ignores empty segments;
- ignores route parameters such as `{id}`, `{id:int}` and `{*path}`;
- ignores tokens such as `[controller]` and `[action]`;
- ignores segments with placeholders;
- ignores version prefixes such as `v1`, `v2` and `api/v1`;
- detects a verb when the full segment is a known verb;
- detects known verbs in command-like kebab-case, snake_case and camelCase segments such as `create-order`, `create_order`, `createOrder`, `emitir-apolice`, `emitir_apolice` and `emitirApolice`;
- does not report substrings inside larger words, such as `createdAt`, `approvalStatus` or `orderProcessingStatus`.

## Known Limitations

- Only literal route paths are analyzed. Routes built through variables, constants from another compilation unit or string interpolation are ignored.
- The rule does not validate REST modeling or resource naming beyond detecting configured verbs.
- The JSON parser for `additional_verbs` supports JSON string arrays and common escaped characters, but intentionally ignores malformed values instead of reporting a separate configuration diagnostic.
- The rule reports one diagnostic per route declaration, using the first problematic segment found.

## Expected Impact

- More consistent resource-oriented route paths.
- Lower noise than broad REST linters because the rule only checks conservative verb lists and literal segments.
- Teams can tune local language and domain-specific command words through `.editorconfig`.
