# Auditoria de documentação

Data da auditoria: 2026-07-23.

## Escopo

Foram revisados:

- `README.md`;
- `docs/adoption.md`;
- `docs/editorconfig-profiles.md`;
- `docs/migration-v2.md`;
- `docs/contributing-rules.md`;
- `docs/release.md`;
- `docs/packages/reliability.md`;
- `docs/packages/architecture.md`;
- `docs/packages/testing.md`;
- `docs/rules/reliability/REL001.md` a `REL006.md`;
- `docs/rules/architecture/ARC001.md` a `ARC006.md`;
- `docs/rules/testing/TST001.md` e `TST002.md`;
- `docs/reviews/rules-analyzer-overlap.md`;
- documentos históricos e de revisão em `docs/history`, `docs/specs` e `docs/reviews`;
- `src/Swa.Analyzers.{Reliability,Architecture,Testing}/RuleIdentifiers.cs`;
- `src/Swa.Analyzers.{Reliability,Architecture,Testing}/AnalyzerReleases.Shipped.md`;
- `src/Swa.Analyzers.{Reliability,Architecture,Testing}/AnalyzerReleases.Unshipped.md`;
- analyzers, testes e samples correspondentes às regras ativas;
- `.editorconfig`, `samples/**/.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, workflows em `.github/workflows` e scripts de validação.

## Problemas encontrados

| Classificação | Problema |
| ------------- | -------- |
| Erro factual | `docs/migration-v2.md` listava os pacotes v2 sem `REL005`, `REL006` e `ARC006`, embora esses IDs já existam em `RuleIdentifiers`, descritores, testes, samples e `AnalyzerReleases.Unshipped.md`. |
| Erro factual | O README e as páginas de pacote mostravam comandos `dotnet add package` sem deixar suficientemente claro que a publicação no NuGet.org ainda não está habilitada. A API pública do NuGet.org retornou `404` para os três package IDs em 2026-07-23. |
| Clareza | O README não tinha um caminho curto de adoção separado da lista completa de regras. |
| Clareza | As páginas de pacote explicavam regras e opções, mas pouco sobre quando instalar, quando calibrar e quais regras são opt-in por política contextual. |
| Terminologia | Algumas páginas recentes usavam português sem acentuação e termos inconsistentes como `Estado padrao`, `Opcoes publicas` e `Configuracao`. |
| Navegação | `ARC006` não tinha seção própria de relação com ferramentas externas, embora o documento de overlap já discutisse essa relação. |
| Documentação obsoleta | Documentos de specs e revisões recentes preservam decisões e validações históricas. Eles continuam úteis, mas não devem ser lidos como guia de adoção atual. |

## Correções realizadas

- O README agora separa status de publicação, instalação, quick start, tabela de regras e guias.
- A instalação foi contextualizada: os comandos `dotnet add package` são o formato esperado quando os pacotes estiverem no NuGet.org ou em feed privado/local.
- As páginas dos pacotes explicam quando instalar, quais regras são habilitadas por padrão, quais são opt-in e como configurar opções específicas.
- `docs/migration-v2.md` foi atualizado para incluir `REL005`, `REL006` e `ARC006` como regras ativas da linha v2, com seção própria para regras novas sem ID v1 equivalente.
- `docs/adoption.md` passou a diferenciar projeto novo e projeto legado.
- `REL005`, `REL006` e `TST001` receberam ajustes editoriais de terminologia e acentuação sem mudar exemplos ou comportamento documentado.
- `ARC006` recebeu seção de relação com ferramentas externas.

## Pontos não alterados

- `AnalyzerReleases.Unshipped.md` usa `Disabled` para regras opt-in. Isso foi mantido porque é a convenção de release metadata do projeto para `isEnabledByDefault: false`, enquanto a documentação de usuário usa `Opt-in` e informa a severidade base `Info`.
- `docs/history/*` e `docs/specs/next-analyzers/*` foram mantidos como documentos históricos ou de planejamento. Referências a `ARCH###` nesses arquivos são legítimas.
- `docs/dependency-update-report.md` foi mantido como relatório datado de uma auditoria de dependências, não como fonte atual de versão mais recente.
- Não foram adicionados testes, porque a tarefa é documental e os exemplos existentes já são cobertos por testes e samples correspondentes.

## Possíveis inconsistências de implementação

Nenhuma divergência encontrada exigiu alteração de código nesta tarefa.

## Resultado

READY WITH NOTES.

A documentação atual representa os três pacotes ativos, os IDs `REL###`, `ARC###` e `TST###`, o status opt-in/default das regras e o estado real de publicação. A nota restante é operacional: enquanto a publicação no NuGet.org continuar comentada no workflow, consumidores precisam usar GitHub Release ou feed privado/local para instalar os pacotes.
