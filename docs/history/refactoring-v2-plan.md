# Plano histórico de refatoração estrutural para a versão 2.0

Este documento e a fonte de verdade persistente para a refatoracao estrutural da versao 2.0 do `Swa.Analyzers`. As proximas etapas podem ser executadas em outras janelas do Codex; portanto, qualquer decisao que oriente a migracao deve ser preservada aqui antes de ser implementada.

Esta etapa e apenas documental. Nao remove, move, renumera ou altera codigo produtivo, testes, SampleApp, workflows, scripts ou metadados de release.

## Estado atual inspecionado

Inventario confirmado antes da criacao deste plano:

- Solucao principal: `Swa.Analyzers.slnx`.
- Projetos atuais:
  - `src/Swa.Analyzers.Core`: projeto `netstandard2.0`, pacote atual `Swa.Analyzers`, analyzers, identificadores e release tracking.
  - `src/Swa.Analyzers.CodeFixes`: projeto `netstandard2.0`, hoje usado pelo code fix da `ARCH001`.
  - `tests/Swa.Analyzers.Tests`: projeto unico de testes automatizados em `net10.0`.
  - `src/Swa.Analyzers.SampleApp`: app de console em `net10.0` usado como validacao manual.
- Regras atuais:
  - 33 IDs declarados em `src/Swa.Analyzers.Core/RuleIdentifiers.cs`, de `ARCH001` a `ARCH033`.
  - 33 analyzers em `src/Swa.Analyzers.Core/Rules/Arch*.cs`.
  - 33 documentos em `docs/rules/ARCH###.md`.
  - 33 pastas de exemplo em `src/Swa.Analyzers.SampleApp/Arch###`.
  - testes automatizados para todos os analyzers em `tests/Swa.Analyzers.Tests/Rules`, alem de teste de code fix para `ARCH001`.
- Release tracking atual:
  - `ARCH001` a `ARCH032` estao em `src/Swa.Analyzers.Core/AnalyzerReleases.Shipped.md` como baseline publicado da versao `1.0.0`.
  - `ARCH033` esta em `src/Swa.Analyzers.Core/AnalyzerReleases.Unshipped.md`.
- Empacotamento atual:
  - `src/Swa.Analyzers.Core/Swa.Analyzers.Core.csproj` empacota o analyzer e a DLL de code fixes em `analyzers/dotnet/cs`.
  - `IncludeBuildOutput=false` e `SuppressDependenciesWhenPacking=true` estao ativos.
  - As versoes de pacotes sao centralizadas em `Directory.Packages.props`.
- Release e CI atuais:
  - `GitVersion.yml` e a fonte da versao publicada.
  - Workflows usam `dotnet restore ./Swa.Analyzers.slnx --locked-mode`, build Release e testes com `-m:1` quando necessario.
  - `scripts/Validate-Release.ps1` valida consistencia entre `ARCH###`, docs, testes, SampleApp e metadados shipped/unshipped.

## Decisao de produto e versionamento

A versao 2.0 e uma breaking change.

Motivos:

- O pacote unico `Swa.Analyzers` deixa de ser a unidade de distribuicao ativa.
- IDs `ARCH###` serao substituidos pelos prefixos `REL###`, `ARC###` e `TST###`.
- Parte das regras publicadas em 1.x sera removida da implementacao ativa.
- Algumas regras mantidas mudarao o estado padrao para opt-in.
- O projeto de code fixes sera removido quando nao houver mais code fixes ativos.

Os tres pacotes NuGet independentes da versao 2.0 serao:

| Pacote | Escopo |
| ------ | ------ |
| `Swa.Analyzers.Reliability` | Regras de confiabilidade e performance operacional mantidas para fluxo ASP.NET e EF Core. |
| `Swa.Analyzers.Architecture` | Regras arquiteturais, de autorizacao, rotas, camadas e consistencia de projeto mantidas. |
| `Swa.Analyzers.Testing` | Regras de qualidade de testes mantidas. |

