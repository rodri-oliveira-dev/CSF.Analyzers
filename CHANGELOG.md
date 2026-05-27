# Changelog

## [Unreleased]

### Adicionado

- Adicionada a ARCH033 para detectar chamadas a `BuildServiceProvider()` em `IServiceCollection` durante o registro de serviços, com suporte à opção `.editorconfig` `dotnet_diagnostic.ARCH033.ignore_tests`.

### Alterado

- Migrated release versioning from project `VersionPrefix` to GitVersion, using semantic commits to calculate the NuGet package version and GitHub Release tag.
- Formalizada a validação de metadados de release shipped e unshipped dos analyzers.

### Corrigido

- Fixed the release workflow order so the .NET SDK is installed before GitVersion runs `dotnet tool install`.
- Corrigida a ARCH010 para ignorar métodos síncronos que expõem parâmetros ou overloads com `CancellationToken`, mantendo a regra restrita a chamadas async.

### Breaking Changes

- None.

## [1.0.0] - 2026-05-01

Primeira versão estável do pacote `Swa.Analyzers`.

### Adicionado

- Published the stable baseline of analyzer rules ARCH001 through ARCH032, covering architecture, reliability, performance, security, observability, design, testability and test-quality conventions.
- Incluída documentação de regras em `docs/rules` e exemplos manuais em `src/Swa.Analyzers.SampleApp`.
- Adicionada validação local e de CI para regras ARCH, documentação, testes, SampleApp, atualizações de changelog e mudanças de versão do pacote.
- Adicionado suporte no workflow de release para gerar pacotes `.nupkg` e `.snupkg` a partir do `VersionPrefix` do projeto.

### Alterado

- Centralizados helpers de parsing de opções dos analyzers para valores booleanos e arrays de strings em `.editorconfig`, além de matching wildcard compartilhado sem alterar comportamento das regras.
- Documented `.editorconfig` fallback behavior consistently for public analyzer options.
- Shared JSON string-array parsing across configurable analyzers and added support for escaped unicode values in `.editorconfig` options.
- Documented the release process and package version source of truth.
- Endurecida a criação de GitHub Release para usar `VersionPrefix`, criar a tag `v1.0.0`, recusar tags/releases existentes e manter a publicação NuGet inativa até configuração explícita de governança.

### Corrigido

- Endurecido o tratamento de configurações malformadas ou excessivas dos analyzers ARCH020, ARCH027 e ARCH030.
- Endurecido o parsing XML de `AdditionalFiles` MSBuild em ARCH030 e ARCH032 com limites defensivos de tamanho e configurações de leitor XML que proíbem DTD.

### Breaking Changes

- None.

## [0.2.0] - 2026-04-30

- Adicionada validação de consistência de release para regras ARCH, documentação, testes, SampleApp, atualizações de changelog e mudanças de versão do pacote.
