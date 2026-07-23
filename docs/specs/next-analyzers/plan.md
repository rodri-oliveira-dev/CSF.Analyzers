# Next analyzers SDD plan

## Objetivo

Definir a especificacao de quatro evolucoes futuras dos pacotes v2 de `Swa.Analyzers`, sem implementar analyzers nesta etapa. Estes documentos sao a fonte de verdade para os proximos prompts.

## Regras incluidas

| ID | Pacote | Categoria | Severidade planejada | Estado padrao | Documento |
| -- | ------ | --------- | -------------------- | ------------- | --------- |
| `REL005` | `Swa.Analyzers.Reliability` | Reliability | Warning | Habilitada | [REL005.md](REL005.md) |
| `REL006` | `Swa.Analyzers.Reliability` | Reliability | Warning | Habilitada para tipos conhecidos | [REL006.md](REL006.md) |
| `ARC006` | `Swa.Analyzers.Architecture` | Architecture | Info | Opt-in | [ARC006.md](ARC006.md) |
| `TST001` | `Swa.Analyzers.Testing` | TestQuality | Info | Opt-in | [TST001-anyargs.md](TST001-anyargs.md) |

`TST001` nao cria novo diagnostico. A evolucao amplia a regra existente [TST001](../../rules/testing/TST001.md).

## Regras explicitamente adiadas

As ideias abaixo ficam registradas para triagem futura, sem implementacao ou compromisso de escopo nesta evolucao:

| ID | Ideia | Motivo do adiamento |
| -- | ----- | ------------------- |
| `ARC007` | Fronteiras entre modulos | Exige modelo de modulos e configuracao organizacional mais ampla que o escopo atual. |
| `REL007` | Provavel N+1 em loops | Heuristica tende a exigir analise interprocedural e conhecimento de navegacoes EF para baixo ruido. |
| `TST003` | Intencao explicita de ordering em `BeEquivalentTo` | Precisa validar sobreposicao com FluentAssertions e convencoes de teste antes de propor diagnostico. |
| `ARC008` | Service Locator | Relacionada a DI e arquitetura, mas nao deve ser misturada com `REL006`, que foca captura de scoped conhecidos em hosted services. |
| `REL008` | `SaveChanges` dentro de loops | Pode ter sobreposicao com regras de performance e exige excecoes para lotes transacionais pequenos/intencionais. |

## Dependencias entre implementacoes

- `ARC006` deve reutilizar a identificacao de entidades de `ARC004` sempre que tecnicamente adequado. Antes de implementar, extrair ou compartilhar a logica de entidade para evitar duas definicoes divergentes.
- `REL005` pode reutilizar parte do reconhecimento EF Core ja existente em `REL003`/`REL004`, mas precisa de nova logica para raiz de `DbContext` e concorrencia.
- `REL006` pode reutilizar o leitor de opcoes `AnalyzerConfigOptionReader`, mas a opcao planejada usa lista separada por `;`; a implementacao deve decidir se cria helper novo ou padroniza a opcao como array JSON antes de escrever docs publicas finais.
- `TST001` deve preservar o comportamento atual e apenas adicionar metodos NSubstitute semanticamente resolvidos.

## Ordem sugerida dos proximos prompts

1. Implementar `REL005`, incluindo analyzer, testes, docs de regra, samples, `RuleIdentifiers`, `AnalyzerReleases.Unshipped.md`, README e performance basica.
2. Implementar `REL006`, incluindo opcao `dotnet_diagnostic.REL006.scoped_type_patterns` e testes de configuracao.
3. Refatorar identificacao de entidade de `ARC004` para helper compartilhado do pacote Architecture e implementar `ARC006`.
4. Expandir `TST001` para APIs `*AnyArgs`, mantendo mesmo ID e sem novo arquivo de regra.
5. Executar validacoes completas e revisar sobreposicao externa novamente antes de release.

## Riscos globais