Nao criar metapacote `Swa.Analyzers` nesta etapa. A ausencia de metapacote tambem deve permanecer na implementacao inicial da v2, salvo decisao posterior registrada neste documento.

Os tres pacotes devem usar a mesma versao calculada pelo GitVersion. Nao deve haver `VersionPrefix` manual nem versao divergente entre pacotes.

## Mapeamento das regras mantidas

As regras abaixo permanecem na implementacao ativa da versao 2.0, com novo ID, pacote e estado padrao:

| ID atual | Novo ID | Pacote | Estado padrao v2 |
| -------- | ------- | ------ | ---------------- |
| `ARCH016` | `REL001` | `Swa.Analyzers.Reliability` | habilitada, warning |
| `ARCH017` | `REL002` | `Swa.Analyzers.Reliability` | habilitada, warning |
| `ARCH021` | `REL003` | `Swa.Analyzers.Reliability` | opt-in, info |
| `ARCH022` | `REL004` | `Swa.Analyzers.Reliability` | habilitada, warning |
| `ARCH020` | `ARC001` | `Swa.Analyzers.Architecture` | habilitada, warning |
| `ARCH027` | `ARC002` | `Swa.Analyzers.Architecture` | habilitada, warning |
| `ARCH015` | `ARC003` | `Swa.Analyzers.Architecture` | opt-in, info |
| `ARCH029` | `ARC004` | `Swa.Analyzers.Architecture` | opt-in, info |
| `ARCH032` | `ARC005` | `Swa.Analyzers.Architecture` | opt-in, info |
| `ARCH005` | `TST001` | `Swa.Analyzers.Testing` | opt-in, info |
| `ARCH006` | `TST002` | `Swa.Analyzers.Testing` | opt-in, info |

Neste plano, "habilitada, warning" significa diagnostico ativo por padrao com severidade `Warning`. "Opt-in, info" significa regra desabilitada por padrao para consumidores e severidade base `Info` quando habilitada explicitamente via `.editorconfig`.

Nenhuma regra nova deve ser criada durante esta refatoracao.

## Regras removidas da implementacao ativa

As regras abaixo nao devem permanecer como analyzers ativos na versao 2.0:

| ID atual | Destino na v2 |
| -------- | ------------- |
| `ARCH001` | remover da implementacao ativa |
| `ARCH002` | remover da implementacao ativa |
| `ARCH003` | remover da implementacao ativa |
| `ARCH004` | remover da implementacao ativa |
| `ARCH007` | remover da implementacao ativa |
| `ARCH008` | remover da implementacao ativa |
| `ARCH009` | remover da implementacao ativa |
| `ARCH010` | remover da implementacao ativa |
| `ARCH011` | remover da implementacao ativa |
| `ARCH012` | remover da implementacao ativa |
| `ARCH013` | remover da implementacao ativa |
| `ARCH014` | remover da implementacao ativa |
| `ARCH018` | remover da implementacao ativa |
| `ARCH019` | remover da implementacao ativa |
| `ARCH023` | remover da implementacao ativa |
| `ARCH024` | remover da implementacao ativa |
| `ARCH025` | remover da implementacao ativa |
| `ARCH026` | remover da implementacao ativa |
| `ARCH028` | remover da implementacao ativa |
| `ARCH030` | remover da implementacao ativa |
| `ARCH031` | remover da implementacao ativa |
| `ARCH033` | remover da implementacao ativa |

Remover da implementacao ativa nao significa apagar o historico. Os documentos e metadados historicos da versao 1.x devem continuar permitindo que um consumidor entenda quais IDs existiram e como migrar.

## Classificacao completa dos IDs atuais

Todos os 33 IDs atuais foram classificados:

