---
name: semantic-versioning
description: Use esta skill ao alterar regras ARCH, severidades, opções de .editorconfig, empacotamento NuGet ou documentação de release do Swa.Analyzers.
---

# Objetivo

Garantir que mudanças no pacote `Swa.Analyzers` sejam classificadas corretamente como PATCH, MINOR ou MAJOR, mantendo commits semânticos, `CHANGELOG.md` e documentação coerentes.

# Fonte oficial da versão

A versão publicada do pacote é calculada por GitVersion a partir de:

```text
GitVersion.yml
```

O workflow `.github/workflows/release.yml` usa o output `semVer` do GitVersion como fonte única para `PackageVersion`, tag `v{SemVer}` e GitHub Release.

Não atualize `VersionPrefix` manualmente para preparar release. O projeto não usa mais `VersionPrefix` como fonte da versão publicada.

Sempre que uma tarefa alterar regras ARCH, empacotamento NuGet, severidade padrão, opções `.editorconfig` ou documentação pública de release, o agente deve decidir qual incremento semântico é esperado e garantir que a mensagem de commit reflita esse incremento.

# Politica de versionamento

O projeto segue Semantic Versioning.

Formato:

```text
MAJOR.MINOR.PATCH
```

Como o pacote já passou de `1.0.0`:

* PATCH: correção de bug, falso positivo, falso negativo, documentação pública ou ajuste sem mudança incompativel.
* MINOR: nova regra ou nova capacidade compatível.
* MAJOR: breaking change.

# PATCH

Use PATCH quando:

* corrigir falso positivo ou falso negativo dentro do escopo atual da regra;
* corrigir bug em parsing de `.editorconfig`;
* ajustar documentação pública de regra, README, release ou pacote;
* ajustar SampleApp;
* melhorar mensagem sem mudar significado;
* corrigir build ou empacotamento sem impacto para consumidores.

Exemplo:

```text
1.1.0 -> 1.1.1
```

Ao aplicar PATCH, atualize:

* `CHANGELOG.md`;
* mensagem de commit com `fix:` ou `perf:` quando a mudança deve incrementar patch.

# MINOR

Use MINOR quando:

* adicionar nova regra ARCH;
* adicionar nova opção de `.editorconfig`;
* ampliar suporte compatível de uma regra existente;
* adicionar documentação e exemplos de uma nova regra;
* adicionar nova validação com severidade padrão Info ou Warning compatível.

Exemplo:

```text
1.1.1 -> 1.2.0
```

Ao aplicar MINOR, atualize:

* `CHANGELOG.md`;
* mensagem de commit com `feat:` quando a mudança deve incrementar minor.

# MAJOR / BREAKING CHANGE

Considere breaking change quando:

* remover regra existente;
* renomear ID de regra;
* mudar severidade padrão para nível mais restritivo;
* mudar valor default de opção `.editorconfig`;
* remover ou renomear opção `.editorconfig`;
* alterar comportamento de regra existente gerando diagnósticos novos em grande volume;
* exigir SDK, linguagem ou target framework incompativel;
* alterar empacotamento NuGet de forma incompativel.

Exemplo:

```text
1.1.1 -> 2.0.0
```

Ao aplicar MAJOR, atualize:

* `CHANGELOG.md`;
* mensagem de commit com `!` no tipo ou `BREAKING CHANGE:` no corpo;
* documentação da regra ou de release explicando o breaking change.

# Regras para novas ARCH

Ao criar nova regra:

* adicionar ID em `src/Swa.Analyzers.Core/RuleIdentifiers.cs`;
* adicionar analyzer em `src/Swa.Analyzers.Core/Rules`;
* adicionar testes em `tests/Swa.Analyzers.Tests/Rules`;
* adicionar documentação em `docs/rules/ARCH###.md`;
* adicionar exemplo em `src/Swa.Analyzers.SampleApp/Arch###`;
* atualizar `README.md`;
* atualizar `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md`;
* atualizar `CHANGELOG.md`;
* usar commit `feat:` para que GitVersion incremente MINOR.

Nova regra ARCH deve incrementar MINOR.

Exemplo:

```text
Versao atual: 1.1.0
Nova regra: ARCH034
Nova versão: 1.2.0
```

# Severidade padrão

Use:

* Warning para regra objetiva e baixo falso positivo.
* Info para regra heurística, opinativa ou de adoção gradual.
* Warning para segurança/confiabilidade crítica.
* Info para regras experimentais.

Mudanca posterior de severidade padrão pode ser breaking change, principalmente quando a severidade fica mais restritiva.

Exemplo:

```text
ARCH030 era Info.
Passou para Warning.
Isso pode quebrar pipelines de consumidores que tratam warning como erro.
Classifique como MAJOR depois do 1.0.0.
```

# Regras sobre CHANGELOG.md

Toda mudança relevante para consumidores do pacote deve atualizar `CHANGELOG.md`.

Ajustes apenas em `.agents/skills`, relatórios internos em `docs/reviews` ou orientações operacionais que não alterem regra, empacotamento, release pública ou comportamento do pacote não exigem bump de `VersionPrefix` nem entrada no `CHANGELOG.md`.

Durante o desenvolvimento, registre mudanças em:

```markdown
## [Unreleased]
```

Ao preparar uma release, mova os itens para uma seção versionada:

```markdown
## [1.2.0] - 2026-05-04
```

Depois recrie a seção vazia de `[Unreleased]`.

# Checklist antes de finalizar

Antes de concluir a tarefa, responda internamente:

1. A mudança adiciona nova regra?

   * Sim: MINOR.
2. A mudança altera comportamento de regra existente?

   * Se compatível: PATCH ou MINOR.
   * Se incompativel: MAJOR.
3. A mudança altera severidade padrão?

   * Se mais restritiva: MAJOR.
4. A mudança altera opção `.editorconfig`?

   * Nova opção: MINOR.
   * Remoção, renomeação ou default alterado: MAJOR.
5. O `CHANGELOG.md` foi atualizado?
6. A mensagem de commit corresponde ao incremento esperado pelo GitVersion?
7. Se foi criada nova regra ARCH, o commit usa `feat:`?
8. Se foi correção sem regra nova, o commit usa `fix:` ou `perf:` quando deve gerar PATCH?
9. Se foi breaking change, o commit usa `!` ou corpo com `BREAKING CHANGE:`?
10. README, documentação, testes, SampleApp e `AnalyzerReleases.Unshipped.md` foram atualizados quando aplicável?

# Regra final obrigatoria

Não finalize uma tarefa que altere regras ARCH, severidades, opções `.editorconfig`, empacotamento NuGet ou documentação de release pública sem verificar e, quando necessário, atualizar:

```text
CHANGELOG.md
GitVersion.yml
docs/release.md
```
