---
name: semantic-versioning
description: Use esta skill ao alterar regras ARCH, severidades, opções de .editorconfig, empacotamento NuGet ou documentação de release do Swa.Analyzers.
---

# Objetivo

Garantir que mudanças no pacote Swa.Analyzers sejam classificadas corretamente como PATCH, MINOR ou MAJOR, mantendo `VersionPrefix`, `CHANGELOG.md` e documentação coerentes.

# Arquivo oficial de versão

A versão oficial do pacote fica em:

`src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj`

No atributo:

```xml
<VersionPrefix>x.y.z</VersionPrefix>
```

Sempre que uma tarefa alterar regras ARCH, empacotamento NuGet, severidade padrão, opções `.editorconfig` ou documentação de release, o agente deve decidir o próximo número de versão e atualizar esse atributo no mesmo PR.

Não deixe apenas como sugestão. Se a mudança exigir nova versão, edite obrigatoriamente o `VersionPrefix`.

Exemplo:

```xml
<VersionPrefix>0.1.0</VersionPrefix>
```

Se uma nova regra ARCH for criada:

```xml
<VersionPrefix>0.2.0</VersionPrefix>
```

# Política de versionamento

O projeto segue Semantic Versioning.

Formato:

```text
MAJOR.MINOR.PATCH
```

Enquanto o pacote estiver em `0.x`:

* PATCH: correções pequenas e compatíveis.
* MINOR: novas regras, novas opções ou melhorias compatíveis.
* MAJOR ou avanço planejado para `1.0.0`: estabilização pública ou mudança incompatível relevante.

Depois do `1.0.0`:

* PATCH: correção de bug, falso positivo, falso negativo, documentação ou ajuste sem mudança incompatível.
* MINOR: nova regra ou nova capacidade compatível.
* MAJOR: breaking change.

# PATCH

Use PATCH quando:

* corrigir falso positivo;
* corrigir falso negativo dentro do escopo atual da regra;
* corrigir bug em parsing de `.editorconfig`;
* ajustar documentação;
* ajustar SampleApp;
* melhorar mensagem sem mudar significado;
* corrigir build ou empacotamento sem impacto para consumidores.

Exemplo:

```text
0.2.0 -> 0.2.1
```

Ao aplicar PATCH, atualize:

* `CHANGELOG.md`;
* `src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj`, atributo `<VersionPrefix>`.

# MINOR

Use MINOR quando:

* adicionar nova regra ARCH;
* adicionar nova opção de `.editorconfig`;
* ampliar suporte compatível de uma regra existente;
* adicionar documentação e exemplos de uma nova regra;
* adicionar nova validação com severidade padrão Info ou Warning compatível.

Exemplo:

```text
0.2.1 -> 0.3.0
```

Ao aplicar MINOR, atualize:

* `CHANGELOG.md`;
* `src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj`, atributo `<VersionPrefix>`.

# MAJOR / BREAKING CHANGE

Considere breaking change quando:

* remover regra existente;
* renomear ID de regra;
* mudar severidade padrão para nível mais restritivo;
* mudar valor default de opção `.editorconfig`;
* remover ou renomear opção `.editorconfig`;
* alterar comportamento de regra existente gerando diagnósticos novos em grande volume;
* exigir SDK, linguagem ou target framework incompatível;
* alterar empacotamento NuGet de forma incompatível.

Exemplo:

```text
1.4.2 -> 2.0.0
```

Ao aplicar MAJOR, atualize:

* `CHANGELOG.md`;
* `src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj`, atributo `<VersionPrefix>`;
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
* atualizar obrigatoriamente o atributo `<VersionPrefix>` em `src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj`.

Nova regra ARCH deve incrementar MINOR.

Exemplo:

```text
Versão atual: 0.1.0
Nova regra: ARCH028
Nova versão: 0.2.0
```

# Severidade padrão

Use:

* Warning para regra objetiva e baixo falso positivo.
* Info para regra heurística, opinativa ou de adoção gradual.
* Warning para segurança/confiabilidade crítica.
* Info para regras experimentais.

Mudança posterior de severidade padrão pode ser breaking change, principalmente quando a severidade fica mais restritiva.

Exemplo:

```text
ARCH030 era Info.
Passou para Warning.
Isso pode quebrar pipelines de consumidores que tratam warning como erro.
Classifique como MAJOR depois do 1.0.0.
```

# Regras sobre CHANGELOG.md

Toda mudança relevante deve atualizar `CHANGELOG.md`.

Durante o desenvolvimento, registre mudanças em:

```markdown
## [Unreleased]
```

Ao preparar uma release, mova os itens para uma seção versionada:

```markdown
## [0.2.0] - 2026-04-30
```

Depois recrie a seção vazia de `[Unreleased]`.

# Checklist antes de finalizar

Antes de concluir a tarefa, responda internamente:

1. A mudança adiciona nova regra?

   * Sim: MINOR.
2. A mudança altera comportamento de regra existente?

   * Se compatível: PATCH ou MINOR.
   * Se incompatível: MAJOR.
3. A mudança altera severidade padrão?

   * Se mais restritiva: MAJOR.
4. A mudança altera opção `.editorconfig`?

   * Nova opção: MINOR.
   * Remoção, renomeação ou default alterado: MAJOR.
5. O `CHANGELOG.md` foi atualizado?
6. O atributo `<VersionPrefix>` foi atualizado em `src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj`?
7. Se foi criada nova regra ARCH, o `VersionPrefix` incrementou MINOR?
8. Se foi correção sem regra nova, o `VersionPrefix` incrementou PATCH?
9. Se foi breaking change, o `VersionPrefix` incrementou MAJOR?
10. README, documentação, testes, SampleApp e `AnalyzerReleases.Unshipped.md` foram atualizados quando aplicável?

# Regra final obrigatória

Não finalize uma tarefa que altere regras ARCH, severidades, opções `.editorconfig`, empacotamento NuGet ou documentação de release sem verificar e, quando necessário, atualizar:

```text
src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj
```

O atributo obrigatório é:

```xml
<VersionPrefix>x.y.z</VersionPrefix>
```
