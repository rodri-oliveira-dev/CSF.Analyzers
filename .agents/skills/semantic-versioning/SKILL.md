---
name: semantic-versioning
description: Use esta skill ao alterar regras ARCH, severidades, opcoes de .editorconfig, empacotamento NuGet ou documentacao de release do Swa.Analyzers.
---

# Objetivo

Garantir que mudancas no pacote `Swa.Analyzers` sejam classificadas corretamente como PATCH, MINOR ou MAJOR, mantendo commits semanticos, `CHANGELOG.md` e documentacao coerentes.

# Fonte oficial da versao

A versao publicada do pacote e calculada por GitVersion a partir de:

```text
GitVersion.yml
```

O workflow `.github/workflows/release.yml` usa o output `semVer` do GitVersion como fonte unica para `PackageVersion`, tag `v{SemVer}` e GitHub Release.

Nao atualize `VersionPrefix` manualmente para preparar release. O projeto nao usa mais `VersionPrefix` como fonte da versao publicada.

Sempre que uma tarefa alterar regras ARCH, empacotamento NuGet, severidade padrao, opcoes `.editorconfig` ou documentacao publica de release, o agente deve decidir qual incremento semantico e esperado e garantir que a mensagem de commit reflita esse incremento.

# Politica de versionamento

O projeto segue Semantic Versioning.

Formato:

```text
MAJOR.MINOR.PATCH
```

Como o pacote ja passou de `1.0.0`:

* PATCH: correcao de bug, falso positivo, falso negativo, documentacao publica ou ajuste sem mudanca incompativel.
* MINOR: nova regra ou nova capacidade compativel.
* MAJOR: breaking change.

# PATCH

Use PATCH quando:

* corrigir falso positivo ou falso negativo dentro do escopo atual da regra;
* corrigir bug em parsing de `.editorconfig`;
* ajustar documentacao publica de regra, README, release ou pacote;
* ajustar SampleApp;
* melhorar mensagem sem mudar significado;
* corrigir build ou empacotamento sem impacto para consumidores.

Exemplo:

```text
1.1.0 -> 1.1.1
```

Ao aplicar PATCH, atualize:

* `CHANGELOG.md`;
* mensagem de commit com `fix:` ou `perf:` quando a mudanca deve incrementar patch.

# MINOR

Use MINOR quando:

* adicionar nova regra ARCH;
* adicionar nova opcao de `.editorconfig`;
* ampliar suporte compativel de uma regra existente;
* adicionar documentacao e exemplos de uma nova regra;
* adicionar nova validacao com severidade padrao Info ou Warning compativel.

Exemplo:

```text
1.1.1 -> 1.2.0
```

Ao aplicar MINOR, atualize:

* `CHANGELOG.md`;
* mensagem de commit com `feat:` quando a mudanca deve incrementar minor.

# MAJOR / BREAKING CHANGE

Considere breaking change quando:

* remover regra existente;
* renomear ID de regra;
* mudar severidade padrao para nivel mais restritivo;
* mudar valor default de opcao `.editorconfig`;
* remover ou renomear opcao `.editorconfig`;
* alterar comportamento de regra existente gerando diagnosticos novos em grande volume;
* exigir SDK, linguagem ou target framework incompativel;
* alterar empacotamento NuGet de forma incompativel.

Exemplo:

```text
1.1.1 -> 2.0.0
```

Ao aplicar MAJOR, atualize:

* `CHANGELOG.md`;
* mensagem de commit com `!` no tipo ou `BREAKING CHANGE:` no corpo;
* documentacao da regra ou de release explicando o breaking change.

# Regras para novas ARCH

Ao criar nova regra:

* adicionar ID em `src/Swa.Analyzers.Core/RuleIdentifiers.cs`;
* adicionar analyzer em `src/Swa.Analyzers.Core/Rules`;
* adicionar testes em `tests/Swa.Analyzers.Tests/Rules`;
* adicionar documentacao em `docs/rules/ARCH###.md`;
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
Nova versao: 1.2.0
```

# Severidade padrao

Use:

* Warning para regra objetiva e baixo falso positivo.
* Info para regra heuristica, opinativa ou de adocao gradual.
* Warning para seguranca/confiabilidade critica.
* Info para regras experimentais.

Mudanca posterior de severidade padrao pode ser breaking change, principalmente quando a severidade fica mais restritiva.

Exemplo:

```text
ARCH030 era Info.
Passou para Warning.
Isso pode quebrar pipelines de consumidores que tratam warning como erro.
Classifique como MAJOR depois do 1.0.0.
```

# Regras sobre CHANGELOG.md

Toda mudanca relevante para consumidores do pacote deve atualizar `CHANGELOG.md`.

Ajustes apenas em `.agents/skills`, relatorios internos em `docs/reviews` ou orientacoes operacionais que nao alterem regra, empacotamento, release publica ou comportamento do pacote nao exigem bump de `VersionPrefix` nem entrada no `CHANGELOG.md`.

Durante o desenvolvimento, registre mudancas em:

```markdown
## [Unreleased]
```

Ao preparar uma release, mova os itens para uma secao versionada:

```markdown
## [1.2.0] - 2026-05-04
```

Depois recrie a secao vazia de `[Unreleased]`.

# Checklist antes de finalizar

Antes de concluir a tarefa, responda internamente:

1. A mudanca adiciona nova regra?

   * Sim: MINOR.
2. A mudanca altera comportamento de regra existente?

   * Se compativel: PATCH ou MINOR.
   * Se incompativel: MAJOR.
3. A mudanca altera severidade padrao?

   * Se mais restritiva: MAJOR.
4. A mudanca altera opcao `.editorconfig`?

   * Nova opcao: MINOR.
   * Remocao, renomeacao ou default alterado: MAJOR.
5. O `CHANGELOG.md` foi atualizado?
6. A mensagem de commit corresponde ao incremento esperado pelo GitVersion?
7. Se foi criada nova regra ARCH, o commit usa `feat:`?
8. Se foi correcao sem regra nova, o commit usa `fix:` ou `perf:` quando deve gerar PATCH?
9. Se foi breaking change, o commit usa `!` ou corpo com `BREAKING CHANGE:`?
10. README, documentacao, testes, SampleApp e `AnalyzerReleases.Unshipped.md` foram atualizados quando aplicavel?

# Regra final obrigatoria

Nao finalize uma tarefa que altere regras ARCH, severidades, opcoes `.editorconfig`, empacotamento NuGet ou documentacao de release publica sem verificar e, quando necessario, atualizar:

```text
CHANGELOG.md
GitVersion.yml
docs/release.md
```
