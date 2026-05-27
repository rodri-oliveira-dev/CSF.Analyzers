# ARCH031: Prefira System.Threading.Lock em vez de lock object

## Objetivo

Recomendar `System.Threading.Lock` quando o código usa `object` apenas como monitor de sincronizacao em um `lock`.

## Motivacao

O padrão histórico de criar um campo `object` para sincronizacao funciona, mas o tipo não comunica a intenção do membro. Em projetos modernos, `System.Threading.Lock` representa explicitamente um monitor dedicado para `lock` e permite que runtime e compilador apliquem o caminho especializado para esse uso.

## Código não conforme

```csharp
public sealed class Worker
{
    private readonly object _gate = new();

    public void Execute()
    {
        lock (_gate)
        {
        }
    }
}
```

Também é reportado criar um monitor novo diretamente dentro do `lock`, pois isso não sincroniza chamadas diferentes:

```csharp
lock (new object())
{
}
```

## Código conforme

```csharp
public sealed class Worker
{
    private readonly System.Threading.Lock _gate = new();

    public void Execute()
    {
        lock (_gate)
        {
        }
    }
}
```

Outros mecanismos de sincronizacao permanecem válidos quando usados diretamente, sem `lock` em `object`:

```csharp
private readonly SemaphoreSlim _semaphore = new(1, 1);
```

## Requisito de plataforma

`System.Threading.Lock` está disponível em plataformas modernas do .NET. A regra usa `net9.0` como target framework mínimo por padrão.

Quando o analyzer não consegue detectar o target framework do projeto, ele executa normalmente para manter a recomendação visível. Em projetos que ainda não podem usar `System.Threading.Lock`, configure um target mínimo maior ou ajuste a severidade da regra.

## Configuração

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH031.severity = warning
```

O target framework mínimo pode ser ajustado:

```ini
[*.cs]
dotnet_diagnostic.ARCH031.minimum_target_framework = net9.0
```

Quando `build_property.TargetFramework` estiver disponível e for menor que o mínimo configurado, a regra não reporta diagnósticos. Valores inválidos usam o padrão `net9.0`.

Variáveis locais do tipo `object` são reportadas por padrão, mas podem ser ignoradas:

```ini
[*.cs]
dotnet_diagnostic.ARCH031.report_local_variables = false
```

Valores inválidos de `report_local_variables` usam o padrão `true`.

### Fallback das opções

- `minimum_target_framework`: texto no formato `netX.Y`; default `net9.0`. Valor ausente, vazio ou inválido usa `net9.0`. JSON não se aplica. Quando `build_property.TargetFramework` está ausente ou inválido, a regra executa normalmente.
- `report_local_variables`: booleano; default `true`. Valores booleanos aceitam casing variado; valor ausente, vazio ou inválido usa `true`.

O fallback mantém a regra visível: configuração inválida não desabilita diagnósticos.

## Heurística

O analyzer registra `lock` statements e usa análise semântica para obter o tipo da expressao bloqueada.

A regra reporta quando:

- a expressao do `lock` resolve para `System.Object`;
- a expressao referência campo, propriedade ou variável local;
- a expressão é uma criação direta como `new object()`.

A regra não reporta quando:

- o tipo já e `System.Threading.Lock`;
- o tipo é customizado e diferente de `object`;
- a expressao não tem tipo resolvido;
- a expressão é variável local e `report_local_variables = false`.

## Limitações conhecidas

- A regra não acompanha fluxo para provar que um `object` e usado exclusivamente como monitor; ela reage ao uso em `lock`.
- `Monitor.Enter` não é analisado nesta versão.
- A detecção de target framework depende das propriedades expostas pelo MSBuild ao analyzer. Quando essa informacao não está disponível, a regra roda normalmente.
- A regra não oferece code fix nesta versão.

## Impacto esperado

- Deixa a intenção de sincronizacao mais clara no código.
- Incentiva o uso do tipo específico de sincronizacao em projetos modernos.
- Evita falsos positivos em tipos customizados usados com `lock`.
