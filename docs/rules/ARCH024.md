# ARCH024: Evite interpolacao ou concatenação em ILogger

## Objetivo

Preservar logging estruturado em chamadas de `ILogger`, evitando mensagens montadas por interpolacao de string ou concatenação.

## Motivacao

Templates estáticos com propriedades nomeadas permitem que o backend de logs indexe campos como `CustomerId`, agregue eventos e filtre dados sem depender de parsing textual. Interpolacao e concatenação transformam esses valores em uma mensagem pronta antes do pipeline de logging, perdendo estrutura e podendo gerar alocações desnecessárias mesmo quando o nível não está habilitado.

O repositório já ativa regras CA de logging, como `CA1848` e `CA2254`. ARCH024 adiciona uma política própria e explícita do projeto: chamadas aos principais métodos `ILogger.Log*` devem usar template estruturado quando a mensagem inclui valores dinâmicos.

## Código não conforme

```csharp
_logger.LogInformation($"Customer {id} created");
_logger.LogWarning("Customer " + id + " not found");
_logger.LogError($"Error: {ex.Message}");
```

## Código conforme

```csharp
_logger.LogInformation("Customer {CustomerId} created", id);
_logger.LogWarning("Customer {CustomerId} not found", id);
_logger.LogError(ex, "Error while processing customer {CustomerId}", id);
```

Mensagens constantes sem parâmetros também são aceitas:

```csharp
_logger.LogInformation("Customer created");
```

O padrão com `LoggerMessage` também é aceito:

```csharp
private static readonly Action<ILogger, int, Exception?> CustomerCreated =
    LoggerMessage.Define<int>(LogLevel.Information, new EventId(), "Customer {CustomerId} created");
```

## Configuração

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH024.severity = warning
```

A regra não possui opções próprias.

## Heurística

O analyzer usa análise semântica e reporta quando todos os pontos abaixo são verdadeiros:

- a chamada resolve para `Microsoft.Extensions.Logging.LoggerExtensions`;
- o método é `LogTrace`, `LogDebug`, `LogInformation`, `LogWarning`, `LogError` ou `LogCritical`;
- o argumento semântico `message` é uma string interpolada;
- ou o argumento `message` é uma concatenação de string não constante.

Chamadas a métodos com o mesmo nome fora de `ILogger` não são reportadas.

## Limitações conhecidas

- A regra não analisa `string.Format`.
- Concatenacoes compostas apenas por constantes são tratadas como mensagem constante e não são reportadas.
- A regra cobre os métodos de extensao usuais de `ILogger`; chamadas ao método genérico de baixo nível `ILogger.Log<TState>` ficam fora do escopo.

## Impacto esperado

- Mantem propriedades estruturadas nos logs.
- Reduz mensagens dinâmicas que dificultam busca e agregação.
- Reforca um padrão uniforme de observabilidade sem adicionar dependências ao projeto.
