# ARCH002: Evite Task.ContinueWith

## Objetivo
Evitar o uso de `Task.ContinueWith(...)` e incentivar `await` como fluxo assíncrono preferêncial.

## Motivação
`ContinueWith` tende a produzir código em estilo de callback, mais difícil de ler e manter do que código linear com `async`/`await`.

Usar `await` normalmente oferece:

- **Melhor legibilidade** (o código permanece linear)
- **Melhor propagação de exceções** (exceções fluem naturalmente pela `Task` aguardada)
- **Melhor manutenção** (menos encadeamento manual de continuações e menos armadilhas sutis de agendamento)

## Não conforme

```csharp
using System.Threading.Tasks;

public sealed class Sample
{
    public Task ExecuteAsync()
    {
        return Task.Delay(10)
            .ContinueWith(_ => DoWork());
    }

    private static void DoWork() { }
}
```

```csharp
using System.Threading.Tasks;

public sealed class Sample
{
    public Task<int> ExecuteAsync()
    {
        return Task.FromResult(1)
            .ContinueWith(t => t.Result + 1);
    }
}
```

## Conforme

```csharp
using System.Threading.Tasks;

public sealed class Sample
{
    public async Task ExecuteAsync()
    {
        await Task.Delay(10);
        DoWork();
    }

    private static void DoWork() { }
}
```

```csharp
using System.Threading.Tasks;

public sealed class Sample
{
    public async Task<int> ExecuteAsync()
    {
        var value = await Task.FromResult(1);
        return value + 1;
    }
}
```

## Configuração
Esta regra não expõe opções customizadas de `.editorconfig` na primeira versão.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH002.severity = warning
```

## Limitações conhecidas
- Esta regra sinaliza `ContinueWith` chamado em `Task` e `Task<T>`.
- Ela não tenta validar se um uso específico de `ContinueWith` é "seguro" em determinado contexto; ela sempre recomenda `await` como padrão.
- Nenhum code fix é fornecido porque substituir `ContinueWith` por `await` não é determinístico e pode alterar semântica (tipos de retorno, agendamento, comportamento de cancelamento, uso de contexto de sincronização etc.).

## Quando não usar
Em casos raros, você pode usar `ContinueWith` intencionalmente para composição de tarefas de baixo nível ou para evitar state machines `async` em caminhos muito quentes.

Se mantiver `ContinueWith`, garanta que você entende e revisou:

- Implicacoes de TaskScheduler / contexto de sincronização
- Observação e propagação de exceções
- Comportamento de cancelamento

## Impacto esperado
- Uso mais consistente de `async`/`await` em toda a base de código
- Menos cadeias de continuação e código assíncrono em estilo callback
- Padrões mais previsíveis de propagação de exceções

## Observações sobre falsos positivos / heurísticas
O analyzer usa informações semânticas para mirar apenas `System.Threading.Tasks.Task.ContinueWith` e `Task<T>.ContinueWith`.
