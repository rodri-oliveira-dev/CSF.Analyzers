# Sobreposicao com analyzers externos

## Objetivo

Este documento revisa as regras mantidas como produto ativo na v2: `REL001`-`REL006`, `ARC001`-`ARC006`, `TST001` e `TST002`.

Exclusividade nao basta para justificar uma regra. Uma regra deve permanecer quando entrega diferenciacao contextual, baixo ruido esperado e valor operacional claro para consumidores.

## Criterios de entrada e permanencia

- O problema e concreto e recorrente em projetos .NET.
- A regra consegue diferenciar contexto, nao apenas procurar padroes amplos.
- O diagnostico tem baixo ruido ou opcoes publicas suficientes para calibragem.
- A correcao esperada e operacionalmente util.
- A regra nao duplica de forma inferior um analyzer consolidado.
- Quando ha ferramenta externa relacionada, a documentacao explica coexistencia, substituicao ou escopo diferente.

## Matriz

| Regra | Ferramenta relacionada | Equivalencia | Diferenciacao | Motivo para manter | Decisao |
| ----- | ---------------------- | ------------ | ------------- | ------------------ | ------- |
| `REL001` | Meziantou.Analyzer, SonarAnalyzer.CSharp | Parcial | Regras genericas de async/performance nao restringem o diagnostico ao fluxo de request ASP.NET. | Evita ruido em codigo fora de request, testes e hosted services. | Coexistir. |
| `REL002` | Meziantou.Analyzer, SonarAnalyzer.CSharp | Parcial | Regras de tarefas nao observadas nao capturam a politica local de fire-and-forget em request ASP.NET com `Task` e `ValueTask`. | O risco operacional depende do ciclo de vida de request. | Coexistir. |
| `REL003` | EF Core guidance, analyzers LINQ/performance | Parcial | A regra exige origem EF Core e politica explicita de leitura sem tracking. | Permanece opt-in para times que adotam leitura sem tracking por padrao. | Coexistir. |
| `REL004` | Regras LINQ/performance de analyzers genericos | Parcial | O diagnostico depende de materializacao EF Core antes de filtro, projecao ou paginacao. | Entrega feedback mais contextual que uma regra LINQ ampla. | Coexistir. |
| `REL005` | EF Core documentation; analyzers async genericos de Meziantou.Analyzer, Roslynator e SonarAnalyzer.CSharp | Parcial | Ferramentas relacionadas podem apontar padroes async amplos, mas nao foi identificado substituto consolidado que rastreie duas operacoes EF Core semanticamente resolvidas sobre a mesma raiz de `DbContext` em `Task.WhenAll` ou `Parallel.ForEachAsync`. | O bug e recorrente, EF Core documenta que a instancia nao suporta operacoes paralelas, e a regra limita o alerta a padroes locais de alta confianca. | Coexistir; nao substituir por analyzer generico. |
| `REL006` | Validacao de DI do ASP.NET Core em runtime; documentacao .NET de `BackgroundService`; SonarAnalyzer.CSharp/Meziantou.Analyzer como cobertura geral de code quality | Parcial | A validacao de DI pode falhar em runtime para lifetimes, mas nao e uma regra Roslyn especifica para captura de `DbContext`, `IOptionsSnapshot<T>` ou tipos configurados em hosted services. | Antecipar captive dependencies em codigo-fonte reduz falhas de startup/runtime e documenta a politica de usar escopo ou factory. | Coexistir com validacao de DI; nao substituir. |
| `ARC001` | ASP.NET Core analyzers e analyzers de seguranca | Parcial | Ferramentas externas cobrem APIs e seguranca geral, mas nao exigem decisao explicita por endpoint com allowlist local. | Politica contextual de seguranca de API. | Coexistir. |
| `ARC002` | NetArchTest, ArchUnitNET, NDepend | Parcial | Ferramentas de arquitetura podem expressar dependencias entre camadas, mas normalmente rodam como testes/ferramentas externas, nao como diagnostico Roslyn calibrado por namespace no editor/build. | Feedback imediato para dependencias proibidas em camadas core. | Coexistir; pode ser substituida por suite arquitetural quando o projeto preferir governanca fora do compiler. |
| `ARC003` | ASP.NET Core analyzers de rotas | Baixa | Validacoes de rota nao codificam a politica de evitar verbos em segmentos de APIs orientadas a recursos. | Politica opt-in de design HTTP. | Coexistir. |
| `ARC004` | Regras de design/imutabilidade; NDepend/ArchUnitNET | Parcial | Regras genericas nao identificam entidades de dominio pelos mesmos marcadores e opcoes `ARC004`. | Politica DDD local e opt-in, com configuracao de namespaces e tipos base. | Coexistir. |
| `ARC005` | MSBuild analyzers e validacoes customizadas de CI | Parcial | A regra compara projetos e `Directory.Build.props` recebidos como `AdditionalFiles`, com allowlist de propriedades. | Garante centralizacao MSBuild no build comum. | Coexistir; substituir apenas se o repo adotar uma validacao MSBuild dedicada equivalente. |
| `ARC006` | ASP.NET Core analyzers; orientacao Microsoft sobre DTOs em Web API; ferramentas de arquitetura como NDepend/ArchUnitNET | Parcial | Ferramentas ASP.NET validam binding, retornos e uso de APIs; ferramentas de arquitetura podem verificar dependencias, mas nao combinam contexto HTTP com o classificador de entidade compartilhado com `ARC004`. | Politica DDD opt-in para evitar acoplamento de contratos HTTP ao dominio com baixo ruido e sem analise profunda de DTOs. | Coexistir; nao substituir por analyzer ASP.NET generico. |
| `TST001` | NSubstitute.Analyzers; documentacao NSubstitute de argument matchers e `AnyArgs` | Parcial | NSubstitute.Analyzers foca uso incorreto do framework. `TST001` aplica convencao de precisao de teste: restringe `Arg.Any<T>()`, `ReturnsForAnyArgs`, `WhenForAnyArgs` e `ReceivedWithAnyArgs`, preservando `DidNotReceiveWithAnyArgs`. | A regra e opt-in e protege asserts/setups positivos contra matching amplo demais. | Coexistir com NSubstitute.Analyzers. |
| `TST002` | FluentAssertions analyzers e revisoes de teste | Parcial | Ferramentas relacionadas ajudam uso da biblioteca, mas nao bloqueiam a politica local de evitar `Excluding*` em `BeEquivalentTo()`. | Convencao opt-in de equivalencia estrita. | Coexistir. |

