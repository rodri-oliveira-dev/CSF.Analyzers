# Revisão de segurança da biblioteca

## Resumo executivo

Esta revisão analisou o `Swa.Analyzers` como uma biblioteca local de analyzers Roslyn executada durante build/IDE, não como aplicação web ou serviço produtivo. Não foram identificadas vulnerabilidades críticas ou altas. Os riscos mais relevantes são de robustez: possível DoS local por arquivos MSBuild grandes ou XML especialmente custoso, lacunas de testes para configurações malformadas em algumas regras, e duplicidade de parsers/configuração que pode gerar comportamento divergente no futuro.

O projeto já adota boas práticas importantes: todos os analyzers revisados chamam `EnableConcurrentExecution()`, configuram `GeneratedCodeAnalysisFlags.None`, tratam símbolos nulos de forma conservadora em vários pontos e usam `CancellationToken` nas principais chamadas semânticas e nos loops de `AdditionalFiles`.

## Escopo analisado

- API pública do pacote `src/Swa.Analyzers.Core`, incluindo classes públicas de analyzers, `RuleIdentifiers` e `RuleHelpLinks`.
- Regras em `src/Swa.Analyzers.Core/Rules`, com foco em callbacks Roslyn, uso de `SemanticModel`, null handling, cancellation, configuração e diagnóstico.
- Opções `.editorconfig` documentadas no `README.md` e em `docs/rules`.
- Parsing de arrays JSON manuais, opções booleanas, padrões de namespace, paths e XML/MSBuild em `AdditionalFiles`.
- Testes em `tests/Swa.Analyzers.Tests/Rules`, com foco em valores inválidos, ausentes e malformados.
- Documentação pública em `README.md` e `docs/rules`.

## Fora de escopo

Este projeto não foi avaliado como aplicação web. Ficaram fora de escopo: SQL Injection, banco de dados, autenticação de usuário, autorização de usuário, sessão, cookies, CORS da aplicação consumidora como infraestrutura, SSRF, chamadas HTTP externas de negócio, secrets produtivos, deploy cloud, Kubernetes, TLS, firewall e endpoints REST próprios. Regras que analisam ASP.NET, CORS ou autorização foram avaliadas apenas como código de analyzer da biblioteca.

## Matriz de achados

| ID | Severidade | Categoria | Arquivo | Problema | Impacto | Recomendação | Prioridade |
|---|---|---|---|---|---|---|---|
| SEC001 | Média | Denial of service | `Arch030AvoidDuplicatedPackageReferencesAcrossProjectsAnalyzer.cs`, `Arch032AvoidDuplicatedMsBuildPropertiesAnalyzer.cs` | Parsing XML de `AdditionalFiles` converte todo o arquivo para `string` e usa `XDocument.Parse` sem limite explícito | Arquivos `.csproj`/`Directory.Build.props` muito grandes ou XML com estrutura custosa podem degradar build/IDE local | Adicionar limites razoáveis, parser XML endurecido e testes de arquivos grandes/malformados | P1 |
| SEC002 | Baixa | Malformed configuration | `Arch020RequireExplicitAuthorizationOnHttpEndpointsAnalyzer.cs` | Configurações JSON malformadas caem silenciosamente para listas vazias, mas não há teste/documentação específica desse fallback | Mudanças futuras podem transformar configuração inválida em exceção ou comportamento menos previsível | Adicionar testes para JSON inválido em `allowed_routes`, `allowed_methods` e `ignored_namespaces`; documentar fallback | P2 |
| SEC003 | Baixa | Denial of service | `Arch027PreventInfrastructureDependenciesInCoreLayersAnalyzer.cs`, `Arch030AvoidDuplicatedPackageReferencesAcrossProjectsAnalyzer.cs` | Listas/padrões configuráveis não têm limite de tamanho e são avaliados em caminhos frequentes | `.editorconfig` excessivo pode aumentar CPU de análise local, especialmente em soluções grandes | Limitar ou normalizar padrões por arquivo/compilation e testar listas grandes | P2 |
| SEC004 | Informativa | XML/JSON parsing | Regras `ARCH015`, `ARCH020`, `ARCH023`, `ARCH029`, `ARCH030`, `ARCH032` | Parser manual de array JSON aceita apenas subconjunto de escapes e não suporta `\uXXXX` | JSON válido com unicode escapado é tratado como inválido, reduzindo previsibilidade da configuração | Extrair helper compartilhado ou usar parser seguro disponível para `netstandard2.0`; documentar subconjunto aceito | P3 |
| SEC005 | Informativa | Documentation gap | `README.md`, `docs/rules/ARCH020.md`, `docs/rules/ARCH023.md`, `docs/rules/ARCH029.md` | Nem toda opção pública descreve claramente fallback para valor vazio, inválido ou JSON malformado | Consumidores podem interpretar fallback silencioso como bug ou configurar allowlists sem perceber que foram ignoradas | Padronizar documentação de fallback para todas as opções `.editorconfig` | P3 |

