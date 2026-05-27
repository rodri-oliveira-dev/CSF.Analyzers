# Sobreposicao entre regras Swa.Analyzers e analyzers externos

## 1. Objetivo

Este documento compara as regras customizadas `ARCH###` documentadas em `docs/rules/` com analyzers externos conhecidos. O objetivo e apoiar decisoes de manutencao, remocao, substituicao, desabilitacao ou coexistencia das regras do `Swa.Analyzers`.

A analise considera equivalencias confirmadas por documentacao local das regras `ARCH###` e por documentacao oficial ou fonte primaria dos analyzers externos. Quando um identificador externo nao foi confirmado, o documento registra explicitamente: Regra externa não confirmada.

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
| ARCH001 | Evitar `async void` fora de event handlers | Meziantou.Analyzer `MA0155`, `MA0147`; SonarAnalyzer.CSharp `S3168` | Alto | Revisar com testes comparativos | Manter ate comparar excecoes de event handler e code fix |
| ARCH002 | Evitar `Task.ContinueWith` | Meziantou.Analyzer `MA0152` apenas relacionado; Roslyn/.NET `CA1849` apenas tematico | Baixo | Não | Manter |
| ARCH003 | Evitar `NotBeNull()` fraco em FluentAssertions | Regra externa não confirmada. | Nenhum | Não | Manter como politica de teste |
| ARCH004 | Exigir campo `_sut` em testes unitarios | StyleCop.Analyzers apenas nomenclatura generica | Baixo | Não | Manter como convencao local |
| ARCH005 | Restringir `NSubstitute.Arg.Any<T>()` | Regra externa não confirmada. | Nenhum | Não | Manter |
| ARCH006 | Alertar `Excluding*` em `BeEquivalentTo` | Regra externa não confirmada. | Nenhum | Não | Manter |
| ARCH007 | Detectar concatenacao de string em loops | SonarAnalyzer.CSharp `S1643`; Meziantou.Analyzer `MA0028` relacionado | Alto | Revisar com testes comparativos | Revisar comparativamente |
| ARCH008 | Evitar composicao manual de paths em APIs de filesystem | SonarAnalyzer.CSharp tem regras de path traversal relacionadas; Regra externa não confirmada. | Baixo | Não | Manter |
| ARCH009 | Evitar bloqueio sincrono sobre async | Meziantou.Analyzer `MA0042`, `MA0045`; SonarAnalyzer.CSharp `S4462`; Roslyn/.NET `CA1849` parcial | Alto | Parcialmente | Manter ou revisar com testes |
| ARCH010 | Propagar `CancellationToken` | Roslyn/.NET `CA2016`; Meziantou.Analyzer `MA0040`, `MA0032` | Muito alto | Revisar com testes comparativos | Candidata forte a substituicao parcial ou total |
| ARCH011 | Evitar async ou bloqueio em construtores | Meziantou.Analyzer `MA0045`, `MA0134`; SonarAnalyzer.CSharp `S4462` relacionado | Medio | Parcialmente | Manter como regra especifica de construtor |
| ARCH012 | Preferir `DateTimeOffset` a `DateTime` | SonarAnalyzer.CSharp, regra "Use DateTimeOffset instead of DateTime"; Meziantou.Analyzer `MA0132`, `MA0133` relacionados | Medio | Parcialmente | Manter ou revisar escopo |
| ARCH013 | Padronizar framework de mock em NSubstitute | Regra externa não confirmada. | Nenhum | Não | Manter como politica local |
| ARCH014 | Preferir `Is.Equivalent` a `Arg.Is` | Regra externa não confirmada. | Nenhum | Não | Manter como politica local |
| ARCH015 | Proibir verbos em rotas HTTP | SonarAnalyzer.CSharp tem regras ASP.NET de roteamento, sem regra equivalente confirmada | Baixo | Não | Manter |
| ARCH016 | Evitar `Task.Run` em request ASP.NET | Meziantou.Analyzer `MA0042`, `MA0045` relacionados; SonarAnalyzer.CSharp `S4462` relacionado | Medio | Parcialmente | Manter por contexto ASP.NET |
| ARCH017 | Evitar fire-and-forget em request ASP.NET | Meziantou.Analyzer `MA0134` relacionado | Medio | Parcialmente | Manter por contexto ASP.NET |
| ARCH018 | Evitar `new HttpClient()` em aplicacao | SonarAnalyzer.CSharp, regra "You should pool HTTP connections with HttpClientFactory" | Alto | Revisar com testes comparativos | Revisar comparativamente |
| ARCH019 | Evitar `Authorize` com `AllowAnonymous` no mesmo endpoint | SonarAnalyzer.CSharp tem regras ASP.NET de seguranca relacionadas, sem equivalencia confirmada | Baixo | Não | Manter |
| ARCH020 | Exigir autorizacao explicita em endpoints HTTP | Regra externa não confirmada. | Nenhum | Não | Manter |
| ARCH021 | Preferir `AsNoTracking` em consultas EF de leitura | Regra externa não confirmada. | Nenhum | Não | Manter |
| ARCH022 | Evitar materializacao prematura em consultas EF | SonarAnalyzer.CSharp e Meziantou.Analyzer tem regras LINQ genericas relacionadas | Baixo | Não | Manter |
| ARCH023 | Preferir `TimeProvider` para hora atual | SonarAnalyzer.CSharp, regra "Use a testable date/time provider"; Meziantou.Analyzer `MA0166`, `MA0167`, `MA0188` relacionados | Medio | Parcialmente | Manter ou revisar escopo |
| ARCH024 | Evitar interpolacao ou concatenacao em `ILogger` | Roslyn/.NET `CA2254`; Meziantou.Analyzer `MA0183`; SonarAnalyzer.CSharp regras de template de log | Muito alto | Revisar com testes comparativos | Candidata forte a substituicao parcial |
| ARCH025 | Exigir categoria `ILogger<T>` compativel | Meziantou.Analyzer `MA0180`; SonarAnalyzer.CSharp `S6672` | Muito alto | Revisar com testes comparativos | Candidata forte a substituicao |
| ARCH026 | Evitar CORS inseguro | SonarAnalyzer.CSharp `S5122` relacionado a CORS permissivo | Alto | Parcialmente | Manter ate validar exatamente `AllowAnyOrigin` com credenciais |
| ARCH027 | Evitar infraestrutura no core | Regra externa não confirmada. | Nenhum | Não | Manter como politica arquitetural |
| ARCH028 | Evitar propriedades mutaveis em records | StyleCop.Analyzers e Roslyn/.NET tem regras de design relacionadas, sem equivalencia confirmada | Baixo | Não | Manter |
| ARCH029 | Evitar setters publicos em entidades de dominio | Regra externa não confirmada. | Nenhum | Não | Manter como politica de dominio |
| ARCH030 | Detectar `PackageReference` duplicado entre projetos | Regra externa não confirmada. | Nenhum | Não | Manter |
| ARCH031 | Preferir `System.Threading.Lock` a `object` | Meziantou.Analyzer `MA0158`; Roslyn/.NET `CA2002` e SonarAnalyzer.CSharp `S2445` relacionados | Alto | Revisar com testes comparativos | Revisar comparativamente |
| ARCH032 | Evitar propriedades MSBuild duplicadas | Regra externa não confirmada. | Nenhum | Não | Manter |
| ARCH033 | Evitar `BuildServiceProvider` em registro de servicos | Regra externa não confirmada. | Nenhum | Não | Manter |

