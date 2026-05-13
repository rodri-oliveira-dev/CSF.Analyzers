# ARCH001: Evite async void fora de event handlers

## Objetivo
Evitar `async void` em métodos, funções locais e funções anônimas, exceto em event handlers padrão.

## Motivação
`async void` não pode ser aguardado com `await` e propaga exceções pelo contexto de sincronização em vez de propagá-las por uma `Task`. Isso torna falhas mais difíceis de observar, testar e compor. Em código de aplicação, `async Task` é o padrão mais seguro.

## Inválido

```csharp
public async void PublishAsync()
{
    await _client.SendAsync();
}
```

```csharp
Action action = async () =>
{
    await Task.Delay(1);
};
```

## Válido

```csharp
public async Task PublishAsync()
{
    await _client.SendAsync();
}
```

```csharp
button.Click += async (sender, e) =>
{
    await Task.Delay(1);
};
```

```csharp
public async void OnClick(object? sender, EventArgs e)
{
    await Task.Delay(1);
}
```

## Como configurar
Esta regra não expõe opções customizadas de `.editorconfig` na primeira versão.

A severidade pode ser configurada normalmente:

```ini
# .editorconfig
[*.cs]
dotnet_diagnostic.ARCH001.severity = warning
```

## Code fix
Esta regra oferece code fix para casos mecânicos e seguros:

- métodos `async void` diagnosticados são convertidos para `async Task`;
- funções locais `async void` diagnosticadas são convertidas para `async Task`;
- `using System.Threading.Tasks;` é adicionado quando necessário.

O code fix não é oferecido para funções anônimas, overrides ou métodos que implementam interfaces, porque esses casos podem alterar contratos e chamadores.

## Limitações conhecidas
- A regra trata o padrão clássico `object sender, EventArgs e`, incluindo tipos derivados de `EventArgs`, como um formato permitido de event handler.
- Eventos baseados em delegates customizados que não herdam de `EventArgs` não são isentos nesta primeira versão.
- O code fix altera apenas a declaração diagnosticada. Chamadores de métodos concretos podem precisar de ajustes manuais para aguardar a `Task` retornada.

## Quando não usar
Não desabilite esta regra de forma ampla. Se o código realmente precisar seguir uma assinatura de event handler, mantenha o event handler pequeno e mova o trabalho real para um método `async Task`.

## Impacto esperado
- Menos falhas assíncronas ocultas
- Melhor testabilidade
- Composição assíncrona mais limpa
- Menor risco de erros de fire-and-forget disfarçados como fluxo de controle comum

## Falsos positivos e heurísticas
A principal heurística é a exceção para event handlers. Se a solução depende muito de delegates de evento customizados, considere estender a regra em uma versão futura para reconhecer padrões de delegate específicos do projeto.
