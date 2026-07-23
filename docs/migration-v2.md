# Migração para a v2

## Breaking change

A versão 2.0 divide o pacote único da linha 1.x em três pacotes NuGet independentes e renumera todos os diagnósticos ativos. A implementação v2 não emite IDs `ARCH###`.

Consumidores devem atualizar referências de pacote, `.editorconfig`, suppressions, `NoWarn`, baselines, documentação interna e pipelines.

## Instalação antes/depois

Antes:

```xml
<PackageReference Include="Swa.Analyzers" PrivateAssets="all" />
```

Depois, escolha os pacotes necessários:

```xml
<PackageReference Include="Swa.Analyzers.Reliability" PrivateAssets="all" />
<PackageReference Include="Swa.Analyzers.Architecture" PrivateAssets="all" />
<PackageReference Include="Swa.Analyzers.Testing" PrivateAssets="all" />
```

Não há metapacote `Swa.Analyzers` na v2 inicial.

## Pacotes de destino

| Pacote | IDs ativos |
| ------ | ---------- |
| `Swa.Analyzers.Reliability` | `REL001`, `REL002`, `REL003`, `REL004`, `REL005`, `REL006` |
| `Swa.Analyzers.Architecture` | `ARC001`, `ARC002`, `ARC003`, `ARC004`, `ARC005`, `ARC006` |
| `Swa.Analyzers.Testing` | `TST001`, `TST002` |

## Mapeamento de regras mantidas

| ID v1 | ID v2 | Pacote v2 | Estado padrão v2 |
| ----- | ----- | --------- | ---------------- |
| `ARCH016` | `REL001` | `Swa.Analyzers.Reliability` | habilitada, warning |
| `ARCH017` | `REL002` | `Swa.Analyzers.Reliability` | habilitada, warning |
| `ARCH021` | `REL003` | `Swa.Analyzers.Reliability` | opt-in, info |
| `ARCH022` | `REL004` | `Swa.Analyzers.Reliability` | habilitada, warning |
| `ARCH020` | `ARC001` | `Swa.Analyzers.Architecture` | habilitada, warning |
| `ARCH027` | `ARC002` | `Swa.Analyzers.Architecture` | habilitada, warning |
| `ARCH015` | `ARC003` | `Swa.Analyzers.Architecture` | opt-in, info |
| `ARCH029` | `ARC004` | `Swa.Analyzers.Architecture` | opt-in, info |
| `ARCH032` | `ARC005` | `Swa.Analyzers.Architecture` | opt-in, info |
| `ARCH005` | `TST001` | `Swa.Analyzers.Testing` | opt-in, info |
| `ARCH006` | `TST002` | `Swa.Analyzers.Testing` | opt-in, info |

## Regras novas na linha v2

As regras abaixo não têm ID v1 equivalente. Trate-as como novas políticas ao migrar:

| ID v2 | Pacote v2 | Estado padrão v2 |
| ----- | --------- | ---------------- |
| `REL005` | `Swa.Analyzers.Reliability` | habilitada, warning |
| `REL006` | `Swa.Analyzers.Reliability` | habilitada, warning |
| `ARC006` | `Swa.Analyzers.Architecture` | opt-in, info |

## Regras removidas

As regras abaixo não possuem analyzer ativo na v2:

`ARCH001`, `ARCH002`, `ARCH003`, `ARCH004`, `ARCH007`, `ARCH008`, `ARCH009`, `ARCH010`, `ARCH011`, `ARCH012`, `ARCH013`, `ARCH014`, `ARCH018`, `ARCH019`, `ARCH023`, `ARCH024`, `ARCH025`, `ARCH026`, `ARCH028`, `ARCH030`, `ARCH031`, `ARCH033`.

Remova suppressions, `NoWarn` e configurações dessas regras depois de confirmar que nenhum pacote v2 as emite.

## `.editorconfig`

Atualize a parte do ID e preserve o nome da opção quando a regra foi mantida.

