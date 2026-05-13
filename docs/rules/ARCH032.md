# ARCH032: Evite propriedades MSBuild duplicadas

## Objetivo

Detectar propriedades MSBuild repetidas em `.csproj` quando a mesma propriedade ja existe no `Directory.Build.props` mais proximo recebido como `AdditionalFiles`.

Duplicar propriedades comuns em varios projetos aumenta o risco de drift: um projeto muda a configuracao localmente, outro fica com valor antigo, e a manutencao passa a depender de lembrar todos os pontos onde a propriedade foi copiada.

## Codigo nao conforme

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

```xml
<!-- MyApp.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

`Nullable` ja esta centralizado no `Directory.Build.props`, entao a definicao local no projeto e reportada.

## Codigo conforme

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

```xml
<!-- MyApp.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>
```

O projeto mantem apenas propriedades especificas dele. As propriedades compartilhadas ficam centralizadas.

## Configuracao

A regra aceita opcoes em `.editorconfig`. As opcoes em formato JSON aceitam arrays de strings e escapes JSON comuns, incluindo unicode escapado:

```ini
[*.csproj]
dotnet_diagnostic.ARCH032.ignored_properties = ["TargetFramework", "TargetFrameworks", "AssemblyName", "RootNamespace"]
dotnet_diagnostic.ARCH032.compare_values = true
```

`ignored_properties` e um array JSON de propriedades que podem aparecer no projeto mesmo quando tambem existem no `Directory.Build.props`. Quando a opcao e omitida ou contem JSON invalido, a regra usa esta lista padrao:

- `TargetFramework`
- `TargetFrameworks`
- `AssemblyName`
- `RootNamespace`
- `PackageId`
- `Version`
- `Authors`
- `Description`

`compare_values` controla a comparacao:

- `true` (padrao): reporta apenas quando nome e valor sao iguais.
- `false`: reporta quando o nome existe nos dois arquivos, mesmo com valores diferentes.

### Fallback das opcoes

- `ignored_properties`: array JSON de strings; default e a lista padrao quando ausente ou malformado. Propriedades sao aparadas e comparadas sem diferenciar maiusculas de minusculas. Entradas vazias sao ignoradas. Um array JSON vazio substitui a lista por vazio.
- `compare_values`: booleano; default `true`. Valores booleanos aceitam casing variado; valor ausente, vazio ou invalido usa `true`.

O fallback preserva o comportamento padrao: propriedades comuns continuam ignoradas e a comparacao de valores permanece habilitada.

A severidade padrao da regra e `Info`, conforme o descriptor do analyzer. Ela pode ser sobrescrita normalmente via `.editorconfig`. Por exemplo, para elevar a regra para `warning`:

```ini
[*.csproj]
dotnet_diagnostic.ARCH032.severity = warning
```

## AdditionalFiles

Roslyn nao transforma `.csproj` e `Directory.Build.props` em syntax trees C#. Por isso, a regra depende de esses arquivos chegarem ao analyzer como `AdditionalFiles`.

Em consumidores que usam o analyzer via MSBuild, inclua os arquivos quando necessario:

```xml
<ItemGroup>
  <AdditionalFiles Include="Directory.Build.props" />
  <AdditionalFiles Include="**\*.csproj" />
</ItemGroup>
```

Se nenhum `.csproj` ou nenhum `Directory.Build.props` for recebido como `AdditionalFiles`, a regra nao reporta diagnosticos.

## Segurança e limites

Arquivos MSBuild informados como `AdditionalFiles` sao processados com limites defensivos. Arquivos vazios, invalidos ou acima do limite configurado sao ignorados para evitar degradacao de build/IDE.

## Heuristica

A regra:

- filtra `AdditionalFiles` chamados `Directory.Build.props`;
- filtra `AdditionalFiles` terminados em `.csproj`;
- le XML com parser endurecido e limite de tamanho;
- extrai propriedades diretas de `PropertyGroup`;
- ignora `PropertyGroup` com `Condition`;
- ignora propriedades com `Condition`;
- ignora propriedades vazias;
- ignora XML invalido e arquivos sem texto;
- compara nomes com `StringComparer.OrdinalIgnoreCase`;
- usa o `Directory.Build.props` ancestral mais proximo do `.csproj`;
- reporta o diagnostico no `.csproj`, no span aproximado do elemento XML.

Propriedades em `ItemGroup` nao sao analisadas.

## Limitacoes conhecidas

- A regra nao avalia expressoes MSBuild, imports ou propriedades calculadas.
- A regra nao reporta propriedades condicionais nesta primeira versao, para reduzir falso positivo.
- A regra considera apenas o `Directory.Build.props` mais proximo no caminho ancestral do projeto.
- A localizacao do diagnostico e aproximada ao elemento XML.
- A regra so enxerga arquivos enviados como `AdditionalFiles`.

## Impacto esperado

- Incentiva centralizacao de configuracao compartilhada.
- Reduz drift entre projetos da mesma solucao.
- Mantem excecoes comuns fora do ruido por meio da lista padrao de propriedades ignoradas.