| ID atual | Classificacao v2 | Novo ID |
| -------- | ---------------- | ------- |
| `ARCH001` | removida | n/a |
| `ARCH002` | removida | n/a |
| `ARCH003` | removida | n/a |
| `ARCH004` | removida | n/a |
| `ARCH005` | mantida | `TST001` |
| `ARCH006` | mantida | `TST002` |
| `ARCH007` | removida | n/a |
| `ARCH008` | removida | n/a |
| `ARCH009` | removida | n/a |
| `ARCH010` | removida | n/a |
| `ARCH011` | removida | n/a |
| `ARCH012` | removida | n/a |
| `ARCH013` | removida | n/a |
| `ARCH014` | removida | n/a |
| `ARCH015` | mantida | `ARC003` |
| `ARCH016` | mantida | `REL001` |
| `ARCH017` | mantida | `REL002` |
| `ARCH018` | removida | n/a |
| `ARCH019` | removida | n/a |
| `ARCH020` | mantida | `ARC001` |
| `ARCH021` | mantida | `REL003` |
| `ARCH022` | mantida | `REL004` |
| `ARCH023` | removida | n/a |
| `ARCH024` | removida | n/a |
| `ARCH025` | removida | n/a |
| `ARCH026` | removida | n/a |
| `ARCH027` | mantida | `ARC002` |
| `ARCH028` | removida | n/a |
| `ARCH029` | mantida | `ARC004` |
| `ARCH030` | removida | n/a |
| `ARCH031` | removida | n/a |
| `ARCH032` | mantida | `ARC005` |
| `ARCH033` | removida | n/a |

Resumo da classificacao:

- 11 regras mantidas com novo ID.
- 22 regras removidas da implementacao ativa.
- 33 IDs atuais classificados.

## Opcoes publicas de .editorconfig

Opcoes publicas das regras mantidas devem ser preservadas semanticamente. A renumeracao deve alterar apenas o prefixo do diagnostico, mantendo nomes, tipos, defaults, fallback e tratamento de valores invalidos.

| ID atual | Novo ID | Opcoes a preservar com novo prefixo |
| -------- | ------- | ----------------------------------- |
| `ARCH015` | `ARC003` | `dotnet_diagnostic.ARC003.route_language`, `dotnet_diagnostic.ARC003.additional_verbs` |
| `ARCH020` | `ARC001` | `dotnet_diagnostic.ARC001.allowed_routes`, `dotnet_diagnostic.ARC001.allowed_methods`, `dotnet_diagnostic.ARC001.ignored_namespaces` |
| `ARCH027` | `ARC002` | `dotnet_diagnostic.ARC002.core_namespace_patterns`, `dotnet_diagnostic.ARC002.forbidden_namespace_patterns`, `dotnet_diagnostic.ARC002.allowed_namespace_patterns`, `dotnet_diagnostic.ARC002.ignore_tests` |
| `ARCH029` | `ARC004` | `dotnet_diagnostic.ARC004.entity_namespaces`, `dotnet_diagnostic.ARC004.entity_base_types`, `dotnet_diagnostic.ARC004.allow_internal_setters` |
| `ARCH032` | `ARC005` | `dotnet_diagnostic.ARC005.ignored_properties`, `dotnet_diagnostic.ARC005.compare_values` |

As regras mantidas `ARCH016`, `ARCH017`, `ARCH021`, `ARCH022`, `ARCH005` e `ARCH006` nao possuem opcoes publicas especificas identificadas no estado atual; apenas a chave padrao de severidade deve mudar para o novo ID quando necessario.

## Code fixes

Nenhuma regra da versao 2.0 tera code fix inicialmente.

Consequencias:

- O code fix atual de `ARCH001` nao deve ser migrado, porque `ARCH001` sera removida da implementacao ativa.
- O projeto `src/Swa.Analyzers.CodeFixes` deve ser removido quando ficar sem uso.
- Os pacotes v2 nao devem empacotar DLL de code fix.
- Testes de code fix associados a regras removidas devem sair da suite ativa quando a remocao da regra for implementada.

