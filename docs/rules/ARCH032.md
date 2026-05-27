# ARCH032: Evite propriedades MSBuild duplicadas

## Objetivo

Detectar propriedades MSBuild repetidas em `.csproj` quando a mesma propriedade já existe no `Directory.Build.props` mais próximo recebido como `AdditionalFiles`.

Duplicar propriedades comuns em vários projetos aumenta o risco de drift: um projeto muda a configuração localmente, outro fica com valor antigo, e a manutenção passa a depender de lembrar todos os pontos onde a propriedade foi copiada.

## Código não conforme

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

`Nullable` já está centralizado no `Directory.Build.props`, então a definição local no projeto é reportada.

## Código conforme

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

O projeto mantém apenas propriedades específicas dele. As propriedades compartilhadas ficam centralizadas.

## Configuração

A regra aceita opções em `.editorconfig`. As opções em formato JSON aceitam arrays de strings e escapes JSON comuns, incluindo unicode escapado:

```ini
[*.csproj]
dotnet_diagnostic.ARCH032.ignored_properties = ["TargetFramework", "TargetFrameworks", "AssemblyName", "RootNamespace"]
dotnet_diagnostic.ARCH032.compare_values = true
```

`ignored_properties` é um array JSON de propriedades que podem aparecer no projeto mesmo quando também existem no `Directory.Build.props`. Quando a opção é omitida ou contém JSON inválido, a regra usa esta lista padrão:

- `TargetFramework`
- `TargetFrameworks`
- `AssemblyName`
- `RootNamespace`
- `PackageId`
- `Version`
- `Authors`
- `Description`

`compare_values` controla a comparação:

- `true` (padrão): reporta apenas quando nome e valor são iguais.
- `false`: reporta quando o nome existe nos dois arquivos, mesmo com valores diferentes.

### Fallback das opções

- `ignored_properties`: array JSON de strings; default é a lista padrão quando ausente ou malformado. Propriedades são aparadas e comparadas sem diferenciar maiúsculas de minúsculas. Entradas vazias são ignoradas. Um array JSON vazio substitui a lista por vazio.
- `compare_values`: booleano; default `true`. Valores booleanos aceitam casing variado; valor ausente, vazio ou inválido usa `true`.

O fallback preserva o comportamento padrão: propriedades comuns continuam ignoradas e a comparação de valores permanece habilitada.

A severidade padrão da regra é `Info`, conforme o descriptor do analyzer. Ela pode ser sobrescrita normalmente via `.editorconfig`. Por exemplo, para elevar a regra para `warning`:

```ini
[*.csproj]
dotnet_diagnostic.ARCH032.severity = warning
```

## AdditionalFiles

Roslyn não transforma `.csproj` e `Directory.Build.props` em syntax trees C#. Por isso, a regra depende de esses arquivos chegarem ao analyzer como `AdditionalFiles`.

Em consumidores que usam o analyzer via MSBuild, inclua os arquivos quando necessário:

```xml
<ItemGroup>
  <AdditionalFiles Include="Directory.Build.props" />
  <AdditionalFiles Include="**\*.csproj" />
</ItemGroup>
```

Se nenhum `.csproj` ou nenhum `Directory.Build.props` for recebido como `AdditionalFiles`, a regra não reporta diagnósticos.

## Segurança e limites

Arquivos MSBuild informados como `AdditionalFiles` são processados com limites defensivos. Arquivos vazios, inválidos ou acima do limite configurado são ignorados para evitar degradação de build/IDE.

## Heurística

A regra:

- filtra `AdditionalFiles` chamados `Directory.Build.props`;
- filtra `AdditionalFiles` terminados em `.csproj`;
- le XML com parser endurecido e limite de tamanho;
- extrai propriedades diretas de `PropertyGroup`;
- ignora `PropertyGroup` com `Condition`;
- ignora propriedades com `Condition`;
- ignora propriedades vazias;
- ignora XML inválido e arquivos sem texto;
- compara nomes com `StringComparer.OrdinalIgnoreCase`;
- usa o `Directory.Build.props` ancestral mais próximo do `.csproj`;
- reporta o diagnóstico no `.csproj`, no span aproximado do elemento XML.

Propriedades em `ItemGroup` não são analisadas.

## Limitações conhecidas

- A regra não avalia expressoes MSBuild, imports ou propriedades calculadas.
- A regra não reporta propriedades condicionais nesta primeira versão, para reduzir falso positivo.
- A regra considera apenas o `Directory.Build.props` mais próximo no caminho ancestral do projeto.
- A localização do diagnóstico é aproximada ao elemento XML.
- A regra so enxerga arquivos enviados como `AdditionalFiles`.

## Impacto esperado

- Incentiva centralização de configuração compartilhada.
- Reduz drift entre projetos da mesma solução.
- Mantem exceções comuns fora do ruído por meio da lista padrão de propriedades ignoradas.
