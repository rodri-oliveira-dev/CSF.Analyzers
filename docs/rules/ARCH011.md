# ARCH011: Proiba lógica assíncrona ou bloqueante em construtores

## Objetivo
Evitar que construtores contenham operações bloqueantes (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`) ou chamadas assíncronas não aguardadas (retorno `Task`/`ValueTask` descartado). Construtores devem permanecer rápidos e não bloqueantes.

## Motivação
Executar trabalho bloqueante ou assíncrono dentro de um construtor causa vários problemas de engenhária:

- **Deadlocks**: bloquear em operações assíncronas dentro de um construtor traz o mesmo risco de deadlock de outros locais, e construtores são especialmente difíceis de refatorar para async porque eles mesmos não podem ser async.
- **Exaustão do thread pool**: esperas síncronas em construtores bloqueiam o chamador durante a criação do objeto, reduzindo escalabilidade.
- **Trabalho assíncrono oculto**: descartar uma `Task`/`ValueTask` sem aguardar, atribuir a um campo ou encadear esconde trabalho fire-and-forget que pode falhar silenciosamente.
- **Composição**: construtores que executam I/O ou trabalho bloqueante tornam os tipos mais difíceis de usar em testes e containers de injeção de dependência.

Prefira um método factory assíncrono (por exemplo, `static async Task<MyClass> CreateAsync(...)`) quando for necessária inicialização assíncrona.

## Não conforme

```csharp
using System.Threading.Tasks;

public sealed class Service
{
    private readonly int _value;

    public Service(Task<int> fetcher)
    {
        // Blocks the calling thread
        _value = fetcher.Result;
    }
}

public sealed class Loader
{
    public Loader(Task dataTask)
    {
        // Blocks the calling thread
        dataTask.Wait();
    }
}

public sealed class Processor
{
    public Processor(Task<int> fetcher)
    {
        // Same blocking risk as .Result
        var value = fetcher.GetAwaiter().GetResult();
    }
}

public sealed class BackgroundStarter
{
    public BackgroundStarter()
    {
        // Fire-and-forget: exceptions are lost, ordering is unclear
        StartAsync();
    }

    private Task StartAsync() => Task.CompletedTask;
}
```

## Conforme

```csharp
using System.Threading.Tasks;

public sealed class Service
{
    private readonly int _value;

    private Service(int value)
    {
        _value = value;
    }

    public static async Task<Service> CreateAsync(Task<int> fetcher)
    {
        var value = await fetcher;
        return new Service(value);
    }
}

public sealed class Loader
{
    private readonly Task _dataTask;

    public Loader(Task dataTask)
    {
        // Assignment only: no blocking or fire-and-forget
        _dataTask = dataTask;
    }
}
```

## Configuração
Esta regra não expõe opções customizadas de `.editorconfig` na primeira versão.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH011.severity = warning
```

## Limitações conhecidas
- O analyzer mira apenas `System.Threading.Tasks.Task`, `Task<T>`, `ValueTask` e `ValueTask<T>`. Ele não sinaliza bloqueio em tipos awaitable customizados.
- Sobrecargas de `.Wait()` com tokens de cancelamento ou timeouts ainda são reportadas porque mantêm a mesma semântica de bloqueio.
- Chámadas async não aguardadas são reportadas apenas quando a `Task`/`ValueTask` é usada como expression statement (fire-and-forget). Atribuir a uma variável local ou passar como argumento não é reportado.
- Nenhum code fix é fornecido porque converter lógica de construtor para uma factory async exige alterar a API pública e os chamadores.

## Quando não usar
Em cenários raros, você pode executar trabalho síncrono intencionalmente em um construtor:

- Inicializacao trivial em memória que não bloqueia em I/O externo.
- Caminhos de código legado em que alterar a superfície do construtor não é viável.

Nesses casos, suprima o diagnóstico com um comentário claro explicando por que o padrão é inevitável.

## Impacto esperado
- Menos deadlocks durante a construção de objetos.
- Melhor uso do thread pool e escalabilidade.
- Ciclo de vida mais claro: inicialização async fica explícita por métodos factory.

## Observações sobre falsos positivos / heurísticas
O analyzer usa informações semânticas para garantir que reporta apenas membros definidos pelos tipos reais de task da BCL dentro de construtores. Ele permanece silencioso para:

- Tipos customizados que por acaso definem uma propriedade `.Result`.
- Tipos customizados que por acaso definem um método `.Wait()`.
- Awaitables customizados com `.GetAwaiter().GetResult()` que não são `Task`/`ValueTask`.
- Atribuições e passagem de argumentos de valores `Task`/`ValueTask`.
- Chámadas bloqueantes fora de construtores (cobertas por ARCH009).