| Chave v1 | Chave v2 |
| -------- | -------- |
| `dotnet_diagnostic.ARCH015.route_language` | `dotnet_diagnostic.ARC003.route_language` |
| `dotnet_diagnostic.ARCH015.additional_verbs` | `dotnet_diagnostic.ARC003.additional_verbs` |
| `dotnet_diagnostic.ARCH020.allowed_routes` | `dotnet_diagnostic.ARC001.allowed_routes` |
| `dotnet_diagnostic.ARCH020.allowed_methods` | `dotnet_diagnostic.ARC001.allowed_methods` |
| `dotnet_diagnostic.ARCH020.ignored_namespaces` | `dotnet_diagnostic.ARC001.ignored_namespaces` |
| `dotnet_diagnostic.ARCH027.core_namespace_patterns` | `dotnet_diagnostic.ARC002.core_namespace_patterns` |
| `dotnet_diagnostic.ARCH027.forbidden_namespace_patterns` | `dotnet_diagnostic.ARC002.forbidden_namespace_patterns` |
| `dotnet_diagnostic.ARCH027.allowed_namespace_patterns` | `dotnet_diagnostic.ARC002.allowed_namespace_patterns` |
| `dotnet_diagnostic.ARCH027.ignore_tests` | `dotnet_diagnostic.ARC002.ignore_tests` |
| `dotnet_diagnostic.ARCH029.entity_namespaces` | `dotnet_diagnostic.ARC004.entity_namespaces` |
| `dotnet_diagnostic.ARCH029.entity_base_types` | `dotnet_diagnostic.ARC004.entity_base_types` |
| `dotnet_diagnostic.ARCH029.allow_internal_setters` | `dotnet_diagnostic.ARC004.allow_internal_setters` |
| `dotnet_diagnostic.ARCH032.ignored_properties` | `dotnet_diagnostic.ARC005.ignored_properties` |
| `dotnet_diagnostic.ARCH032.compare_values` | `dotnet_diagnostic.ARC005.compare_values` |

Antes:

```ini
[*.cs]
dotnet_diagnostic.ARCH020.severity = warning
dotnet_diagnostic.ARCH027.core_namespace_patterns = "*.Domain;*.Application"
dotnet_diagnostic.ARCH005.severity = info
```

Depois:

```ini
[*.cs]
dotnet_diagnostic.ARC001.severity = warning
dotnet_diagnostic.ARC002.core_namespace_patterns = "*.Domain;*.Application"
dotnet_diagnostic.TST001.severity = info
```

Regras opt-in precisam de severidade explícita:

```ini
[*.cs]
dotnet_diagnostic.REL003.severity = info
dotnet_diagnostic.ARC003.severity = info
dotnet_diagnostic.ARC004.severity = info
dotnet_diagnostic.ARC006.severity = info
dotnet_diagnostic.TST001.severity = info
dotnet_diagnostic.TST002.severity = info

[*.csproj]
dotnet_diagnostic.ARC005.severity = info
```

Para `ARC005` em consumo via NuGet, prefira ativar a severidade em `.globalconfig`, pois a regra reporta diagnósticos em arquivos MSBuild recebidos como `AdditionalFiles`:

```ini
is_global = true
dotnet_diagnostic.ARC005.severity = info
```

## Suppressions e `NoWarn`

Suppressions em código, `GlobalSuppressions.cs`, `NoWarn`, baselines de CI e SARIF antigos com `ARCH###` não suprimem diagnósticos `REL###`, `ARC###` ou `TST###`.

Antes:

```csharp
#pragma warning disable ARCH020
```

Depois:

```csharp
#pragma warning disable ARC001
```

Para regras removidas, apague a suppression em vez de renumerar.

## Pipelines

Atualize etapas que empacotavam ou validavam `Swa.Analyzers` como pacote único. A v2 deve restaurar, compilar, testar e empacotar os três projetos ativos:

```powershell
dotnet restore ./Swa.Analyzers.slnx --locked-mode
dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore
dotnet test ./Swa.Analyzers.slnx --configuration Release -m:1
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1
```

## Checklist de migração

1. Remova `Swa.Analyzers` da linha 1.x.
2. Instale os pacotes v2 necessários.
3. Troque IDs e opções de `.editorconfig` conforme a tabela.
4. Recrie suppressions e `NoWarn` apenas para regras mantidas.
5. Remova configurações de regras v1 removidas.
6. Recrie baselines de CI ou SARIF.
7. Confirme que regras opt-in foram ativadas somente quando representam política real do time.
