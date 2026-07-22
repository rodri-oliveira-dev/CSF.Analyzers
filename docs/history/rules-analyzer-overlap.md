# Sobreposicao entre regras Swa.Analyzers e analyzers externos

## 1. Objetivo

Este documento compara as regras customizadas `ARCH###` documentadas em `docs/rules/` com analyzers externos conhecidos. O objetivo e apoiar decisoes de manutencao, remocao, substituicao, desabilitacao ou coexistencia das regras do `Swa.Analyzers`.

A analise considera equivalencias confirmadas por documentacao local das regras `ARCH###` e por documentacao oficial ou fonte primaria dos analyzers externos. Quando um identificador externo nao foi confirmado, o documento registra explicitamente: Regra externa nÃ£o confirmada.

## 2. Escopo

A analise considera todos os arquivos `ARCH###.md` existentes em `docs/rules/` no momento desta revisao.

Os analyzers externos comparados sao:

- Roslyn/.NET analyzers, principalmente `Microsoft.CodeAnalysis.NetAnalyzers`.
- `Meziantou.Analyzer`.
- `SonarAnalyzer.CSharp`.
- `StyleCop.Analyzers`.

`CSF.Analyzers.Package` foi ignorado por decisao de escopo.

`Microsoft.SourceLink.GitLab` e `DotNet.ReproducibleBuilds` estao fora do escopo de comparacao de regras. Eles sao pacotes ligados a build, rastreabilidade e reprodutibilidade, nao substitutos diretos para regras `ARCH###`.

Fontes principais consultadas:

- Microsoft Learn, regras de qualidade de codigo: <https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/>
- Repositorio oficial do Meziantou.Analyzer: <https://github.com/meziantou/Meziantou.Analyzer>
- Regras oficiais do SonarSource para C#: <https://rules.sonarsource.com/csharp/>
- Repositorio oficial do StyleCop.Analyzers: <https://github.com/DotNetAnalyzers/StyleCopAnalyzers>

## 3. Criterio de equivalencia

| Grau       | Significado                                                                                                                  |
| ---------- | ---------------------------------------------------------------------------------------------------------------------------- |
| Nenhum     | Nao ha sobreposicao relevante conhecida.                                                                                     |
| Baixo      | Existe relacao tematica, mas a regra externa nao cobre o mesmo problema de forma pratica.                                    |
| Medio      | Existe cobertura parcial ou generica, mas a regra `ARCH###` tem contexto, heuristica ou politica propria importante.         |
| Alto       | A regra externa cobre boa parte do mesmo problema, mas ha diferencas relevantes de escopo, configuracao ou falsos positivos. |
| Muito alto | A regra externa cobre praticamente o mesmo caso e pode ser candidata real a substituicao.                                    |

## 4. Matriz resumida

| Regra | Objetivo resumido | Pacote externo relacionado | Grau | Pode substituir? | Recomendacao |
| ----- | ----------------- | -------------------------- | ---- | ---------------- | ------------ |
| ARCH005 | Restringir `NSubstitute.Arg.Any<T>()` | Regra externa nÃ£o confirmada. | Nenhum | NÃ£o | Manter |
| ARCH006 | Alertar `Excluding*` em `BeEquivalentTo` | Regra externa nÃ£o confirmada. | Nenhum | NÃ£o | Manter |
| ARCH015 | Proibir verbos em rotas HTTP | SonarAnalyzer.CSharp tem regras ASP.NET de roteamento, sem regra equivalente confirmada | Baixo | NÃ£o | Manter |
| ARCH016 | Evitar `Task.Run` em request ASP.NET | Meziantou.Analyzer `MA0042`, `MA0045` relacionados; SonarAnalyzer.CSharp `S4462` relacionado | Medio | Parcialmente | Manter por contexto ASP.NET |
| ARCH017 | Evitar fire-and-forget em request ASP.NET | Meziantou.Analyzer `MA0134` relacionado | Medio | Parcialmente | Manter por contexto ASP.NET |
| ARCH020 | Exigir autorizacao explicita em endpoints HTTP | Regra externa nÃ£o confirmada. | Nenhum | NÃ£o | Manter |
| ARCH021 | Preferir `AsNoTracking` em consultas EF de leitura | Regra externa nÃ£o confirmada. | Nenhum | NÃ£o | Manter |
| ARCH022 | Evitar materializacao prematura em consultas EF | SonarAnalyzer.CSharp e Meziantou.Analyzer tem regras LINQ genericas relacionadas | Baixo | NÃ£o | Manter |
| ARCH027 | Evitar infraestrutura no core | Regra externa nÃ£o confirmada. | Nenhum | NÃ£o | Manter como politica arquitetural |
| ARCH029 | Evitar setters publicos em entidades de dominio | Regra externa nÃ£o confirmada. | Nenhum | NÃ£o | Manter como politica de dominio |
| ARCH032 | Evitar propriedades MSBuild duplicadas | Regra externa nÃ£o confirmada. | Nenhum | NÃ£o | Manter |

