---
name: semantic-versioning
description: Use esta skill ao alterar regras REL/ARC/TST, severidades, opcoes de .editorconfig, empacotamento NuGet ou documentacao de release do CSF.Analyzers.
---

# Objetivo

Garantir que mudancas nos pacotes `CSF.Analyzers.Reliability`, `CSF.Analyzers.Architecture` e `CSF.Analyzers.Testing` sejam classificadas corretamente como PATCH, MINOR ou MAJOR, mantendo commits semanticos, `CHANGELOG.md` e documentacao coerentes.

# Fonte oficial da versao

A versao publicada e calculada por GitVersion a partir de `GitVersion.yml`.

O workflow `.github/workflows/release.yml` usa o output `semVer` do GitVersion como fonte unica para `PackageVersion`, tag `v{SemVer}` e GitHub Release.

Nao atualize `VersionPrefix` manualmente para preparar release.

# Politica de versionamento

O projeto segue Semantic Versioning.

- PATCH: correcao de bug, falso positivo, falso negativo, documentacao publica ou ajuste sem mudanca incompativel.
- MINOR: nova regra ou nova capacidade compativel.
- MAJOR: breaking change, incluindo remocao ou renumeracao de regra, alteracao incompativel de severidade, opcao publica ou empacotamento.

# Mudancas de regra

Ao criar ou alterar regra, atualize quando aplicavel:

- `src/CSF.Analyzers.<Pacote>/RuleIdentifiers.cs`;
- `src/CSF.Analyzers.<Pacote>/Rules`;
- `tests/CSF.Analyzers.<Pacote>.Tests/Rules`;
- `docs/rules/<grupo>/<ID>.md`;
- `samples/CSF.Analyzers.<Pacote>.Sample/<ID em PascalCase>`;
- `README.md`;
- `src/CSF.Analyzers.<Pacote>/AnalyzerReleases.Unshipped.md`;
- `CHANGELOG.md`.

# Checklist antes de finalizar

1. A mudanca adiciona nova regra?
2. A mudanca altera comportamento de regra existente?
3. A mudanca altera severidade padrao?
4. A mudanca altera opcao `.editorconfig`?
5. O `CHANGELOG.md` precisa ser atualizado?
6. A mensagem de commit corresponde ao incremento esperado pelo GitVersion?
7. Se for breaking change, o commit usa `!` ou corpo com `BREAKING CHANGE:`?
8. README, documentacao, testes, samples e release metadata foram atualizados quando aplicavel?

# Regra final

Nao finalize uma tarefa que altere regras, severidades, opcoes `.editorconfig`, empacotamento NuGet ou documentacao de release publica sem verificar e, quando necessario, atualizar:

```text
CHANGELOG.md
GitVersion.yml
docs/release.md
```
