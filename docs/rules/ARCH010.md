# ARCH010: Exija propagacao de CancellationToken

## Objetivo
Detectar invocacoes de metodos assincronos que podem aceitar um `CancellationToken` quando um token ja esta disponivel no escopo atual, mas nao esta sendo passado.

## Motivacao
Cancelamento cooperativo e uma base importante de codigo assincrono responsivo e escalavel. Quando um metodo recebe um `CancellationToken` e chama outro metodo que suporta cancelamento, deixar de propagar o token:

- Impede que o trabalho chamado seja cancelado quando o chamador for cancelado.
- Obriga o chamador a esperar a conclusao completa de suboperacoes que poderiam ser canceladas.
- Torna APIs menos previsiveis, porque consumidores esperam que o cancelamento flua pela pilha de chamadas.

## Nao conforme

```csharp
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkAsync(int id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class Consumer
{
    // ARCH010: Pass the available CancellationToken to 'DoWorkAsync'.
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkAsync(1);
    }
}
```

```csharp
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    // Optional parameter: still reportable when omitted.
    public Task DoWorkAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public sealed class Consumer
{
    // ARCH010: Pass the available CancellationToken to 'DoWorkAsync'.
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkAsync(1);
    }
}
```

```csharp
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public sealed class Repository
{
    // ARCH010: Pass the available CancellationToken to 'ToListAsync'.
    public async Task<List<Customer>> ListAsync(IQueryable<Customer> customers, CancellationToken token)
    {
        return await customers.ToListAsync();
    }
}
```

```csharp
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public sealed class Gateway
{
    // ARCH010: Pass the available CancellationToken to 'GetAsync'.
    public async Task<HttpResponseMessage> GetAsync(HttpClient httpClient, CancellationToken ct)
    {
        return await httpClient.GetAsync("https://example.test");
    }
}
```

## Conforme

```csharp
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkAsync(int id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class Consumer
{
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkAsync(1, cancellationToken);
    }
}
```

```csharp
using System.Threading.Tasks;

public sealed class Consumer
{
    // No token available: analyzer stays silent.
    public async Task ExecuteAsync(Service service)
    {
        await service.DoWorkAsync(1);
    }
}
```

```csharp
using System.Threading;
using System.Threading.Tasks;

public sealed class Consumer
{
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        // Intentional: this explicit CancellationToken argument is treated as a decision.
        await service.DoWorkAsync(1, CancellationToken.None);
    }
}
```

## Cobertura
ARCH010 usa analise semantica para detectar chamadas a metodos assincronos que retornam `Task`, `Task<T>`, `ValueTask` ou `ValueTask<T>` e que podem receber `CancellationToken` por:

- Parametro opcional omitido, por exemplo `DoWorkAsync(id, CancellationToken cancellationToken = default)`.
- Sobrecarga com a mesma assinatura de prefixo e um parametro final adicional `CancellationToken`.
- Extension methods reduzidos, incluindo metodos comuns de consulta do Entity Framework Core como `ToListAsync`, `FirstOrDefaultAsync`, `SingleOrDefaultAsync`, `AnyAsync` e `CountAsync`.
- APIs de infraestrutura comuns com overload de token, incluindo `HttpClient.GetAsync`, `PostAsync`, `PutAsync`, `DeleteAsync` e `SendAsync`.
- Metodos de persistencia como `SaveChangesAsync`.

O token disponivel pode vir de parametros, variaveis locais, campos ou propriedades no escopo acessivel. A regra e baseada no tipo `System.Threading.CancellationToken`, entao nomes como `cancellationToken`, `ct` e `token` sao aceitos.

## Configuracao
Esta regra nao expoe opcoes customizadas de `.editorconfig`.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH010.severity = warning
```

## Limitacoes conhecidas
- A deteccao de overloads e conservadora: a sobrecarga deve ter exatamente um parametro a mais do que o metodo invocado, os parametros de prefixo devem ter tipos compativeis, e o parametro adicional deve ser `CancellationToken`.
- Sobrecargas com rearranjo de parametros, parametros intermediarios extras ou escolhas dependentes de conversoes complexas podem nao ser sinalizadas.
- A regra nao tenta escolher qual token deve ser propagado quando mais de um `CancellationToken` esta disponivel.
- A regra nao avalia se uma variavel `CancellationToken` e de fato utilizavel, se ja foi cancelada, ou se representa `CancellationToken.None`.
- Uma chamada que passa explicitamente `CancellationToken.None` ou `default` e tratada como uma decisao intencional e nao gera diagnostico.

## Quando nao usar
Em casos raros, voce pode omitir um token intencionalmente:

- Quando a suboperacao precisa terminar completamente mesmo que a operacao pai seja cancelada.
- Quando a API chamada e conhecida por ignorar o token e passa-lo adiciona ruido.

Nesses casos, suprima o diagnostico com um comentario claro explicando a omissao intencional.

## Impacto esperado
- Melhor resposta a cancelamento em toda a aplicacao.
- Menor latencia quando usuarios ou sistemas solicitam cancelamento.
- Comportamento mais previsivel de APIs assincronas.

## Observacoes sobre falsos positivos / heuristicas
O analyzer permanece silencioso quando:

- Nenhum `CancellationToken` esta disponivel no escopo lexico atual.
- A invocacao ja passa um argumento `CancellationToken`.
- O metodo invocado e sincrono, mesmo que tenha parametro opcional ou sobrecarga com `CancellationToken`.
- O metodo invocado nao tem sobrecarga ou parametro opcional que aceite `CancellationToken`.
- A unica sobrecarga com `CancellationToken` tem uma assinatura de parametros fundamentalmente diferente.