## 5. Analise detalhada por regra


### Objetivo da regra

Evitar `async void` em metodos, funcoes locais e funcoes anonimas, exceto em event handlers padrao.

### Sobreposicao encontrada

`Meziantou.Analyzer` confirma `MA0155`, "Do not use async void methods", e `MA0147`, "Avoid async void method for delegate". O SonarAnalyzer.CSharp confirma `S3168`, associado a metodos `async` que devem retornar `Task` em vez de `void`.

### Grau de equivalencia

Alto.

### Pode ser substituida?

Apenas apos testes comparativos.

### O que e igual

Os analyzers externos tambem desencorajam `async void` por causa de composicao, observabilidade de excecoes e aguardabilidade.

### O que e diferente


### Recomendacao

Manter por enquanto. Avaliar substituicao apenas se `MA0155`, `MA0147` e `S3168` cobrirem os mesmos positivos e negativos dos testes locais.


### Objetivo da regra

Evitar `Task.ContinueWith(...)` e incentivar `await`.

### Sobreposicao encontrada

Foi encontrada sobreposicao tematica com regras assicronas gerais, como Roslyn/.NET `CA1849` e Meziantou.Analyzer `MA0152`, mas nenhuma regra confirmada que proiba especificamente `Task.ContinueWith`.

### Grau de equivalencia

Baixo.

### Pode ser substituida?

Nao.

### O que e igual

As regras externas tambem favorecem uso idiomatico de `async` e `await` em alguns cenarios.

### O que e diferente


### Recomendacao

Manter.


### Objetivo da regra

Detectar `NotBeNull()` do FluentAssertions em testes e incentivar assercoes mais especificas.

### Sobreposicao encontrada

Regra externa nÃ£o confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada.

### O que e diferente


### Recomendacao

Manter.


### Objetivo da regra

Exigir a convencao `_sut` para o campo principal de system under test em tipos de teste unitario.

### Sobreposicao encontrada

StyleCop.Analyzers possui regras de nomenclatura e estilo, mas nao foi confirmada regra equivalente para inferir o SUT a partir do nome do tipo de teste.

### Grau de equivalencia

Baixo.

### Pode ser substituida?

Nao.

### O que e igual

Existe relacao tematica com convencoes de nomenclatura.

### O que e diferente


### Recomendacao

Manter como convencao local.

## ARCH005 - Restrinja o uso de Arg.Any()

### Objetivo da regra

Restringir `NSubstitute.Arg.Any<T>()` em testes, permitindo apenas convencoes negativas explicitamente aceitas.

### Sobreposicao encontrada

Regra externa nÃ£o confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada.

### O que e diferente

`ARCH005` conhece uma politica local de uso do NSubstitute e permite `Arg.Any<T>()` apenas em cadeias negativas com `DidNotReceive()` ou `DidNotReceiveWithAnyArgs()`.

### Recomendacao

Manter.

## ARCH006 - Alerte sobre exclusoes em BeEquivalentTo()

### Objetivo da regra

Alertar sobre exclusoes `Excluding*` dentro de opcoes de `BeEquivalentTo(...)` do FluentAssertions em testes.

### Sobreposicao encontrada

Regra externa nÃ£o confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada.

### O que e diferente

`ARCH006` e especifica para FluentAssertions, analisa o delegate de opcoes de `BeEquivalentTo` e foca no risco de assercoes permissivas.

### Recomendacao

Manter.


### Objetivo da regra

Detectar concatenacao repetida de `string` dentro de loops e sugerir `StringBuilder` ou estrategia similar.

### Sobreposicao encontrada

SonarAnalyzer.CSharp confirma `S1643`, associado a concatenacao de strings em loop. Meziantou.Analyzer confirma `MA0028`, "Optimize StringBuilder usage", mas essa regra e relacionada a otimizacao de `StringBuilder`, nao necessariamente a proibicao direta de concatenacao em loops.

### Grau de equivalencia

Alto.

### Pode ser substituida?

Revisar com testes comparativos.

