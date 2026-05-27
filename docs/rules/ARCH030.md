# ARCH030: Detecte PackageReference duplicado entre projetos

## Objetivo

Detectar quando o mesmo `PackageReference` aparece em mais de um `.csproj` recebido como `AdditionalFiles`.

Em soluções com camadas bem definidas, uma dependência repetida em vários projetos pode indicar acoplamento excessivo, falta de centralização ou uma dependência que deveria existir apenas em um adaptador mais externo.

## Quando a duplicidade é problema

A duplicidade merece revisão quando o mesmo pacote aparece em projetos de camadas diferentes, por exemplo domínio, aplicação e infraestrutura. Isso pode espalhar detalhes de framework para projetos que deveriam depender de abstrações ou de project references.

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

## Quando não é problema

Alguns pacotes são naturalmente repetidos em muitos projetos, especialmente pacotes de teste. A regra já permite por padrão:

- `Microsoft.NET.Test.Sdk`
- `xunit`
- `xunit.runner.visualstudio`
- `coverlet.collector`
- `FluentAssertions`
- `NSubstitute`
- `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`

Também pode ser aceitavel repetir pacotes em projetos de benchmark, testes de contrato, exemplos ou projetos auxiliares. Use `allowed_project_patterns` nesses casos.

## Código conforme

Centralize a dependência no projeto que realmente a utiliza ou exponha a funcionalidade por uma abstração:

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

## Configuração

A severidade padrão da regra é `Info`, conforme o descriptor do analyzer e a tabela pública do README.

A regra aceita arrays JSON em `.editorconfig`. As opções em formato JSON aceitam arrays de strings e escapes JSON comuns, incluindo unicode escapado:

```ini
[*.csproj]
dotnet_diagnostic.ARCH030.allowed_packages = ["Microsoft.NET.Test.Sdk", "xunit", "coverlet.collector"]
dotnet_diagnostic.ARCH030.allowed_project_patterns = ["*.Tests.csproj", "*.Benchmarks.csproj"]
```

`allowed_packages` define a lista de pacotes que podem aparecer em vários projetos. Quando a opção é omitida ou contém JSON inválido, a regra usa a allowlist padrão.

`allowed_project_patterns` remove projetos inteiros da análise. Os padrões aceitam `*` e são comparados com o nome do arquivo e com o caminho normalizado. O padrão é vazio.

Listas configuráveis são normalizadas e possuem limites defensivos de quantidade e tamanho para evitar custo excessivo durante build/IDE. Entradas vazias, duplicadas ou acima do limite são ignoradas.

### Fallback das opções

- `allowed_packages`: array JSON de strings; default é a allowlist padrão quando ausente ou malformado. Pacotes são aparados e comparados sem diferenciar maiúsculas de minúsculas. Entradas vazias são ignoradas. Um array JSON vazio substitui a allowlist por vazio.
- `allowed_project_patterns`: array JSON de strings; default vazio. Padrões são aparados, aceitam `*` e são comparados sem diferenciar maiúsculas de minúsculas. Entradas vazias, duplicadas ou acima do limite são ignoradas. JSON vazio, inválido ou malformado e ignorado.

O fallback de `allowed_packages` preserva a allowlist padrão; o de `allowed_project_patterns` é restritivo, pois não ignora projetos quando a configuração é inválida.

A severidade pode ser elevada normalmente via override de `.editorconfig` quando o projeto quiser tratar duplicidades como aviso:

```ini
[*.csproj]
dotnet_diagnostic.ARCH030.severity = warning
```

## AdditionalFiles

Roslyn não analisa `.csproj` como syntax trees C#. Por isso, a regra depende de os arquivos de projeto estarem disponíveis como `AdditionalFiles`.

Em consumidores que usam o analyzer via MSBuild, inclua os projetos como arquivos adicionais quando necessário:

```xml
<ItemGroup>
  <AdditionalFiles Include="**\*.csproj" />
</ItemGroup>
```

Se nenhum `.csproj` for recebido como `AdditionalFiles`, a regra não reporta diagnósticos.

## Segurança e limites

Arquivos MSBuild informados como `AdditionalFiles` são processados com limites defensivos. Arquivos vazios, inválidos ou acima do limite configurado são ignorados para evitar degradação de build/IDE.

## Heurística

A regra:

- filtra `AdditionalFiles` com caminho terminado em `.csproj`;
- le XML com parser endurecido e limite de tamanho;
- localiza elementos `PackageReference`;
- usa `Include` ou `Update` como nome do pacote;
- compara nomes com `StringComparer.OrdinalIgnoreCase`;
- ignora pacotes e projetos permitidos por configuração;
- reporta apenas um diagnóstico por pacote duplicado.

XML inválido, arquivo sem texto e `PackageReference` sem `Include` ou `Update` são ignorados silenciosamente.

## Limitações conhecidas

- A regra não tenta decidir se a duplicidade é sempre incorreta; ela apenas recomenda revisão.
- A regra não analisa `Directory.Packages.props`, `PackageVersion` ou dependências transitivas.
- A localização do diagnóstico fica no inicio do `.csproj`, não necessáriamente no atributo do pacote.
- A regra so enxerga projetos enviados como `AdditionalFiles`.

## Impacto esperado

- Ajuda a revisar dependências repetidas entre camadas.
- Reduz ruído ao permitir pacotes comuns de teste por padrão.
- Incentiva centralização de dependências e uso de project references quando fizer sentido.