## Achados detalhados

### SEC001 - Parsing XML de AdditionalFiles pode causar DoS local de build

**Severidade:** Média  
**Categoria:** Denial of service  
**Arquivo(s):** `src/Swa.Analyzers.Core/Rules/Arch030AvoidDuplicatedPackageReferencesAcrossProjectsAnalyzer.cs`, `src/Swa.Analyzers.Core/Rules/Arch032AvoidDuplicatedMsBuildPropertiesAnalyzer.cs`  
**Evidência:** `ARCH030` registra análise em `RegisterCompilationEndAction`, percorre `Options.AdditionalFiles`, chama `additionalFile.GetText(context.CancellationToken)` e depois `XDocument.Parse(sourceText.ToString(), LoadOptions.PreserveWhitespace)`. `ARCH032` faz fluxo equivalente para `.csproj` e `Directory.Build.props`, usando `LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo`.  
**Cenário de exploração ou mau uso:** Um consumidor inclui como `AdditionalFiles` um `.csproj` ou `Directory.Build.props` muito grande, profundamente aninhado ou com XML especialmente custoso. Mesmo sendo local, isso pode acontecer por erro de configuração, geração de projeto ou PR malicioso em um repositório que executa analyzers no CI.  
**Impacto:** Consumo elevado de memória/CPU no build, no CI ou na IDE. O risco é local/build-time; não é DoS remoto. XML inválido é tratado com `catch (XmlException)`, o que é positivo, mas não mitiga tamanho excessivo ou custo de parsing antes da exceção.  
**Recomendação:** Antes de converter para `string` e parsear, aplicar limite de tamanho razoável para arquivos MSBuild analisados. Considerar `XmlReader` com configurações explícitas, incluindo proibição/limitação de DTD quando aplicável, e evitar duplicar buffers grandes sem necessidade. Manter cancellation checks antes/depois de cada arquivo.  
**Sugestão de teste futuro:** Simular `AdditionalFiles` com XML malformado, arquivo vazio, arquivo grande, XML com DTD/entidades internas e múltiplos projetos para garantir que o analyzer falha fechado e não reporta exceção.

### SEC002 - Fallback de configuração malformada em ARCH020 não tem teste dedicado

**Severidade:** Baixa  
**Categoria:** Malformed configuration  
**Arquivo(s):** `src/Swa.Analyzers.Core/Rules/Arch020RequireExplicitAuthorizationOnHttpEndpointsAnalyzer.cs`, `tests/Swa.Analyzers.Tests/Rules/Arch020RequireExplicitAuthorizationOnHttpEndpointsAnalyzerTests.cs`, `docs/rules/ARCH020.md`  
**Evidência:** `AuthorizationRuleOptions.Create` lê `allowed_routes`, `allowed_methods` e `ignored_namespaces` via `ReadStringArray`; quando `TryParseJsonStringArray` falha, o método retorna vazio. Os testes cobrem valores válidos para as três opções, mas não há caso explícito para JSON malformado, valor não array, item não string ou array com aspas não fechadas.  
**Cenário de exploração ou mau uso:** Um consumidor configura `dotnet_diagnostic.ARCH020.allowed_routes = /internal/status` ou um array JSON incompleto esperando liberar uma rota técnica. A configuração é ignorada silenciosamente e a regra passa a reportar diagnóstico.  
**Impacto:** Não há bypass de segurança da biblioteca; o fallback é conservador. O impacto é previsibilidade de build e risco de regressão futura, pois o comportamento de configuração inválida não está fixado por teste.  
**Recomendação:** Adicionar testes de configuração inválida para cada opção e documentar que valores malformados são ignorados, mantendo a política mais restritiva.  
**Sugestão de teste futuro:** `allowed_routes = ["/health",`, `allowed_methods = Ping`, `ignored_namespaces = [123]` e arrays com string vazia/whitespace.

