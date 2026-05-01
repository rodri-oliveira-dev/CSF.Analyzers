# ARCH030: Detecte PackageReference duplicado entre projetos

## Objetivo

Detectar quando o mesmo `PackageReference` aparece em mais de um `.csproj` recebido como `AdditionalFiles`.

Em solucoes com camadas bem definidas, uma dependencia repetida em varios projetos pode indicar acoplamento excessivo, falta de centralizacao ou uma dependencia que deveria existir apenas em um adaptador mais externo.

## Quando a duplicidade e problema

A duplicidade merece revisao quando o mesmo pacote aparece em projetos de camadas diferentes, por exemplo dominio, aplicacao e infraestrutura. Isso pode espalhar detalhes de framework para projetos que deveriam depender de abstracoes ou de project references.

```xml
<!-- MyApp.Domain.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Serilog" />
  </ItemGroup>
</Project>
```

```xml
<!-- MyApp.Application.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Serilog" />
  </ItemGroup>
</Project>
```

## Quando nao e problema

Alguns pacotes sao naturalmente repetidos em muitos projetos, especialmente pacotes de teste. A regra ja permite por padrao:

- `Microsoft.NET.Test.Sdk`
- `xunit`
- `xunit.runner.visualstudio`
- `coverlet.collector`
- `FluentAssertions`
- `NSubstitute`
- `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`

Tambem pode ser aceitavel repetir pacotes em projetos de benchmark, testes de contrato, exemplos ou projetos auxiliares. Use `allowed_project_patterns` nesses casos.

## Codigo conforme

Centralize a dependencia no projeto que realmente a utiliza ou exponha a funcionalidade por uma abstracao:

```xml
<!-- MyApp.Infrastructure.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Serilog" />
  </ItemGroup>
</Project>
```

```xml
<!-- MyApp.Application.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\MyApp.Abstractions\MyApp.Abstractions.csproj" />
  </ItemGroup>
</Project>
```

## Configuracao

A regra aceita arrays JSON em `.editorconfig`. As opcoes em formato JSON aceitam arrays de strings e escapes JSON comuns, incluindo unicode escapado:

```ini
[*.csproj]
dotnet_diagnostic.ARCH030.allowed_packages = ["Microsoft.NET.Test.Sdk", "xunit", "coverlet.collector"]
dotnet_diagnostic.ARCH030.allowed_project_patterns = ["*.Tests.csproj", "*.Benchmarks.csproj"]
```

`allowed_packages` define a lista de pacotes que podem aparecer em varios projetos. Quando a opcao e omitida ou contem JSON invalido, a regra usa a allowlist padrao.

`allowed_project_patterns` remove projetos inteiros da analise. Os padroes aceitam `*` e sao comparados com o nome do arquivo e com o caminho normalizado. O padrao e vazio.

Listas configuraveis sao normalizadas e possuem limites defensivos de quantidade e tamanho para evitar custo excessivo durante build/IDE. Entradas vazias, duplicadas ou acima do limite sao ignoradas.

### Fallback das opcoes

- `allowed_packages`: array JSON de strings; default e a allowlist padrao quando ausente ou malformado. Pacotes sao aparados e comparados sem diferenciar maiusculas de minusculas. Entradas vazias sao ignoradas. Um array JSON vazio substitui a allowlist por vazio.
- `allowed_project_patterns`: array JSON de strings; default vazio. Padroes sao aparados, aceitam `*` e sao comparados sem diferenciar maiusculas de minusculas. Entradas vazias, duplicadas ou acima do limite sao ignoradas. JSON vazio, invalido ou malformado e ignorado.

O fallback de `allowed_packages` preserva a allowlist padrao; o de `allowed_project_patterns` e restritivo, pois nao ignora projetos quando a configuracao e invalida.

A severidade pode ser configurada normalmente:

```ini
[*.csproj]
dotnet_diagnostic.ARCH030.severity = warning
```

## AdditionalFiles

Roslyn nao analisa `.csproj` como syntax trees C#. Por isso, a regra depende de os arquivos de projeto estarem disponiveis como `AdditionalFiles`.

Em consumidores que usam o analyzer via MSBuild, inclua os projetos como arquivos adicionais quando necessario:

```xml
<ItemGroup>
  <AdditionalFiles Include="**\*.csproj" />
</ItemGroup>
```

Se nenhum `.csproj` for recebido como `AdditionalFiles`, a regra nao reporta diagnosticos.

## Segurança e limites

Arquivos MSBuild informados como `AdditionalFiles` sao processados com limites defensivos. Arquivos vazios, invalidos ou acima do limite configurado sao ignorados para evitar degradacao de build/IDE.

## Heuristica

A regra:

- filtra `AdditionalFiles` com caminho terminado em `.csproj`;
- le XML com parser endurecido e limite de tamanho;
- localiza elementos `PackageReference`;
- usa `Include` ou `Update` como nome do pacote;
- compara nomes com `StringComparer.OrdinalIgnoreCase`;
- ignora pacotes e projetos permitidos por configuracao;
- reporta apenas um diagnostico por pacote duplicado.

XML invalido, arquivo sem texto e `PackageReference` sem `Include` ou `Update` sao ignorados silenciosamente.

## Limitacoes conhecidas

- A regra nao tenta decidir se a duplicidade e sempre incorreta; ela apenas recomenda revisao.
- A regra nao analisa `Directory.Packages.props`, `PackageVersion` ou dependencias transitivas.
- A localizacao do diagnostico fica no inicio do `.csproj`, nao necessariamente no atributo do pacote.
- A regra so enxerga projetos enviados como `AdditionalFiles`.

## Impacto esperado

- Ajuda a revisar dependencias repetidas entre camadas.
- Reduz ruido ao permitir pacotes comuns de teste por padrao.
- Incentiva centralizacao de dependencias e uso de project references quando fizer sentido.
