# ARCH024: Evite interpolacao ou concatenacao em ILogger

## Objetivo

Preservar logging estruturado em chamadas de `ILogger`, evitando mensagens montadas por interpolacao de string ou concatenacao.

## Motivacao

Templates estaticos com propriedades nomeadas permitem que o backend de logs indexe campos como `CustomerId`, agregue eventos e filtre dados sem depender de parsing textual. Interpolacao e concatenacao transformam esses valores em uma mensagem pronta antes do pipeline de logging, perdendo estrutura e podendo gerar alocacoes desnecessarias mesmo quando o nivel nao esta habilitado.

O repositorio ja ativa regras CA de logging, como `CA1848` e `CA2254`. ARCH024 adiciona uma politica propria e explicita do projeto: chamadas aos principais metodos `ILogger.Log*` devem usar template estruturado quando a mensagem inclui valores dinamicos.

## Codigo nao conforme

```csharp
_logger.LogInformation($"Customer {id} created");
_logger.LogWarning("Customer " + id + " not found");
_logger.LogError($"Error: {ex.Message}");
```

## Codigo conforme

```csharp
_logger.LogInformation("Customer {CustomerId} created", id);
_logger.LogWarning("Customer {CustomerId} not found", id);
_logger.LogError(ex, "Error while processing customer {CustomerId}", id);
```

Mensagens constantes sem parametros tambem sao aceitas:

```csharp
_logger.LogInformation("Customer created");
```

O padrao com `LoggerMessage` tambem e aceito:

```csharp
private static readonly Action<ILogger, int, Exception?> CustomerCreated =
    LoggerMessage.Define<int>(LogLevel.Information, new EventId(), "Customer {CustomerId} created");
```

## Configuracao

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH024.severity = warning
```

A regra nao possui opcoes proprias.

## Heuristica

O analyzer usa analise semantica e reporta quando todos os pontos abaixo sao verdadeiros:

- a chamada resolve para `Microsoft.Extensions.Logging.LoggerExtensions`;
- o metodo e `LogTrace`, `LogDebug`, `LogInformation`, `LogWarning`, `LogError` ou `LogCritical`;
- o argumento semantico `message` e uma string interpolada;
- ou o argumento `message` e uma concatenacao de string nao constante.

Chamadas a metodos com o mesmo nome fora de `ILogger` nao sao reportadas.

## Limitacoes conhecidas

- A regra nao analisa `string.Format`.
- Concatenacoes compostas apenas por constantes sao tratadas como mensagem constante e nao sao reportadas.
- A regra cobre os metodos de extensao usuais de `ILogger`; chamadas ao metodo generico de baixo nivel `ILogger.Log<TState>` ficam fora do escopo.

## Impacto esperado

- Mantem propriedades estruturadas nos logs.
- Reduz mensagens dinamicas que dificultam busca e agregacao.
- Reforca um padrao uniforme de observabilidade sem adicionar dependencias ao projeto.
