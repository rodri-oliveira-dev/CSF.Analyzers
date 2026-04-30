# ARCH031: Prefira System.Threading.Lock em vez de lock object

## Objetivo

Recomendar `System.Threading.Lock` quando o codigo usa `object` apenas como monitor de sincronizacao em um `lock`.

## Motivacao

O padrao historico de criar um campo `object` para sincronizacao funciona, mas o tipo nao comunica a intencao do membro. Em projetos modernos, `System.Threading.Lock` representa explicitamente um monitor dedicado para `lock` e permite que runtime e compilador apliquem o caminho especializado para esse uso.

## Codigo nao conforme

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

Tambem e reportado criar um monitor novo diretamente dentro do `lock`, pois isso nao sincroniza chamadas diferentes:

```csharp
lock (new object())
{
}
```

## Codigo conforme

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

Outros mecanismos de sincronizacao permanecem validos quando usados diretamente, sem `lock` em `object`:

```csharp
private readonly SemaphoreSlim _semaphore = new(1, 1);
```

## Requisito de plataforma

`System.Threading.Lock` esta disponivel em plataformas modernas do .NET. A regra usa `net9.0` como target framework minimo por padrao.

Quando o analyzer nao consegue detectar o target framework do projeto, ele executa normalmente para manter a recomendacao visivel. Em projetos que ainda nao podem usar `System.Threading.Lock`, configure um target minimo maior ou ajuste a severidade da regra.

## Configuracao

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH031.severity = warning
```

O target framework minimo pode ser ajustado:

```ini
[*.cs]
dotnet_diagnostic.ARCH031.minimum_target_framework = net9.0
```

Quando `build_property.TargetFramework` estiver disponivel e for menor que o minimo configurado, a regra nao reporta diagnosticos. Valores invalidos usam o padrao `net9.0`.

Variaveis locais do tipo `object` sao reportadas por padrao, mas podem ser ignoradas:

```ini
[*.cs]
dotnet_diagnostic.ARCH031.report_local_variables = false
```

Valores invalidos de `report_local_variables` usam o padrao `true`.

## Heuristica

O analyzer registra `lock` statements e usa analise semantica para obter o tipo da expressao bloqueada.

A regra reporta quando:

- a expressao do `lock` resolve para `System.Object`;
- a expressao referencia campo, propriedade ou variavel local;
- a expressao e uma criacao direta como `new object()`.

A regra nao reporta quando:

- o tipo ja e `System.Threading.Lock`;
- o tipo e customizado e diferente de `object`;
- a expressao nao tem tipo resolvido;
- a expressao e variavel local e `report_local_variables = false`.

## Limitacoes conhecidas

- A regra nao acompanha fluxo para provar que um `object` e usado exclusivamente como monitor; ela reage ao uso em `lock`.
- `Monitor.Enter` nao e analisado nesta versao.
- A deteccao de target framework depende das propriedades expostas pelo MSBuild ao analyzer. Quando essa informacao nao esta disponivel, a regra roda normalmente.
- A regra nao oferece code fix nesta versao.

## Impacto esperado

- Deixa a intencao de sincronizacao mais clara no codigo.
- Incentiva o uso do tipo especifico de sincronizacao em projetos modernos.
- Evita falsos positivos em tipos customizados usados com `lock`.