## Compartilhamento de codigo

Codigo compartilhado entre os pacotes deve ser compartilhado como codigo-fonte, evitando que os pacotes de analyzer dependam de uma DLL auxiliar que precise ser distribuida separadamente.

Diretriz de implementacao:

- Preferir uma pasta de fonte compartilhada, por exemplo `src/Swa.Analyzers.Shared`, contendo helpers comuns de analyzers.
- Incluir esses arquivos nos projetos dos pacotes por `Compile Include` ou mecanismo equivalente de compartilhamento de fonte.
- Nao criar pacote ou assembly auxiliar obrigatorio para consumo dos analyzers.
- Manter helpers compartilhados pequenos, sem acoplar regras de dominios diferentes quando o compartilhamento nao reduzir complexidade real.

## Estrutura de diretorios pretendida

Estrutura alvo sugerida para a v2:

```text
src/
  Swa.Analyzers.Shared/
    Common/
  Swa.Analyzers.Reliability/
    Rules/
    AnalyzerReleases.Shipped.md
    AnalyzerReleases.Unshipped.md
  Swa.Analyzers.Architecture/
    Rules/
    AnalyzerReleases.Shipped.md
    AnalyzerReleases.Unshipped.md
  Swa.Analyzers.Testing/
    Rules/
    AnalyzerReleases.Shipped.md
    AnalyzerReleases.Unshipped.md
  Swa.Analyzers.Reliability.SampleApp/
  Swa.Analyzers.Architecture.SampleApp/
  Swa.Analyzers.Testing.SampleApp/
tests/
  Swa.Analyzers.Reliability.Tests/
  Swa.Analyzers.Architecture.Tests/
  Swa.Analyzers.Testing.Tests/
docs/
  rules/
    v1/
    REL001.md
    REL002.md
    REL003.md
    REL004.md
    ARC001.md
    ARC002.md
    ARC003.md
    ARC004.md
    ARC005.md
    TST001.md
    TST002.md
  refactoring-v2-plan.md
```

Observacoes:

- A pasta `docs/rules/v1/` e uma sugestao para preservar documentacao historica da versao 1.x, nao uma exigencia de implementacao literal se outro mecanismo equivalente for adotado.
- Cada pacote deve ter seu proprio projeto de testes.
- Cada pacote deve ter um projeto de exemplo ou mecanismo equivalente que permita validar que ele contem apenas suas proprias regras.
- A solucao principal deve continuar sendo a entrada de validacao local e de CI.

## Documentacao da v2

Os documentos da versao 2.0 devem ser escritos em portugues, preservando o padrao predominante atual do repositorio.

Documentos esperados:

- README atualizado para os tres pacotes, sem metapacote.
- Paginas de regra para `REL###`, `ARC###` e `TST###`.
- Documento historico ou secao de migracao que preserve o mapeamento dos IDs `ARCH###` antigos.
- Documentacao de release atualizada para explicar GitVersion com tres pacotes e para ajustar validacoes antes acopladas a `ARCH###`.

Nao documentar comportamento que nao foi implementado.

## Estrategia para preservar historico dos IDs antigos

Os metadados historicos da versao 1.x devem ser preservados como documentacao historica, mas nao devem impedir a remocao das implementacoes antigas.

Estrategia:

- Preservar o conteudo historico de `AnalyzerReleases.Shipped.md` da v1, especialmente `ARCH001` a `ARCH032`.
- Preservar o registro de `ARCH033` como regra nao publicada ou historica conforme o estado real no momento da migracao.
- Criar uma tabela publica de migracao `ARCH### -> REL###/ARC###/TST###` para regras mantidas.
- Registrar explicitamente os `ARCH###` sem sucessor.
- Atualizar help links dos novos IDs para documentos novos.
- Evitar reutilizar IDs `ARCH###` na v2.
- Nao criar aliases ativos com IDs antigos, salvo decisao posterior explicita, porque aliases podem manter diagnosticos antigos vivos e confundir validacao de pacote.