## Substitutos externos

- `REL005`: usar apenas analyzers async genericos deixaria escapar a condicao essencial: mesma instancia candidata de `DbContext` com operacoes EF Core concorrentes. A regra local deve coexistir com analyzers async e com a orientacao oficial do EF Core.
- `REL006`: a validacao de DI em runtime continua recomendada, mas ela nao substitui feedback estatico em hosted services. A regra local deve coexistir com validacao do container e com revisoes de lifetime.
- `ARC006`: analyzers ASP.NET Core e ferramentas de arquitetura sao complementares. A regra local so deve ser substituida por ferramenta externa se ela conseguir reconhecer contexto HTTP, wrappers de retorno e entidades segundo a mesma politica DDD configuravel.
- `TST001`: NSubstitute.Analyzers deve coexistir porque valida erros de uso do framework; `TST001` valida uma convencao de precisao de testes.
- Para regras sem substituto direto, a decisao de manter continua dependente de baixo ruido, documentacao precisa e testes de falso positivo.

## Fontes externas revisadas em 2026-07-23

- EF Core async e threading: <https://learn.microsoft.com/en-us/ef/core/miscellaneous/async>
- EF Core `DbContext` lifetime/threading: <https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/>
- .NET scoped services em `BackgroundService`: <https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service>
- ASP.NET Web API com DTOs: <https://learn.microsoft.com/en-us/aspnet/web-api/overview/data/using-web-api-with-entity-framework/part-5>
- ASP.NET Core action return types: <https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types>
- NSubstitute argument matchers: <https://nsubstitute.github.io/help/argument-matchers/>
- NSubstitute return for any args: <https://nsubstitute.github.io/help/return-for-any-args/>
- NSubstitute received calls: <https://nsubstitute.github.io/help/received-calls/>
- NSubstitute analyzers: <https://nsubstitute.github.io/help/nsubstitute-analysers/>
- NSubstitute.Analyzers issue sobre `AnyArgs`: <https://github.com/nsubstitute/NSubstitute.Analyzers/issues/175>
- Meziantou.Analyzer project listing: <https://www.meziantou.net/projects.htm>
- SonarAnalyzer.CSharp package: <https://www.nuget.org/packages/SonarAnalyzer.CSharp/>
- Roslynator project: <https://github.com/dotnet/roslynator>

## Apendice: regras v1 removidas

As regras v1 sem sucessor nao fazem parte da implementacao ativa da v2. A lista completa e os mapeamentos ficam em [migracao v2](../migration-v2.md).

Quando houver substitutos externos consolidados, prefira avalia-los antes de recriar uma regra local. Exemplos de temas removidos com cobertura externa ou relacionada incluem logging estruturado, categoria de `ILogger<T>`, CORS permissivo, uso de `System.Threading.Lock`, provedores de tempo e lifetime de `HttpClient`.