- Falsos positivos por heuristicas amplas em concorrencia EF Core. `REL005` deve ficar restrita a padroes locais claros na primeira versao.
- Duplicacao de conceito de entidade entre `ARC004` e `ARC006`. A aceitacao de `ARC006` depende de reaproveitamento ou extracao controlada.
- `REL006` pode ser confundida com uma regra generica de lifetime do container. A regra deve ficar limitada a hosted services e tipos scoped conhecidos/configurados.
- APIs ASP.NET Core de Minimal APIs e typed results evoluem com o framework. A implementacao deve reconhecer simbolos, nao apenas nomes textuais.
- `TST001` deve distinguir APIs oficiais do NSubstitute de metodos customizados homonimos.

## Criterios globais de aceite

- `REL005`, `REL006` e `ARC006` aparecem em `RuleIdentifiers` somente nos prompts de implementacao, nao nesta etapa.
- Cada implementacao futura atualiza analyzer, testes, docs de regra, sample, README e `AnalyzerReleases.Unshipped.md` quando aplicavel.
- Regras opt-in continuam opt-in nos testes e docs.
- Toda opcao publica documentada deve ser implementada e coberta por testes de valor ausente, valido e invalido quando aplicavel.
- Nenhuma regra deve diagnosticar apenas por nome textual quando a resolucao semantica for viavel.
- Os casos negativos documentados nas specs devem virar testes antes de finalizar cada regra.

## Fontes oficiais consultadas

- EF Core, `DbContext` lifetime e threading: <https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/>
- EF Core `DbContext` API: <https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext>
- .NET hosted services com scoped services: <https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service>
- .NET options pattern: <https://learn.microsoft.com/en-us/dotnet/core/extensions/options>
- ASP.NET Core action return types: <https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types>
- ASP.NET Core Minimal API responses: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses>
- NSubstitute argument matchers: <https://nsubstitute.github.io/help/argument-matchers/>
- NSubstitute return for any args: <https://nsubstitute.github.io/help/return-for-any-args/>
- NSubstitute received calls: <https://nsubstitute.github.io/help/received-calls/>
- NSubstitute callbacks: <https://nsubstitute.github.io/help/callbacks/>
- NSubstitute return for all calls of a type: <https://nsubstitute.github.io/help/return-for-all/>
- NSubstitute analyzers: <https://nsubstitute.github.io/help/nsubstitute-analysers/>

## Analise de sobreposicao

A revisao partiu de [rules-analyzer-overlap.md](../../reviews/rules-analyzer-overlap.md) e de buscas em Roslyn/.NET analyzers, ASP.NET Core analyzers, Meziantou.Analyzer, SonarAnalyzer.CSharp e NSubstitute.Analyzers.

- `REL005`: existe orientacao oficial do EF Core, mas nao foi identificada regra consolidada equivalente com rastreamento contextual de duas operacoes EF concorrentes no mesmo simbolo raiz de `DbContext`. A diferenciacao e detectar padroes locais de concorrencia em codigo de aplicacao EF Core.
- `REL006`: o container pode falhar em runtime para algumas combinacoes de lifetimes, mas a regra proposta e mais contextual: hosted services e tipos scoped conhecidos/configurados. Nao substitui validacao completa de DI.
- `ARC006`: analyzers externos podem validar binding, retornos ou serializacao ASP.NET, mas a diferenciacao e aplicar politica DDD local reutilizando identificacao de entidade de `ARC004`.
- `TST001`: NSubstitute.Analyzers foca usos incorretos do framework, como substituicao de membros nao virtuais. A evolucao e uma convencao de qualidade de teste: evitar APIs que ignoram argumentos em asserts/setups positivos. `DidNotReceiveWithAnyArgs` permanece permitido por intencao negativa explicita.

## Decisoes desta etapa

- Manter `REL005` como `Warning` habilitada por padrao, com escopo restrito a padroes concorrentes locais.
- Manter `REL006` como `Warning` habilitada por padrao apenas para `DbContext` e `IOptionsSnapshot<T>` mais tipos configurados.
- Manter `ARC006` como `Info` opt-in por depender de convencao de arquitetura/DDD.
- Expandir `TST001` em vez de criar `TST003`, porque o problema conceitual e o mesmo: matcher/atalho excessivamente permissivo em NSubstitute.
- Nao incluir `ReturnsForAll<T>` em `TST001` nesta evolucao; a API define retorno por tipo, nao por ignorar argumentos de uma chamada especifica.