### O que e igual

`S1643` cobre o mesmo tema central: concatenacao repetida de strings em loops.

### O que e diferente


### Recomendacao

Revisar com testes comparativos contra `S1643`.


### Objetivo da regra

Detectar composicao manual de paths via concatenacao ou interpolacao quando o valor e passado diretamente para APIs de filesystem.

### Sobreposicao encontrada

SonarAnalyzer.CSharp possui regras de seguranca relacionadas a acesso a arquivos e path traversal, mas uma regra externa equivalente a exigir `Path.Combine` ou `Path.Join` nesse fluxo nao foi confirmada.

### Grau de equivalencia

Baixo.

### Pode ser substituida?

Nao.

### O que e igual

A relacao e tematica: seguranca e confiabilidade ao lidar com paths.

### O que e diferente


### Recomendacao

Manter.


### Objetivo da regra

Evitar `.Result`, `.Wait()` e `.GetAwaiter().GetResult()` em `Task`, `Task<T>`, `ValueTask` e `ValueTask<T>`, exceto quando a regra especifica de construtores assume o diagnostico.

### Sobreposicao encontrada

Meziantou.Analyzer confirma `MA0042`, "Do not use blocking calls when the calling method is async", e `MA0045`, "Do not use blocking calls, even when the calling method must become async". SonarAnalyzer.CSharp confirma `S4462`, "Calls to async methods should not be blocking". Roslyn/.NET `CA1849` cobre chamada sincrona a metodo quando ha alternativa async em metodo async.

### Grau de equivalencia

Alto.

### Pode ser substituida?

Parcialmente.

### O que e igual

Todos buscam reduzir bloqueio sincrono em fluxos assincronos.

### O que e diferente


### Recomendacao

Manter ou substituir parcialmente apenas apos testes comparativos.


### Objetivo da regra

Detectar chamadas a metodos assincronos que poderiam receber um `CancellationToken` quando um token ja esta disponivel no escopo.

### Sobreposicao encontrada

Roslyn/.NET confirma `CA2016`, "Forward the CancellationToken parameter to methods that take one". Meziantou.Analyzer confirma `MA0040`, com objetivo equivalente, e `MA0032`, que sugere overload com `CancellationToken` mesmo quando nao ha token no escopo.

### Grau de equivalencia

Muito alto.

### Pode ser substituida?

Revisar com testes comparativos.

### O que e igual

`CA2016` e `MA0040` cobrem a propagacao de token para chamadas internas quando o metodo atual ja recebe `CancellationToken`.

### O que e diferente


### Recomendacao

Candidata forte a substituicao ou desabilitacao se `CA2016` e `MA0040` cobrirem os mesmos testes com ruido aceitavel.


### Objetivo da regra

Evitar operacoes bloqueantes e chamadas async descartadas dentro de construtores.

### Sobreposicao encontrada

Meziantou.Analyzer `MA0045` e `MA0134` sao relacionados a bloqueios e resultado de chamadas async nao observado. SonarAnalyzer.CSharp `S4462` tambem e relacionado a bloqueio sobre async.

### Grau de equivalencia

Medio.

### Pode ser substituida?

Parcialmente.

### O que e igual

As regras externas cobrem parte dos padroes perigosos: bloqueios e async nao observado.

### O que e diferente


### Recomendacao

Manter como politica especifica de construtor.


### Objetivo da regra

Incentivar `DateTimeOffset` em vez de `DateTime` em declaracoes de tipo controladas pelo projeto.

### Sobreposicao encontrada

SonarAnalyzer.CSharp lista uma regra para "Use DateTimeOffset instead of DateTime". Meziantou.Analyzer possui regras relacionadas a conversoes entre `DateTime` e `DateTimeOffset`, como `MA0132` e `MA0133`, mas elas nao parecem equivalentes ao escopo completo de declaracoes controladas pelo projeto.

### Grau de equivalencia

Medio.

### Pode ser substituida?

Parcialmente.

### O que e igual

Ha consenso externo sobre reduzir ambiguidade temporal ao preferir `DateTimeOffset` em certos cenarios.

### O que e diferente


### Recomendacao

Manter por enquanto. Revisar se a regra Sonar cobrir o mesmo escopo com configuracao adequada.


### Objetivo da regra

Detectar e desencorajar frameworks de mock diferentes de NSubstitute quando a politica do projeto padroniza NSubstitute.

### Sobreposicao encontrada

Regra externa nÃ£o confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada.

### O que e diferente


### Recomendacao

