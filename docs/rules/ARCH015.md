# ARCH015: Proiba verbos em rotas HTTP

## Objetivo

Detectar verbos em estilo de comando em segmentos literais de rotas HTTP declaradas com atributos MVC/Web API e Minimal APIs.

A regra se concentra apenas no caminho da rota. Ela não valida se o endpoint é RESTful e não inspeciona nomes de controllers, actions, métodos ou o próprio método HTTP.

## Código não conforme

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

Com `route_language = pt-BR`:

```csharp
app.MapPost("/apolices/emitir", () => Results.Ok());
```

Com `route_language = en-US`:

```csharp
app.MapPost("/policies/issue", () => Results.Ok());
```

## Código conforme

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

## Configuração

`route_language` seleciona a lista nativa de verbos:

```ini
[*.cs]
dotnet_diagnostic.ARCH015.route_language = pt-BR
```

Valores aceitos:

- `en-US`
- `pt-BR`

Quando `route_language` está ausente, a regra usa `en-US`. Quando ele tem um valor inválido, a regra também volta para `en-US` e não falha o build apenas porque a configuração é inválida.

`additional_verbs` adiciona verbos específicos do time a lista nativa. O valor deve ser um array JSON de strings:

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

Verbos adicionais são aparados, entradas vazias são ignoradas e a comparação não diferencia maiúsculas de minúsculas. As opcoes em formato JSON aceitam arrays de strings e escapes JSON comuns, incluindo unicode escapado. Se o array JSON estiver malformado, a regra ignora `additional_verbs` e usa apenas os verbos nativos do idioma configurado.

## Listas nativas de verbos

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

As listas são intencionalmente conservadoras. Termos ambíguos devem ser configurados por cada time por meio de `additional_verbs`.

## Heurística

O analyzer:

- divide o caminho por `/`;
- ignora query strings;
- avalia apenas strings literais de rota;
- ignora segmentos vazios;
- ignora parâmetros de rota como `{id}`, `{id:int}` e `{*path}`;
- ignora tokens como `[controller]` e `[action]`;
- ignora segmentos com placeholders;
- ignora prefixos de versão como `v1`, `v2` e `api/v1`;
- detecta um verbo quando o segmento completo é um verbo conhecido;
- detecta verbos conhecidos em segmentos command-like em kebab-case, snake_case e camelCase, como `create-order`, `create_order`, `createOrder`, `emitir-apolice`, `emitir_apolice` e `emitirApolice`;
- não reporta substrings dentro de palavras maiores, como `createdAt`, `approvalStatus` ou `orderProcessingStatus`.

## Limitações conhecidas

- Apenas caminhos literais de rota são analisados. Rotas criadas por variáveis, constantes de outra unidade de compilação ou string interpolation são ignoradas.
- A regra não valida modelagem REST nem nomes de recursos além de detectar verbos configurados.
- O parser JSON de `additional_verbs` suporta arrays JSON de strings e caracteres escapados comuns, incluindo unicode escapado, mas ignora valores malformados intencionalmente em vez de reportar um diagnóstico separado de configuração.
- A regra reporta um diagnóstico por declaração de rota, usando o primeiro segmento problematico encontrado.

## Impacto esperado

- Caminhos de rota mais consistentes e orientados a recursos.
- Menos ruído do que linters REST amplos, porque a regra verifica apenas listas conservadoras de verbos e segmentos literais.
- Times podem ajustar idioma local e palavras de comando específicas do domínio por meio de `.editorconfig`.