## Estrategia para evitar referencias orfas

Antes de finalizar qualquer etapa que remova, mova ou renumere regras, executar busca textual e revisar pelo menos:

- `README.md`.
- `CHANGELOG.md`.
- `docs/release.md`.
- `docs/adoption.md`.
- `docs/editorconfig-profiles.md`.
- `docs/rules`.
- `src/**/RuleIdentifiers.cs`.
- `src/**/RuleHelpLinks.cs`.
- `src/**/AnalyzerReleases.*.md`.
- `src/**/*.csproj`.
- `src/**/.editorconfig`.
- `src/**/Rules`.
- `src/**/SampleApp`.
- `tests/**/*.cs`.
- `scripts/Validate-Release.ps1`.
- `.github/workflows/*.yml`.

Buscas recomendadas:

```powershell
rg "ARCH\d{3}|REL\d{3}|ARC\d{3}|TST\d{3}"
rg "Swa\.Analyzers(\.Core|\.CodeFixes)?|Swa\.Analyzers\.Reliability|Swa\.Analyzers\.Architecture|Swa\.Analyzers\.Testing"
rg "dotnet_diagnostic\.(ARCH|REL|ARC|TST)\d{3}"
```

Qualquer referencia a IDs removidos deve estar em contexto historico, migracao ou changelog. Qualquer referencia a `Swa.Analyzers.Core` como pacote publico deve ser revisada na v2.

## Politica para entrada de novas regras no futuro

Durante esta refatoracao, nenhuma nova regra deve ser criada.

Apos a v2:

- Novas regras devem entrar no pacote correspondente ao dominio real: `REL###`, `ARC###` ou `TST###`.
- IDs devem ser sequenciais dentro do prefixo do pacote.
- A proposta de nova regra deve justificar:
  - problema concreto;
  - publico-alvo;
  - falso positivo esperado;
  - configuracoes publicas, se houver;
  - severidade padrao;
  - motivo para pertencer ao pacote escolhido.
- Uma nova implementacao nao deve duplicar analyzers consolidados sem diferenciacao contextual comprovada.
- Se houver analyzer consolidado no .NET SDK, Roslyn, ASP.NET Core, EF Core ou pacote externo amplamente adotado, a regra so deve existir se adicionar contexto arquitetural especifico e documentado.
- Toda nova regra deve incluir analyzer, testes, documentacao, exemplo ou mecanismo equivalente, metadados de release e README do pacote.
- Nova regra compativel deve ser `MINOR`; mudanca incompativel deve ser `MAJOR`.

## Riscos da migracao

Principais riscos:

- Consumidores perderem diagnosticos que existiam no pacote unico sem perceber que precisam instalar pacotes separados.
- Pipelines com `TreatWarningsAsErrors` mudarem de comportamento por causa de IDs e severidades novas.
- Opcoes `.editorconfig` antigas ficarem silenciosamente sem efeito apos a renumeracao.
- Help links ou README apontarem para documentos removidos ou IDs antigos fora de contexto historico.
- `scripts/Validate-Release.ps1` e workflows continuarem assumindo `ARCH###` e projeto unico.
- Pacotes v2 empacotarem regras de outro dominio por referencia indevida ou inclusao acidental.
- Codigo compartilhado virar DLL auxiliar distribuivel por engano.
- Metadados shipped/unshipped ficarem inconsistentes ao dividir os pacotes.
- Tests ou SampleApps validarem a solucao inteira sem provar isolamento por pacote.
- O projeto de code fixes permanecer referenciado mesmo sem code fixes ativos.

## Etapas e criterios de aceite

### Etapa 1: Plano persistente

Escopo:

- Criar `docs/refactoring-v2-plan.md`.
- Nao alterar codigo produtivo, IDs, projetos, workflows, scripts ou arquivos de regra existentes.

