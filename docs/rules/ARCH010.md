# ARCH010: Exija propagação de CancellationToken

## Objetivo
Detectar invocações de métodos que podem aceitar um `CancellationToken` quando um token já está disponível no escopo atual, mas não está sendo passado.

## Motivação
Cancelamento cooperativo é uma base importante de código assíncrono responsivo e escalável. Quando um método recebe um `CancellationToken` e chama outro método que suporta cancelamento, deixar de propagar o token:

- Impede que o trabalho chamado seja cancelado quando o chamador for cancelado.
- Obriga o chamador a esperar a conclusão completa de suboperações que não podem ser canceladas.
- Torna APIs menos previsíveis, porque consumidores esperam que o cancelamento flua pela pilha de chamadas.

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
using System.Threading;
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

## Configuração
Esta regra não expõe opções customizadas de `.editorconfig` na primeira versão, mas a infraestrutura foi desenháda para suportar configurações futuras (por exemplo, excluir padrões de métodos ou tipos específicos).

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH010.severity = warning
```

## Limitações conhecidas
- O analyzer detecta sobrecargas usando uma heurística conservadora: a sobrecarga deve ter exatamente um parâmetro a mais do que o método invocado, os parâmetros de prefixo devem ter tipos correspondentes, e o parâmetro adicional deve ser `CancellationToken`. Sobrecargas com arranjos diferentes de parâmetros não são sinalizadas.
- Extension methods com sobrecargas que aceitam `CancellationToken` não são detectados atualmente. Esta é uma lacuna conhecida que pode ser tratada em uma versão futura.
- O analyzer usa `SemanticModel.LookupSymbols` para detectar tokens disponíveis. Tokens acessiveis apenas por escopos complexos (por exemplo, capturados em closures de escopos distantes de formas que contornam a busca normal de símbolos) podem não ser reconhecidos.
- O analyzer não avalia se uma variável `CancellationToken` é de fato utilizável (por exemplo, se já foi cancelada ou descartada). Ele verifica apenas disponibilidade.

## Quando não usar
Em casos raros, você pode omitir um token intencionalmente:

- Quando a suboperação precisa terminar completamente mesmo que a operação pai seja cancelada (por exemplo, descarregar estado em disco durante cancelamento).
- Quando a API chamada é conhecida por ignorar o token e passá-lo adiciona ruído.

Nesses casos, suprima o diagnóstico com um comentário claro explicando a omissão intencional.

## Impacto esperado
- Melhor resposta a cancelamento em toda a aplicação.
- Menor latência quando usuários ou sistemas solicitam cancelamento.
- Comportamento mais previsivel de APIs assíncronas.

## Observações sobre falsos positivos / heurísticas
O analyzer permanece silencioso quando:

- Nenhum `CancellationToken` está disponível no escopo léxico atual (parâmetros, locais, campos ou propriedades).
- A invocação já passa um argumento `CancellationToken`.
- O método invocado não tem sobrecarga ou parâmetro opcional que aceite `CancellationToken`.
- A única sobrecarga com `CancellationToken` tem uma assinatura de parâmetros fundamentalmente diferente.
