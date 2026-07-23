# Swa.Analyzers.Architecture

## Objetivo

`Swa.Analyzers.Architecture` aplica políticas contextuais de autorização, rotas HTTP, dependências de camadas, entidades de domínio e centralização MSBuild.

## Público-alvo

Use em APIs ASP.NET, soluções com camadas core bem definidas, projetos com DDD e repositórios que centralizam propriedades em `Directory.Build.props`.

## Instalação

Use este comando quando o pacote estiver publicado no NuGet.org ou disponível no feed privado/local configurado no projeto. A publicação no NuGet.org ainda não está habilitada no workflow de release.

```powershell
dotnet add package Swa.Analyzers.Architecture
```

Em projetos com Central Package Management, declare a versão em `Directory.Packages.props` e mantenha o `PackageReference` sem `Version`.

## Regras

| ID | Categoria | Severidade | Estado |
| -- | --------- | ---------- | ------ |
| [`ARC001`](../rules/architecture/ARC001.md) | Security | `Warning` | Habilitada |
| [`ARC002`](../rules/architecture/ARC002.md) | Architecture | `Warning` | Habilitada |
| [`ARC003`](../rules/architecture/ARC003.md) | Design | `Info` | Opt-in |
| [`ARC004`](../rules/architecture/ARC004.md) | Design | `Info` | Opt-in |
| [`ARC005`](../rules/architecture/ARC005.md) | Maintainability | `Info` | Opt-in |
| [`ARC006`](../rules/architecture/ARC006.md) | Architecture | `Info` | Opt-in |

## Configuração

```ini
[*.cs]
dotnet_diagnostic.ARC001.allowed_routes = ["/health", "/metrics"]
dotnet_diagnostic.ARC002.core_namespace_patterns = "*.Domain;*.Application"
dotnet_diagnostic.ARC002.forbidden_namespace_patterns = "Microsoft.EntityFrameworkCore;Microsoft.AspNetCore;Npgsql"
dotnet_diagnostic.ARC002.allowed_namespace_patterns =
dotnet_diagnostic.ARC002.ignore_tests = true

# Opt-in para políticas organizacionais.
dotnet_diagnostic.ARC003.severity = info
dotnet_diagnostic.ARC004.severity = info
dotnet_diagnostic.ARC006.severity = info

[*.csproj]
dotnet_diagnostic.ARC005.severity = info
```

No consumo via NuGet, o pacote adiciona o `.csproj` e o `Directory.Build.props` do projeto como `AdditionalFiles` para `ARC005`. Como o diagnóstico é reportado em arquivo MSBuild no fim da compilação, ative a severidade de `ARC005` em `.globalconfig` quando o build não aplicar a severidade de `.editorconfig` a `AdditionalFiles`:

```ini
is_global = true
dotnet_diagnostic.ARC005.severity = info
```

`ARC004` e `ARC006` compartilham o mesmo classificador de entidades de domínio. Configure `dotnet_diagnostic.ARC004.entity_namespaces` e `dotnet_diagnostic.ARC004.entity_base_types` uma vez quando os marcadores padrão não forem suficientes.

## Quando instalar

Instale em APIs ASP.NET e soluções com políticas explícitas de autorização, fronteiras de camada, DDD ou centralização de propriedades MSBuild.

`ARC001` e `ARC002` são habilitadas por padrão. `ARC003`, `ARC004`, `ARC005` e `ARC006` são opt-in porque representam convenções de arquitetura ou governança que precisam ser calibradas para cada solução.

## Limitações

As regras dependem de sinais de ASP.NET, namespaces, tipos base, interfaces, rotas literais e arquivos MSBuild recebidos como `AdditionalFiles`. Elas não substituem revisão de arquitetura nem validam todos os formatos dinâmicos.

## Relação com analyzers externos

Analyzers externos podem cobrir práticas gerais de segurança, design ou build. Este pacote existe para políticas contextuais que precisam de configuração da solução.

## Quando não usar

Não use `ARC002`, `ARC003`, `ARC004`, `ARC005` ou `ARC006` como regra universal sem calibrar a arquitetura real. Em bibliotecas sem endpoints HTTP, `ARC001`, `ARC003` e `ARC006` normalmente não agregam valor.
