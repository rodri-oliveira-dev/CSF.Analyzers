# ARCH009: Proiba bloqueio síncrono de operações assíncronas

## Objetivo
Evitar o bloqueio síncrono de operações assíncronas detectando o uso de `.Result`, `.Wait()` e `.GetAwaiter().GetResult()` em `Task`, `Task<T>`, `ValueTask` e `ValueTask<T>`.

## Motivação
Bloquear no thread chamador um trabalho iniciado de forma assíncrona é uma fonte conhecida de deadlocks e degradação de escalabilidade.

- **Deadlocks**: quando uma chamada bloqueante roda em uma thread que carrega um `SynchronizationContext` (por exemplo, threads de UI ou threads de requisição do ASP.NET legado), a continuação aguardada pode tentar voltar para o mesmo contexto, que agora está bloqueado.
- **Exaustão do thread pool**: esperas síncronas consómem threads que poderiam processar novo trabalho, reduzindo o throughput geral.
- **Encapsulamento de exceções**: `.Result` encapsula exceções em `AggregateException`, escondendo o tipo original da exceção e complicando o tratamento de erro.

Prefira `await` para que o chamador libere a thread e retome naturalmente quando a operação terminar.

## Não conforme

```csharp
using System.Threading.Tasks;

public sealed class Sample
{
    public int FetchSync(Task<int> fetcher)
    {
        // Risks deadlock and hides original exception type
        return fetcher.Result;
    }

    public void ExecuteSync(Task task)
    {
        // Blocks the calling thread
        task.Wait();
    }

    public int FetchViaAwaiter(Task<int> fetcher)
    {
        // Same blocking risk as .Result
        return fetcher.GetAwaiter().GetResult();
    }
}
```

## Conforme

```csharp
using System.Threading.Tasks;

public sealed class Sample
{
    public async Task<int> FetchAsync(Task<int> fetcher)
    {
        return await fetcher;
    }

    public async Task ExecuteAsync(Task task)
    {
        await task;
    }
}
```

## Configuração
Esta regra não expõe opções customizadas de `.editorconfig` na primeira versão.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH009.severity = warning
```

## Limitações conhecidas
- O analyzer mira apenas `System.Threading.Tasks.Task`, `Task<T>`, `ValueTask` e `ValueTask<T>`. Ele não sinaliza bloqueio em tipos awaitable customizados.
- Sobrecargas de `.Wait()` com tokens de cancelamento ou timeouts ainda são reportadas porque mantêm a mesma semântica de bloqueio.
- Nenhum code fix é fornecido porque converter código bloqueante para `await` muitas vezes exige alterar a assinatura do método que contém o código (tipo de retorno, modificador `async`) e pode afetar chamadores.

## Quando não usar
Em cenários muito raros, você pode bloquear intencionalmente:

- Métodos `Main` de aplicações console que não podem ser `async` em target frameworks antigos.
- Fronteiras de APIs legadas de terceiros em que você não pode alterar a assinatura para `async`.

Nesses casos, suprima o diagnóstico com um comentário claro explicando por que o bloqueio é inevitável.

## Impacto esperado
- Menos deadlocks em aplicações de UI e web legadas.
- Melhor uso do thread pool e melhor escalabilidade.
- Tratamento de exceções mais limpo (sem encapsulamento em `AggregateException`).

## Observações sobre falsos positivos / heurísticas
O analyzer usa informações semânticas para garantir que reporta apenas membros definidos pelos tipos reais de task da BCL. Ele permanece silencioso para:

- Tipos customizados que por acaso definem uma propriedade `.Result`.
- Tipos customizados que por acaso definem um método `.Wait()`.
- Awaitables customizados com `.GetAwaiter().GetResult()` que não são `Task`/`ValueTask`.
