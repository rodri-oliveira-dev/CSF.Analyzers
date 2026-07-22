# Changelog

## [Unreleased]

### Adicionado


### Alterado

- Migrated release versioning from project `VersionPrefix` to GitVersion, using semantic commits to calculate the NuGet package version and GitHub Release tag.
- Formalizada a validaÃ§Ã£o de metadados de release shipped e unshipped dos analyzers.

### Corrigido

- Fixed the release workflow order so the .NET SDK is installed before GitVersion runs `dotnet tool install`.

### Breaking Changes

- None.

## [1.0.0] - 2026-05-01

Primeira versÃ£o estÃ¡vel do pacote `Swa.Analyzers`.

### Adicionado

- IncluÃ­da documentaÃ§Ã£o de regras em `docs/rules` e exemplos manuais em `src/Swa.Analyzers.SampleApp`.
- Adicionada validaÃ§Ã£o local e de CI para regras ARCH, documentaÃ§Ã£o, testes, SampleApp, atualizaÃ§Ãµes de changelog e mudanÃ§as de versÃ£o do pacote.
- Adicionado suporte no workflow de release para gerar pacotes `.nupkg` e `.snupkg` a partir do `VersionPrefix` do projeto.

### Alterado

- Centralizados helpers de parsing de opÃ§Ãµes dos analyzers para valores booleanos e arrays de strings em `.editorconfig`, alÃ©m de matching wildcard compartilhado sem alterar comportamento das regras.
- Documented `.editorconfig` fallback behavior consistently for public analyzer options.
- Shared JSON string-array parsing across configurable analyzers and added support for escaped unicode values in `.editorconfig` options.
- Documented the release process and package version source of truth.
- Endurecida a criaÃ§Ã£o de GitHub Release para usar `VersionPrefix`, criar a tag `v1.0.0`, recusar tags/releases existentes e manter a publicaÃ§Ã£o NuGet inativa atÃ© configuraÃ§Ã£o explÃ­cita de governanÃ§a.

### Corrigido


### Breaking Changes

- None.

## [0.2.0] - 2026-04-30

- Adicionada validaÃ§Ã£o de consistÃªncia de release para regras ARCH, documentaÃ§Ã£o, testes, SampleApp, atualizaÃ§Ãµes de changelog e mudanÃ§as de versÃ£o do pacote.