Manter.


### Objetivo da regra

Incentivar uma convencao local `Is.Equivalent` no lugar de `Arg.Is` para match de valores em testes.

### Sobreposicao encontrada

Regra externa nÃ£o confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada.

### O que e diferente


### Recomendacao

Manter somente em projetos que adotem essa convencao.

## ARCH015 - Proiba verbos em rotas HTTP

### Objetivo da regra

Detectar verbos de comando em segmentos literais de rotas HTTP de MVC/Web API e Minimal APIs.

### Sobreposicao encontrada

SonarAnalyzer.CSharp possui regras ASP.NET relacionadas a atributos HTTP e templates de rota, mas uma regra equivalente para detectar verbos em segmentos de rota nao foi confirmada.

### Grau de equivalencia

Baixo.

### Pode ser substituida?

Nao.

### O que e igual

Existe relacao tematica com design de APIs HTTP.

### O que e diferente

`ARCH015` aplica uma politica REST especifica, com configuracao de idioma e verbos adicionais. Regras externas conhecidas de roteamento tendem a validar atributos, barras ou formato, nao semantica de verbo no segmento.

### Recomendacao

Manter.

## ARCH016 - Evite Task.Run em fluxo de request ASP.NET

### Objetivo da regra

Detectar `Task.Run` e `Task.Factory.StartNew` dentro de fluxos de request ASP.NET.

### Sobreposicao encontrada

Meziantou.Analyzer `MA0042` e `MA0045` e SonarAnalyzer.CSharp `S4462` sao relacionados a problemas em assincronia, mas nao foi confirmada regra externa especifica para `Task.Run` em request ASP.NET.

### Grau de equivalencia

Medio.

### Pode ser substituida?

Parcialmente.

### O que e igual

As regras externas tratam uso inadequado de assincronia e bloqueio.

### O que e diferente

`ARCH016` conhece o contexto ASP.NET de controllers, actions e handlers inline de Minimal APIs, e foca escalabilidade de request. Isso e mais especifico que regras assincronas genericas.

### Recomendacao

Manter.

## ARCH017 - Evite fire-and-forget em fluxo de request

### Objetivo da regra

Detectar descarte explicito de `Task` ou `ValueTask` em fluxos de request ASP.NET.

### Sobreposicao encontrada

Meziantou.Analyzer confirma `MA0134`, "Observe result of async calls", que e relacionado a tarefas nao observadas.

### Grau de equivalencia

Medio.

### Pode ser substituida?

Parcialmente.

### O que e igual

Ambas as abordagens reduzem tarefas nao observadas.

### O que e diferente

`ARCH017` restringe o diagnostico ao fluxo de request ASP.NET e trata o risco de excecoes perdidas, cancelamento ignorado e ciclo de vida de request. `MA0134` e mais geral.

### Recomendacao

Manter como regra contextual. Usar `MA0134` como complemento, nao substituto automatico.


### Objetivo da regra

Detectar `new HttpClient()` em codigo de aplicacao e preferir `IHttpClientFactory`, typed clients ou abstracao equivalente.

### Sobreposicao encontrada

SonarAnalyzer.CSharp lista a regra "You should pool HTTP connections with HttpClientFactory". Regra externa confirmada por titulo, mas o ID nao foi confirmado nesta revisao.

### Grau de equivalencia

Alto.

### Pode ser substituida?

Revisar com testes comparativos.

### O que e igual

Ambas tratam lifetime e pooling de conexoes HTTP por meio de `HttpClientFactory`.

### O que e diferente


### Recomendacao

Revisar comparativamente antes de decidir substituicao.


### Objetivo da regra

Detectar endpoints ASP.NET que combinam metadados conflitantes de autorizacao, como `[Authorize]` e `[AllowAnonymous]`.

### Sobreposicao encontrada

SonarAnalyzer.CSharp possui regras ASP.NET e regras de seguranca relacionadas a configuracoes permissivas, mas uma equivalencia especifica para a combinacao `Authorize` com `AllowAnonymous` nao foi confirmada.

### Grau de equivalencia

Baixo.

### Pode ser substituida?

Nao.

### O que e igual

Existe relacao tematica com seguranca de endpoints ASP.NET.

### O que e diferente


### Recomendacao

Manter.

## ARCH020 - Exija autorizacao explicita em endpoints HTTP

### Objetivo da regra

Garantir que cada endpoint HTTP declare explicitamente `Authorize`/`RequireAuthorization()` ou `AllowAnonymous`/`AllowAnonymous()`.