### SEC003 - Padrões configuráveis sem limite podem amplificar custo de análise

**Severidade:** Baixa  
**Categoria:** Denial of service  
**Arquivo(s):** `src/Swa.Analyzers.Core/Rules/Arch027PreventInfrastructureDependenciesInCoreLayersAnalyzer.cs`, `src/Swa.Analyzers.Core/Rules/Arch030AvoidDuplicatedPackageReferencesAcrossProjectsAnalyzer.cs`  
**Evidência:** `ARCH027` lê listas separadas por `;` em `ReadPatternList` e avalia `MatchesAnyPattern`/`MatchesWildcard` em callbacks de `UsingDirective` e nomes de tipo. `ARCH030` avalia `allowed_project_patterns` em cada `.csproj` adicional. Não há limite de quantidade/tamanho dos padrões configurados.  
**Cenário de exploração ou mau uso:** `.editorconfig` com centenas ou milhares de padrões, ou padrões longos com `*`, aplicado a solução grande.  
**Impacto:** Aumento de CPU durante análise local/CI. O algoritmo é simples e não usa regex catastrófica, então o risco é limitado, mas proporcional ao tamanho da configuração e da solução.  
**Recomendação:** Normalizar e cachear padrões já processados, considerar limite defensivo por opção e adicionar testes de listas grandes para evitar regressões de complexidade.  
**Sugestão de teste futuro:** Configurar centenas de padrões em `ARCH027` e `ARCH030` e validar que a execução permanece previsível, sem exceções e com cancellation respeitado.

### SEC004 - Parser JSON manual duplicado aceita subconjunto de JSON

**Severidade:** Informativa  
**Categoria:** XML/JSON parsing  
**Arquivo(s):** `src/Swa.Analyzers.Core/Rules/Arch015ProhibitVerbsInHttpRoutesAnalyzer.cs`, `src/Swa.Analyzers.Core/Rules/Arch020RequireExplicitAuthorizationOnHttpEndpointsAnalyzer.cs`, `src/Swa.Analyzers.Core/Rules/Arch023PreferTimeProviderAnalyzer.cs`, `src/Swa.Analyzers.Core/Rules/Arch029ProhibitPublicSettersInDomainEntitiesAnalyzer.cs`, `src/Swa.Analyzers.Core/Rules/Arch030AvoidDuplicatedPackageReferencesAcrossProjectsAnalyzer.cs`, `src/Swa.Analyzers.Core/Rules/Arch032AvoidDuplicatedMsBuildPropertiesAnalyzer.cs`  
**Evidência:** Cada arquivo contém uma versão local de `JsonStringArrayParser`/`TryParseJsonStringArray`. O parser aceita arrays de strings e escapes comuns (`\"`, `\\`, `\/`, `\b`, `\f`, `\n`, `\r`, `\t`), mas rejeita escapes unicode `\uXXXX`, que são JSON válido.  
**Cenário de exploração ou mau uso:** Configuração `.editorconfig` gerada por ferramenta usa escapes unicode para nomes de namespace, rotas ou pacotes. A opção é tratada como inválida e cai para default/lista vazia, dependendo da regra.  
**Impacto:** Robustez e previsibilidade. Não há execução de código nem vazamento de dados; o risco é inconsistência de configuração e manutenção.  
**Recomendação:** Extrair helper interno único para arrays JSON de strings, com comportamento documentado e testes compartilhados. Alternativamente usar parser JSON de plataforma compatível com o target do pacote, sem adicionar dependência desnecessária.  
**Sugestão de teste futuro:** Array com `"\u0041pp"`, barra invertida final, aspas não fechadas, número, objeto, array vazio e strings com whitespace.

### SEC005 - Fallback de opções públicas não é documentado de forma uniforme

