# ARCH010: Exija propagacao de CancellationToken

## Objetivo
Detectar invocações de métodos assíncronos que podem aceitar um `CancellationToken` quando um token já está disponível no escopo atual, mas não está sendo passado.

## Motivacao
Cancelamento cooperativo é uma base importante de código assíncrono responsivo e escalavel. Quando um método recebe um `CancellationToken` e chama outro método que suporta cancelamento, deixar de propagar o token:

- Impede que o trabalho chamado seja cancelado quando o chamador for cancelado.
- Obriga o chamador a esperar a conclusao completa de suboperacoes que poderiam ser canceladas.
- Torna APIs menos previsiveis, porque consumidores esperam que o cancelamento flua pela pilha de chamadas.

## Não conforme

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
ARCH010 usa análise semântica para detectar chamadas a métodos assíncronos que retornam `Task`, `Task<T>`, `ValueTask` ou `ValueTask<T>` e que podem receber `CancellationToken` por:

- Parametro opcional omitido, por exemplo `DoWorkAsync(id, CancellationToken cancellationToken = default)`.
- Sobrecarga com a mesma assinatura de prefixo e um parâmetro final adicional `CancellationToken`.
- Extension methods reduzidos, incluindo métodos comuns de consulta do Entity Framework Core como `ToListAsync`, `FirstOrDefaultAsync`, `SingleOrDefaultAsync`, `AnyAsync` e `CountAsync`.
- APIs de infraestrutura comuns com overload de token, incluindo `HttpClient.GetAsync`, `PostAsync`, `PutAsync`, `DeleteAsync` e `SendAsync`.
- Metodos de persistencia como `SaveChangesAsync`.

O token disponível pode vir de parâmetros, variáveis locais, campos ou propriedades no escopo acessível. A regra é baseada no tipo `System.Threading.CancellationToken`, entao nomes como `cancellationToken`, `ct` e `token` são aceitos.

## Configuração
Esta regra não expõe opções customizadas de `.editorconfig`.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH010.severity = warning
```

## Limitações conhecidas
- A detecção de overloads é conservadora: a sobrecarga deve ter exatamente um parâmetro a mais do que o método invocado, os parâmetros de prefixo devem ter tipos compatíveis, e o parâmetro adicional deve ser `CancellationToken`.
- Sobrecargas com rearranjo de parâmetros, parâmetros intermediarios extras ou escolhas dependentes de conversoes complexas podem não ser sinalizadas.
- A regra não tenta escolher qual token deve ser propagado quando mais de um `CancellationToken` está disponível.
- A regra não avalia se uma variável `CancellationToken` e de fato utilizável, se já foi cancelada, ou se representa `CancellationToken.None`.
- Uma chamada que passa explicitamente `CancellationToken.None` ou `default` e tratada como uma decisão intencional e não gera diagnóstico.

## Quando não usar
Em casos raros, voce pode omitir um token intencionalmente:

- Quando a suboperacao precisa terminar completamente mesmo que a operacao pai seja cancelada.
- Quando a API chamada e conhecida por ignorar o token e passa-lo adiciona ruído.

Nesses casos, suprima o diagnóstico com um comentário claro explicando a omissao intencional.

## Impacto esperado
- Melhor resposta a cancelamento em toda a aplicação.
- Menor latencia quando usuarios ou sistemas solicitam cancelamento.
- Comportamento mais previsível de APIs assincronas.

## Observacoes sobre falsos positivos / heurísticas
O analyzer permanece silencioso quando:

- Nenhum `CancellationToken` está disponível no escopo lexico atual.
- A invocacao já passa um argumento `CancellationToken`.
- O método invocado e sincrono, mesmo que tenha parâmetro opcional ou sobrecarga com `CancellationToken`.
- O método invocado não tem sobrecarga ou parâmetro opcional que aceite `CancellationToken`.
- A única sobrecarga com `CancellationToken` tem uma assinatura de parâmetros fundamentalmente diferente.
