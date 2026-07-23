# Changelog

## [Unreleased]

### Adicionado

- Adicionada a regra opt-in `ARC006` ao pacote `CSF.Analyzers.Architecture` para evitar entidades de dominio diretamente em contratos HTTP.
- Adicionado target `buildTransitive` ao pacote `CSF.Analyzers.Architecture` para fornecer `.csproj` e `Directory.Build.props` como `AdditionalFiles` para `ARC005` em consumo via NuGet.
- Adicionados guardrails de performance para o pacote `CSF.Analyzers.Testing`.

### Alterado

- BREAKING: Renumerados os diagnosticos ativos para `REL###`, `ARC###` e `TST###`, com metadados v2 ainda em `AnalyzerReleases.Unshipped.md`.
- BREAKING: Atualizadas as chaves publicas de `.editorconfig` para os novos IDs dos diagnosticos.
- Migrated release versioning from project `VersionPrefix` to GitVersion, using semantic commits to calculate the NuGet package version and GitHub Release tag.
- Atualizada a infraestrutura de CI e release para validar, empacotar, inspecionar e anexar os seis artefatos dos tres pacotes v2 sem gerar o pacote legado.
- Formalizada a validaÃ§Ã£o de metadados de release shipped e unshipped dos analyzers.

### Corrigido

- Fixed the release workflow order so the .NET SDK is installed before GitVersion runs `dotnet tool install`.
- Atualizadas instrucoes internas, cobertura e documentacao publica para remover residuos da estrutura v1 baseada em Core/SampleApp e refletir os tres pacotes v2.

### Breaking Changes

- Os IDs `ARCH###` deixam de ser emitidos pelos pacotes ativos da v2. Consulte `docs/migration-v2.md`.
- As regras `REL003`, `ARC003`, `ARC004`, `ARC005`, `TST001` e `TST002` passam a ser opt-in com severidade base `Info`.

## [1.0.0] - 2026-05-01

Primeira versÃ£o estÃ¡vel do pacote `CSF.Analyzers`.

### Adicionado

- IncluÃ­da documentaÃ§Ã£o de regras em `docs/rules` e exemplos manuais em `src/CSF.Analyzers.SampleApp`.
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