### Sobreposicao encontrada

Regra externa nÃ£o confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada.

### O que e diferente

`ARCH020` e uma politica de seguranca de API que exige decisao explicita por endpoint e possui allowlists configuraveis.

### Recomendacao

Manter.

## ARCH021 - Prefira AsNoTracking em consultas EF Core somente leitura

### Objetivo da regra

Sugerir `AsNoTracking()` em consultas EF Core materializadas para leitura quando ha evidencia segura de que a entidade nao sera alterada e persistida no mesmo metodo.

### Sobreposicao encontrada

Regra externa nÃ£o confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada.

### O que e diferente

`ARCH021` e especifica para EF Core, materializacao e heuristica de leitura segura.

### Recomendacao

Manter.

## ARCH022 - Evite materializacao prematura em consultas

### Objetivo da regra

Evitar materializacao em memoria antes de filtros, projecoes, paginacao ou ordenacao que poderiam compor a query enviada ao banco.

### Sobreposicao encontrada

SonarAnalyzer.CSharp e Meziantou.Analyzer possuem regras LINQ genericas, como ordenacao antes de filtro ou combinacao de metodos LINQ, mas nenhuma equivalencia confirmada para materializacao prematura de EF Core.

### Grau de equivalencia

Baixo.

### Pode ser substituida?

Nao.

### O que e igual

Existe relacao com eficiencia de consultas e composicao LINQ.

### O que e diferente

`ARCH022` foca EF Core e o custo de trazer dados para memoria antes de continuar a query. Regras LINQ genericas nao necessariamente distinguem `IQueryable`, materializadores EF e execucao no banco.

### Recomendacao

Manter.


### Objetivo da regra

Evitar acesso direto ao relogio do sistema em codigo de dominio, aplicacao e servicos, preferindo `TimeProvider` ou abstracao equivalente.

### Sobreposicao encontrada

SonarAnalyzer.CSharp lista uma regra "Use a testable date/time provider". Meziantou.Analyzer confirma `MA0166`, `MA0167` e `MA0188`, relacionadas a propagacao ou uso de `TimeProvider`.

### Grau de equivalencia

Medio.

### Pode ser substituida?

Parcialmente.

### O que e igual

Os analyzers externos tambem incentivam testabilidade temporal e uso de provedores de tempo.

### O que e diferente


### Recomendacao

Manter e revisar se as regras Meziantou podem complementar ou reduzir parte da cobertura.


### Objetivo da regra

Preservar logging estruturado em chamadas de `ILogger`, evitando mensagens montadas por interpolacao ou concatenacao.

### Sobreposicao encontrada

Roslyn/.NET confirma `CA2254`, "Template should be a static expression". Meziantou.Analyzer confirma `MA0183`, "The format string should use placeholders". SonarAnalyzer.CSharp possui regras de template de log, incluindo sintaxe, placeholders e ordem.

### Grau de equivalencia

Muito alto.

### Pode ser substituida?

Revisar com testes comparativos.

### O que e igual

As regras externas cobrem a ideia central de template de log estatico e preservacao de parametros estruturados.

### O que e diferente


### Recomendacao

Candidata forte a substituicao parcial. Validar `CA2254`, `MA0183` e regras Sonar contra os testes locais.


### Objetivo da regra

Garantir que `ILogger<TCategoryName>` use como categoria o proprio tipo da classe onde o logger e declarado ou injetado.

### Sobreposicao encontrada

Meziantou.Analyzer confirma `MA0180`, "ILogger type parameter should match containing type". SonarAnalyzer.CSharp confirma `S6672`, "Generic logger injection should match enclosing type".

### Grau de equivalencia

Muito alto.

### Pode ser substituida?

Revisar com testes comparativos.

### O que e igual

As regras externas cobrem o mesmo principio: a categoria generica do logger deve refletir o tipo que contem ou recebe o logger.

### O que e diferente


### Recomendacao

Candidata forte a substituicao.


### Objetivo da regra

Detectar politicas CORS que combinam origem wildcard com credenciais, especialmente `AllowAnyOrigin()` com `AllowCredentials()`.

### Sobreposicao encontrada

SonarAnalyzer.CSharp confirma regra relacionada a CORS permissivo, `S5122`, "Having a permissive Cross-Origin Resource Sharing policy is security-sensitive".

### Grau de equivalencia

Alto.

### Pode ser substituida?

Parcialmente.

### O que e igual

Ambas tratam configuracao CORS permissiva como risco de seguranca.