## 5. Analise detalhada por regra

## ARCH001 - Evite async void fora de event handlers

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

`ARCH001` documenta excecao especifica para event handlers no formato `object sender, EventArgs e` e oferece code fix seguro para metodos e funcoes locais concretas. A equivalencia de todas essas excecoes e do comportamento do code fix nao foi confirmada nos analyzers externos.

### Recomendacao

Manter por enquanto. Avaliar substituicao apenas se `MA0155`, `MA0147` e `S3168` cobrirem os mesmos positivos e negativos dos testes locais.

## ARCH002 - Evite Task.ContinueWith

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

`ARCH002` mira semanticamente `System.Threading.Tasks.Task.ContinueWith` e `Task<T>.ContinueWith`, independentemente de o metodo chamador ser `async`. As regras externas confirmadas nao cobrem esse contrato especifico.

### Recomendacao

Manter.

## ARCH003 - Proiba NotBeNull() em testes

### Objetivo da regra

Detectar `NotBeNull()` do FluentAssertions em testes e incentivar assercoes mais especificas.

### Sobreposicao encontrada

Regra externa não confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada.

### O que e diferente

`ARCH003` e uma politica de qualidade de testes especifica para FluentAssertions e limitada a contextos de teste reconhecidos.

### Recomendacao

Manter.

## ARCH004 - Exija o nome _sut em testes unitarios

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

`ARCH004` infere o tipo sob teste pelo nome da classe de teste e busca um unico campo candidato. Isso e uma politica de teste, nao uma regra geral de nomenclatura.

### Recomendacao

Manter como convencao local.

## ARCH005 - Restrinja o uso de Arg.Any()

### Objetivo da regra

Restringir `NSubstitute.Arg.Any<T>()` em testes, permitindo apenas convencoes negativas explicitamente aceitas.

### Sobreposicao encontrada

Regra externa não confirmada.

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

Regra externa não confirmada.

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

## ARCH007 - Detecte concatenacao de strings em loops

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

`ARCH007` tem heuristica local documentada para `for`, `foreach`, `while` e `do/while`, e deve ser comparada contra casos positivos e negativos locais antes de substituir. `MA0028` parece complementar, mas nao substitui diretamente.