**Severidade:** Informativa  
**Categoria:** Documentation gap  
**Arquivo(s):** `README.md`, `docs/rules/ARCH020.md`, `docs/rules/ARCH023.md`, `docs/rules/ARCH029.md`, `docs/rules/ARCH030.md`, `docs/rules/ARCH032.md`  
**Evidência:** `ARCH015`, `ARCH030` e `ARCH032` documentam fallback de JSON inválido com mais clareza. Outras opções públicas aparecem no README e nas docs, mas nem sempre descrevem valor ausente, vazio, inválido, casing inesperado e JSON malformado.  
**Cenário de exploração ou mau uso:** Consumidor configura uma allowlist em formato inválido e interpreta diagnósticos resultantes como falso positivo, quando a opção foi ignorada por desenho.  
**Impacto:** Baixo. Pode gerar ruído de adoção e correções equivocadas, mas não expõe segredos nem cria vulnerabilidade direta.  
**Recomendação:** Padronizar uma seção de configuração em cada regra com: valores aceitos, valor default, tratamento de vazio, tratamento de inválido e se a regra reporta ou ignora erro de configuração.  
**Sugestão de teste futuro:** Não se aplica diretamente ao runtime; validar via revisão de documentação e testes de snapshot se o projeto adotar esse padrão.

## Entradas e casos inválidos que precisam ser considerados

- `.editorconfig` ausente: já há cobertura parcial; manter fallback seguro por regra.
- Valor vazio ou whitespace em opções booleanas: deve cair para default, sem exceção.
- Booleanos com casing inesperado (`TRUE`, `False`): `bool.TryParse` aceita casing variado; manter testes onde a opção for pública.
- JSON malformado: aspas não fechadas, vírgula final, valor não array, array com número/objeto, barra invertida final.
- JSON válido com unicode escapado (`\uXXXX`): hoje tende a ser rejeitado pelo parser manual.
- Arrays muito grandes em `.editorconfig`: risco de CPU/memória local; deve haver limite ou testes de comportamento.
- Padrões de namespace/projeto muito longos ou em grande quantidade: validar custo em `ARCH027` e `ARCH030`.
- `AdditionalFiles` ausentes: regras `ARCH030` e `ARCH032` devem permanecer silenciosas.
- `AdditionalFiles` com path relativo, absoluto, Windows, Linux, `..` e casing diferente: especialmente em `ARCH032`, que decide ancestralidade por string normalizada.
- XML vazio, XML inválido, XML com namespace MSBuild, elementos condicionais, encoding incomum e arquivos grandes.
- Código C# incompleto/parcialmente compilável: todos os analyzers devem manter padrão de sair cedo quando símbolos ou tipos estão ausentes.
- Diagnósticos com strings vindas de código analisado: rotas, nomes de método, pacote e propriedade MSBuild aparecem no build; evitar incluir conteúdo de arquivos ou valores de propriedades.

## Duplicidades e oportunidades de refatoração

- `JsonStringArrayParser` duplicado em `ARCH015`, `ARCH020`, `ARCH023`, `ARCH029`, `ARCH030` e `ARCH032`. Risco: correções de parsing e novos testes precisam ser replicados. Sugestão: extrair helper interno compartilhado, com contrato explícito para fallback e escapes aceitos. Urgência: opcional, mas recomendada antes de adicionar novas opções JSON.
- Leitura de arrays de string em `.editorconfig` aparece em várias regras com pequenas diferenças de default, casing e fallback. Risco: inconsistência de comportamento público. Sugestão: criar helper para `ReadStringArrayOption`, recebendo default, normalizador e comparador. Urgência: P3.
- Leitura de booleanos (`ReadBoolean`) é repetida em `ARCH023`, `ARCH026`, `ARCH027`, `ARCH028`, `ARCH029`, `ARCH031` e `ARCH032`. Risco: diferenças sutis em `Trim`, default e global/tree options. Sugestão: helper comum para booleanos por arquivo/global quando aplicável. Urgência: P3.
- Wildcard matching é implementado em `ARCH027` e `ARCH030`. Risco: divergência em comparação case-sensitive/case-insensitive e tratamento de separadores. Sugestão: helper compartilhado com comparador configurável. Urgência: P3.
- Parsing XML e varredura de `AdditionalFiles` em `ARCH030` e `ARCH032` têm estrutura parecida. Risco: hardening de XML e limites de tamanho precisam ser aplicados em dois lugares. Sugestão: helper interno para ler `AdditionalText` MSBuild de forma segura. Urgência: P2, por causa de SEC001.
- Stubs de frameworks nos testes aparecem repetidos em regras ASP.NET/EF/Logging. Risco baixo, mas aumenta custo para atualizar APIs simuladas. Sugestão: extrair constantes de stubs por domínio de teste quando houver nova manutenção ampla. Urgência: opcional.