### O que e diferente


### Recomendacao

Manter ate validar comportamento exato de `S5122` contra os testes locais.

## ARCH027 - Evite dependencias de infraestrutura em camadas core

### Objetivo da regra

Detectar dependencias diretas de frameworks ou adaptadores de infraestrutura em namespaces configurados como camadas core.

### Sobreposicao encontrada

Regra externa nÃ£o confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada entre os analyzers avaliados.

### O que e diferente

`ARCH027` e uma regra arquitetural configuravel por padroes de namespace proibidos e namespaces core.

### Recomendacao

Manter.


### Objetivo da regra

Detectar propriedades com `set` mutavel em records.

### Sobreposicao encontrada

StyleCop.Analyzers e Roslyn/.NET possuem regras de design e estilo relacionadas a imutabilidade e propriedades, mas uma regra equivalente para proibir setters mutaveis em records nao foi confirmada.

### Grau de equivalencia

Baixo.

### Pode ser substituida?

Nao.

### O que e igual

Existe relacao tematica com design de tipos e imutabilidade.

### O que e diferente


### Recomendacao

Manter.

## ARCH029 - Proiba setters publicos em entidades de dominio

### Objetivo da regra

Detectar setters publicos ou internos nao autorizados em entidades de dominio.

### Sobreposicao encontrada

Regra externa nÃ£o confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada.

### O que e diferente

`ARCH029` depende de sinais de entidade de dominio, namespaces configuraveis e tipos base configuraveis. E uma politica de modelagem de dominio.

### Recomendacao

Manter.


### Objetivo da regra

Detectar o mesmo `PackageReference` em mais de um `.csproj` recebido como `AdditionalFiles`.

### Sobreposicao encontrada

Regra externa nÃ£o confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada.

### O que e diferente


### Recomendacao

Manter.


### Objetivo da regra

Recomendar `System.Threading.Lock` quando codigo moderno usa `object` como monitor de sincronizacao em `lock`.

### Sobreposicao encontrada

Meziantou.Analyzer confirma `MA0158`, "Use System.Threading.Lock". Roslyn/.NET `CA2002` e SonarAnalyzer.CSharp `S2445` sao relacionados a objetos com identidade fraca em `lock`, mas nao substituem diretamente a preferencia por `System.Threading.Lock`.

### Grau de equivalencia

Alto.

### Pode ser substituida?

Revisar com testes comparativos.

### O que e igual

`MA0158` cobre a mesma recomendacao moderna. `CA2002` e `S2445` tambem tratam riscos de locking, mas com foco diferente.

### O que e diferente


### Recomendacao

Revisar comparativamente com `MA0158`. `CA2002` e `S2445` podem coexistir, mas nao substituem sozinhos.

## ARCH032 - Evite propriedades MSBuild duplicadas

### Objetivo da regra

Detectar propriedades MSBuild repetidas em `.csproj` quando a mesma propriedade ja existe no `Directory.Build.props` ancestral mais proximo recebido como `AdditionalFiles`.

### Sobreposicao encontrada

Regra externa nÃ£o confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada.

### O que e diferente

`ARCH032` analisa arquivos MSBuild via `AdditionalFiles`, compara propriedades e valores, e possui lista configuravel de propriedades ignoradas.

### Recomendacao

Manter.


### Objetivo da regra

Detectar chamadas a `BuildServiceProvider()` feitas sobre `IServiceCollection` durante configuracao de dependency injection.

### Sobreposicao encontrada

Regra externa nÃ£o confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada nos pacotes avaliados.

### O que e diferente


### Recomendacao

Manter.

## 6. Recomendacoes finais

Boas candidatas a substituicao:


Regras com sobreposicao parcial:


Regras que devem permanecer customizadas:


Regras que exigem teste comparativo antes de decisao:


## 7. Limitacoes da analise

Esta analise e baseada em documentacao local, documentacao oficial e fontes primarias dos analyzers externos. Ela nao executou os analyzers externos contra os mesmos casos positivos e negativos usados nos testes do `Swa.Analyzers`.

A decisao final de substituicao deve ser validada rodando os analyzers externos contra os mesmos exemplos e testes do `Swa.Analyzers`, comparando:

- diagnosticos esperados;
- falsos positivos;
- falsos negativos;
- configuracao por `.editorconfig`;
- severidade recomendada;
- disponibilidade e seguranca de code fixes.

Sem essa validacao, as recomendacoes devem ser tratadas como triagem tecnica, nao como decisao final de remocao.