### Recomendacao

Revisar com testes comparativos contra `S1643`.

## ARCH008 - Proiba composicao manual de caminhos de arquivo

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

`ARCH008` e uma regra de estilo/confiabilidade sobre composicao manual de paths passada a APIs de filesystem. Regras de path traversal normalmente focam entrada nao confiavel, nao padronizacao de API.

### Recomendacao

Manter.

## ARCH009 - Proiba bloqueio sincrono de operacoes assincronas

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

`ARCH009` e explicita sobre `Task`, `Task<T>`, `ValueTask` e `ValueTask<T>`, e delega casos em construtor para `ARCH011` para evitar duplicidade. `CA1849` nao cobre necessariamente `.Result` e `.Wait()` de forma geral. As regras externas precisam ser testadas contra os mesmos cenarios locais.

### Recomendacao

Manter ou substituir parcialmente apenas apos testes comparativos.

## ARCH010 - Exija propagacao de CancellationToken

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

`ARCH010` pode ter heuristicas locais sobre disponibilidade de token e formato de invocacao. `MA0032` e mais amplo em alguns cenarios porque sugere overload mesmo sem token disponivel, o que pode gerar politica diferente.

### Recomendacao

Candidata forte a substituicao ou desabilitacao se `CA2016` e `MA0040` cobrirem os mesmos testes com ruido aceitavel.

## ARCH011 - Proiba logica assincrona ou bloqueante em construtores

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

`ARCH011` e especifica para construtores, combina bloqueio e fire-and-forget no mesmo contexto e evita diagnostico duplicado com `ARCH009`.

### Recomendacao

Manter como politica especifica de construtor.

## ARCH012 - Prefira DateTimeOffset em vez de DateTime

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

`ARCH012` aplica uma politica arquitetural de tipo em declaracoes controladas pelo projeto. As regras confirmadas do Meziantou focam conversoes e uso implicito, e a regra Sonar precisa ser comparada para confirmar escopo, excecoes e ruido.

### Recomendacao

Manter por enquanto. Revisar se a regra Sonar cobrir o mesmo escopo com configuracao adequada.

## ARCH013 - Restrinja frameworks de mock ao NSubstitute

### Objetivo da regra

Detectar e desencorajar frameworks de mock diferentes de NSubstitute quando a politica do projeto padroniza NSubstitute.

### Sobreposicao encontrada

Regra externa não confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada.

### O que e diferente

`ARCH013` e uma politica organizacional, nao uma regra geral de qualidade.

### Recomendacao

Manter.

## ARCH014 - Prefira Is.Equivalent em vez de NSubstitute Arg.Is

### Objetivo da regra

Incentivar uma convencao local `Is.Equivalent` no lugar de `Arg.Is` para match de valores em testes.

### Sobreposicao encontrada

Regra externa não confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada.

### O que e diferente

`ARCH014` depende de helper ou API adotada pelo consumidor, que nao faz parte de NSubstitute nem dos analyzers externos avaliados.

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

## ARCH018 - Evite instanciacao direta de HttpClient

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

`ARCH018` documenta escopo de codigo de aplicacao e excecoes locais. A regra Sonar precisa ser validada contra os exemplos locais para confirmar se cobre instanciacao direta, contextos permitidos e falsos positivos.

### Recomendacao

Revisar comparativamente antes de decidir substituicao.

## ARCH019 - Evite Authorize e AllowAnonymous no mesmo endpoint

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

`ARCH019` foca conflito semantico de metadados entre controller/action ou composicao equivalente. Essa politica nao foi confirmada em analyzer externo.

### Recomendacao

Manter.

## ARCH020 - Exija autorizacao explicita em endpoints HTTP

### Objetivo da regra

Garantir que cada endpoint HTTP declare explicitamente `Authorize`/`RequireAuthorization()` ou `AllowAnonymous`/`AllowAnonymous()`.

### Sobreposicao encontrada

Regra externa não confirmada.

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

Regra externa não confirmada.

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

## ARCH023 - Prefira TimeProvider para obter data e hora

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

`ARCH023` tem escopo por namespaces, tipos permitidos e opcao para logging simples. `MA0166` e `MA0167` focam overloads e propagacao de `TimeProvider`; `MA0188` prefere `System.TimeProvider` em vez de abstracao customizada. Isso nao substitui automaticamente a politica local.

### Recomendacao

Manter e revisar se as regras Meziantou podem complementar ou reduzir parte da cobertura.

## ARCH024 - Evite interpolacao ou concatenacao em ILogger

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

`ARCH024` mira especificamente interpolacao e concatenacao em `ILogger`, podendo ter heuristicas locais sobre overloads e tipo do logger. `CA2254` e candidato principal, mas deve ser comparado contra todos os casos locais.