## Itens analisados sem achado relevante

- Todos os analyzers `ARCH001` a `ARCH032` chamam `EnableConcurrentExecution()` e `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`.
- O uso de `SemanticModel` em regras revisadas é majoritariamente protegido por checagens de `null`/tipo antes de reportar diagnóstico.
- Regras que dependem de símbolos de frameworks tendem a falhar silenciosamente quando os tipos não existem, evitando exceções em projetos que não referenciam ASP.NET, EF ou logging.
- Não foram encontrados logs próprios da biblioteca que concatenem entrada do usuário.
- Diagnósticos revisados não incluem conteúdo bruto de arquivos, tokens ou secrets; eles exibem nomes de símbolo, rota literal, pacote, propriedade ou projeto, que são esperados em output de analyzer.
- Não há APIs de negócio, endpoints HTTP próprios, autenticação, banco de dados ou chamadas externas de produção no projeto.
- `ARCH030` e `ARCH032` já respeitam `CancellationToken` durante loops de arquivos e ignoram XML inválido via `XmlException`.
- Não foi identificado uso de regex em caminho quente; os matchers são manuais.

## Plano de ação priorizado

### P0

Nenhum item P0 identificado.

### P1

- Endurecer parsing XML de `ARCH030` e `ARCH032` contra arquivos grandes e XML custoso.
- Adicionar testes de `AdditionalFiles` grandes/malformados para regras MSBuild.

### P2

- Cobrir configuração malformada de `ARCH020`.
- Avaliar limites/caches para listas e padrões configuráveis em `ARCH027` e `ARCH030`.
- Considerar helper comum para leitura segura de `AdditionalFiles` MSBuild.

### P3

- Extrair parser JSON de array de strings para helper compartilhado.
- Padronizar helpers de leitura de booleanos e arrays em `.editorconfig`.
- Documentar fallback de configuração inválida de forma uniforme em todas as regras com opções públicas.
- Reduzir duplicidade de stubs de testes quando houver manutenção maior.

## Checklist de segurança para futuras regras ARCH

- Definir explicitamente o comportamento para opção ausente, vazia, inválida e malformada.
- Preferir parser estruturado a parser manual; se o parser for manual, documentar o subconjunto aceito.
- Adicionar testes para código C# incompleto, símbolos ausentes e projetos sem referência ao framework alvo.
- Usar `EnableConcurrentExecution()` e `ConfigureGeneratedCodeAnalysis(...)`.
- Passar `CancellationToken` em chamadas semânticas e loops de arquivos.
- Evitar análise cara em todo nó sem filtro sintático inicial.
- Cachear opções por `SyntaxTree` ou por compilation quando a regra usar `.editorconfig`.
- Limitar leitura/processamento de `AdditionalFiles` quando o arquivo puder ser grande.
- Não incluir conteúdo de arquivos, valores de configuração sensíveis ou mensagens longas em diagnósticos.
- Tratar paths com separadores Windows/Linux e não assumir formato absoluto.
- Testar falso positivo e falso negativo antes de ampliar heurísticas.
- Registrar na documentação o fallback de configuração e as limitações conhecidas.

## Observações finais

A postura geral da biblioteca é conservadora e adequada para analyzers Roslyn. Os achados não indicam risco remoto ou exposição produtiva; são riscos de build-time, robustez e manutenção. A prioridade mais prática é endurecer os analyzers que leem XML via `AdditionalFiles`, porque eles processam entrada fora da árvore C# e podem afetar diretamente desempenho de CI/IDE em soluções grandes.
