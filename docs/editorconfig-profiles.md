# Perfis de `.editorconfig`

Os perfis abaixo são pontos de partida. Eles não alteram a severidade padrão implementada nos pacotes; apenas mostram configurações copiáveis para consumidores.

Regras opt-in permanecem desabilitadas nos perfis genéricos, salvo quando o perfil representa explicitamente aquela política.

## Mapa das regras

| ID | Pacote | Padrão | Observação |
| -- | ------ | ------ | ---------- |
| `REL001` | Reliability | `warning` | Contexto ASP.NET; pode ser informativa em bases legadas. |
| `REL002` | Reliability | `warning` | Boa candidata a bloqueio após inventário. |
| `REL003` | Reliability | opt-in `info` | Ative em projetos EF Core com política de leitura sem tracking. |
| `REL004` | Reliability | `warning` | Útil para consultas EF Core materializadas cedo demais. |
| `REL005` | Reliability | `warning` | Evita operacoes concorrentes no mesmo `DbContext`. |
| `ARC001` | Architecture | `warning` | Exige decisão explícita de autorização. |
| `ARC002` | Architecture | `warning` | Configure namespaces antes de elevar severidade. |
| `ARC003` | Architecture | opt-in `info` | Política de API orientada a recursos. |
| `ARC004` | Architecture | opt-in `info` | Política DDD para entidades de domínio. |
| `ARC005` | Architecture | opt-in `info` | Requer `AdditionalFiles` com projetos e `Directory.Build.props`. |
| `TST001` | Testing | opt-in `info` | Política de uso de `NSubstitute.Arg.Any()`. |
| `TST002` | Testing | opt-in `info` | Política de precisão em `BeEquivalentTo()`. |

## recommended

Perfil inicial para projetos ativos. Mantém somente regras já habilitadas por padrão e reduz ruído em bases existentes.

```ini
[*.cs]
dotnet_diagnostic.REL001.severity = info
dotnet_diagnostic.REL002.severity = warning
dotnet_diagnostic.REL004.severity = info
dotnet_diagnostic.REL005.severity = warning
dotnet_diagnostic.ARC001.severity = warning
dotnet_diagnostic.ARC002.severity = info
```

## strict

Perfil para bases novas ou já saneadas. Não ativa regras opt-in de política local por padrão.

```ini
[*.cs]
dotnet_diagnostic.REL001.severity = warning
dotnet_diagnostic.REL002.severity = error
dotnet_diagnostic.REL004.severity = warning
dotnet_diagnostic.REL005.severity = error
dotnet_diagnostic.ARC001.severity = error
dotnet_diagnostic.ARC002.severity = warning
```

## reliability

Perfil para serviços ASP.NET e projetos com EF Core.

```ini
[*.cs]
dotnet_diagnostic.REL001.severity = warning
dotnet_diagnostic.REL002.severity = warning
dotnet_diagnostic.REL004.severity = warning
dotnet_diagnostic.REL005.severity = warning

# Ative quando consultas de leitura devem explicitar ausência de tracking.
dotnet_diagnostic.REL003.severity = info
```

## architecture

Perfil para camadas core e endpoints HTTP.

```ini
[*.cs]
dotnet_diagnostic.ARC001.severity = warning
dotnet_diagnostic.ARC002.severity = warning

dotnet_diagnostic.ARC002.core_namespace_patterns = "*.Domain;*.Application"
dotnet_diagnostic.ARC002.forbidden_namespace_patterns = "Microsoft.EntityFrameworkCore;Microsoft.AspNetCore;StackExchange.Redis;Npgsql"
dotnet_diagnostic.ARC002.allowed_namespace_patterns =
dotnet_diagnostic.ARC002.ignore_tests = true
```

## testing

Perfil para projetos que adotam as políticas de teste do pacote `Swa.Analyzers.Testing`.

```ini
[*.cs]
dotnet_diagnostic.TST001.severity = warning
dotnet_diagnostic.TST002.severity = warning
```

## ddd

Perfil para projetos com entidades de domínio e agregados.

```ini
[*.cs]
dotnet_diagnostic.ARC004.severity = warning
dotnet_diagnostic.ARC004.entity_namespaces = ["MyApp.Domain.Entities", "MyApp.Domain.Aggregates"]
dotnet_diagnostic.ARC004.entity_base_types = ["Entity", "AggregateRoot"]
dotnet_diagnostic.ARC004.allow_internal_setters = false
```

## legacy-safe

Perfil para primeira execução em bases grandes ou antigas.

```ini
[*.cs]
dotnet_diagnostic.REL001.severity = info
dotnet_diagnostic.REL002.severity = info
dotnet_diagnostic.REL003.severity = none
dotnet_diagnostic.REL004.severity = info
dotnet_diagnostic.REL005.severity = info
dotnet_diagnostic.ARC001.severity = info
dotnet_diagnostic.ARC002.severity = info
dotnet_diagnostic.ARC003.severity = none
dotnet_diagnostic.ARC004.severity = none
dotnet_diagnostic.TST001.severity = none
dotnet_diagnostic.TST002.severity = none

[*.csproj]
dotnet_diagnostic.ARC005.severity = none
```

## Perfis específicos adicionais

Para APIs orientadas a recursos, ative `ARC003` explicitamente:

```ini
[*.cs]
dotnet_diagnostic.ARC003.severity = warning
dotnet_diagnostic.ARC003.route_language = pt-BR
dotnet_diagnostic.ARC003.additional_verbs = ["ativar", "inativar", "recalcular"]
```

Para centralização de MSBuild, ative `ARC005` quando os arquivos de projeto forem passados como `AdditionalFiles`. No pacote NuGet, isso é feito automaticamente por um target `buildTransitive`. Para builds que não aplicam severidade de `.editorconfig` a diagnósticos em `AdditionalFiles`, use `.globalconfig`:

```ini
is_global = true
dotnet_diagnostic.ARC005.severity = info
dotnet_diagnostic.ARC005.compare_values = true
```