### Recomendacao

Candidata forte a substituicao parcial. Validar `CA2254`, `MA0183` e regras Sonar contra os testes locais.

## ARCH025 - ILogger<T> deve usar o tipo da classe atual

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

`ARCH025` pode ter regras locais para campos, construtores e tipos aninhados que precisam ser comparadas. A equivalencia parece forte, mas ainda depende de ruido e cobertura de cenarios.

### Recomendacao

Candidata forte a substituicao.

## ARCH026 - Evite configuracao CORS insegura

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

`ARCH026` foca a combinacao ASP.NET Core `AllowAnyOrigin()` com `AllowCredentials()` e possui opcao `disallow_any_origin`. `S5122` e mais ampla como Security Hotspot e pode exigir revisao humana em cenarios que `ARCH026` trata como politica local.

### Recomendacao

Manter ate validar comportamento exato de `S5122` contra os testes locais.

## ARCH027 - Evite dependencias de infraestrutura em camadas core

### Objetivo da regra

Detectar dependencias diretas de frameworks ou adaptadores de infraestrutura em namespaces configurados como camadas core.

### Sobreposicao encontrada

Regra externa não confirmada.

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

## ARCH028 - Proiba propriedades mutaveis em records

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

`ARCH028` e especifica para `record`, `record class`, `record struct` e `readonly record struct`, com opcao para setters nao publicos.

### Recomendacao

Manter.

## ARCH029 - Proiba setters publicos em entidades de dominio

### Objetivo da regra

Detectar setters publicos ou internos nao autorizados em entidades de dominio.

### Sobreposicao encontrada

Regra externa não confirmada.

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

## ARCH030 - Detecte PackageReference duplicado entre projetos

### Objetivo da regra

Detectar o mesmo `PackageReference` em mais de um `.csproj` recebido como `AdditionalFiles`.

### Sobreposicao encontrada

Regra externa não confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada.

### O que e diferente

`ARCH030` analisa XML de projeto via `AdditionalFiles`, possui allowlist de pacotes e padroes de projeto, e trata uma politica de dependencia entre projetos.

### Recomendacao

Manter.

## ARCH031 - Prefira System.Threading.Lock em vez de lock object

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

`ARCH031` tem configuracao de target framework minimo e opcao para variaveis locais. A equivalencia de `MA0158` nesses detalhes deve ser confirmada com testes.

### Recomendacao

Revisar comparativamente com `MA0158`. `CA2002` e `S2445` podem coexistir, mas nao substituem sozinhos.

## ARCH032 - Evite propriedades MSBuild duplicadas

### Objetivo da regra

Detectar propriedades MSBuild repetidas em `.csproj` quando a mesma propriedade ja existe no `Directory.Build.props` ancestral mais proximo recebido como `AdditionalFiles`.

### Sobreposicao encontrada

Regra externa não confirmada.

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

## ARCH033 - Evite BuildServiceProvider durante registro de servicos

### Objetivo da regra

Detectar chamadas a `BuildServiceProvider()` feitas sobre `IServiceCollection` durante configuracao de dependency injection.

### Sobreposicao encontrada

Regra externa não confirmada.

### Grau de equivalencia

Nenhum.

### Pode ser substituida?

Nao.

### O que e igual

Nao foi encontrada cobertura externa confirmada nos pacotes avaliados.

### O que e diferente

`ARCH033` conhece a API `Microsoft.Extensions.DependencyInjection.IServiceCollection`, ignora contextos de teste quando configurado e evita confundir chamadas com APIs customizadas de mesmo nome.

### Recomendacao

Manter.

## 6. Recomendacoes finais

Boas candidatas a substituicao:

- `ARCH010`, por causa de `CA2016` e `MA0040`.
- `ARCH024`, por causa de `CA2254`, `MA0183` e regras Sonar de templates de log.
- `ARCH025`, por causa de `MA0180` e `S6672`.

Regras com sobreposicao parcial:

- `ARCH001`, `ARCH007`, `ARCH009`, `ARCH011`, `ARCH012`, `ARCH016`, `ARCH017`, `ARCH018`, `ARCH023`, `ARCH026` e `ARCH031`.

Regras que devem permanecer customizadas:

- `ARCH002`, `ARCH003`, `ARCH004`, `ARCH005`, `ARCH006`, `ARCH008`, `ARCH013`, `ARCH014`, `ARCH015`, `ARCH019`, `ARCH020`, `ARCH021`, `ARCH022`, `ARCH027`, `ARCH028`, `ARCH029`, `ARCH030`, `ARCH032` e `ARCH033`.

Regras que exigem teste comparativo antes de decisao:

- `ARCH001`, `ARCH007`, `ARCH009`, `ARCH010`, `ARCH018`, `ARCH024`, `ARCH025`, `ARCH026` e `ARCH031`.

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