Aceite:

- Documento registra os tres pacotes v2.
- Documento registra que nao havera metapacote `Swa.Analyzers` nesta etapa.
- Documento classifica todos os 33 IDs atuais.
- Documento registra os 11 mapeamentos novos.
- Documento registra as 22 regras removidas da implementacao ativa.
- Documento registra breaking change, code fixes, compartilhamento de codigo-fonte, testes por pacote, exemplos por pacote, GitVersion unico, historico v1, `.editorconfig`, idioma, politica de novas regras, riscos e estrategia anti-referencia-orfa.

### Etapa 2: Preparar estrutura de projetos

Escopo:

- Criar projetos dos tres pacotes.
- Preparar compartilhamento de fonte.
- Preparar projetos de testes e exemplos por pacote.
- Atualizar solucao.

Aceite:

- Cada pacote compila isoladamente.
- Nenhum pacote depende de DLL auxiliar compartilhada para analyzer.
- Nenhum pacote contem analyzer fora do seu dominio.
- Code fixes nao sao empacotados.
- GitVersion continua fonte unica de versao.

### Etapa 3: Migrar regras mantidas

Escopo:

- Mover ou recriar as 11 regras mantidas nos pacotes corretos.
- Renumerar IDs para `REL###`, `ARC###` e `TST###`.
- Preservar semanticamente as opcoes publicas das regras mantidas.

Aceite:

- `REL001` a `REL004`, `ARC001` a `ARC005`, `TST001` e `TST002` existem e possuem testes.
- Estados padrao batem com a tabela deste plano.
- Help links apontam para docs novas.
- Nao ha aliases ativos `ARCH###` na implementacao v2.
- Nao ha regra nova.

### Etapa 4: Remover implementacao ativa das regras descartadas

Escopo:

- Remover analyzers, testes ativos, exemplos ativos e referencias de pacote das 22 regras removidas.
- Remover code fix e projeto de code fixes quando sem uso.

Aceite:

- Nenhum pacote v2 emite IDs removidos.
- Referencias a IDs removidos existem apenas como historico ou guia de migracao.
- A solucao compila e os testes passam.
- Release metadata historico permanece preservado.

### Etapa 5: Atualizar documentacao, release e validacoes

Escopo:

- Atualizar README, docs de regras, docs de release, adoption/editorconfig profiles quando aplicavel.
- Atualizar release check para prefixos `REL`, `ARC` e `TST`.
- Atualizar workflows se necessario para empacotar tres pacotes.

Aceite:

- `scripts/Validate-Release.ps1` valida os tres prefixos e os tres pacotes.
- Workflows geram os tres pacotes com a mesma versao GitVersion.
- Documentacao publica nao promete metapacote.
- Buscas textuais nao encontram referencias orfas fora de contexto historico.

### Etapa 6: Validacao final da v2

Escopo:

- Executar restore, build, testes, release check e pack dos tres pacotes.
- Revisar artefatos NuGet.

Aceite:

- `dotnet restore ./Swa.Analyzers.slnx --locked-mode` passa.
- `dotnet build ./Swa.Analyzers.slnx --configuration Release --no-restore` passa.
- `dotnet test ./Swa.Analyzers.slnx --configuration Release -m:1` passa.
- Release check passa.
- Pack gera exatamente os tres pacotes v2 esperados.
- Cada pacote contem apenas sua DLL de analyzer e regras do seu dominio.

## Regras de nao escopo para esta refatoracao

- Nao criar novas regras.
- Nao criar metapacote.
- Nao manter code fixes na v2 inicial.
- Nao duplicar analyzers consolidados sem diferenciacao contextual comprovada.
- Nao introduzir dependencias novas sem justificativa explicita.
- Nao alterar a fonte de versao para fora do GitVersion.
- Nao apagar historico de IDs antigos.
