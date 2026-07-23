# Sobreposição com analyzers externos

## Objetivo

Este documento revisa somente as 12 regras mantidas como produto ativo na v2: `REL001`-`REL005`, `ARC001`-`ARC005`, `TST001` e `TST002`.

Exclusividade não basta para justificar uma regra. Uma regra deve permanecer quando entrega diferenciação contextual, baixo ruído esperado e valor operacional claro para consumidores.

## Critérios de entrada e permanência

- O problema é concreto e recorrente em projetos .NET.
- A regra consegue diferenciar contexto, não apenas procurar padrões amplos.
- O diagnóstico tem baixo ruído ou opções públicas suficientes para calibragem.
- A correção esperada é operacionalmente útil.
- A regra não duplica de forma inferior um analyzer consolidado.
- Quando há ferramenta externa relacionada, a documentação explica coexistência, substituição ou escopo diferente.

## Matriz

| Regra | Sobreposição conhecida | Decisão |
| ----- | ---------------------- | ------- |
| `REL001` | Regras genéricas de async de Meziantou.Analyzer e SonarAnalyzer são relacionadas. | Manter pelo recorte ASP.NET request flow e exclusões para testes/hosted services. |
| `REL002` | Meziantou.Analyzer tem regra relacionada a tarefas não observadas. | Manter pelo recorte de request ASP.NET, `Task`/`ValueTask` descartadas e ciclo de vida de request. |
| `REL003` | Não há substituto externo confirmado para a heurística local. | Manter como opt-in por depender de política EF Core de leitura. |
| `REL004` | Regras LINQ genéricas podem apontar eficiência, mas não cobrem o mesmo recorte EF Core. | Manter pelo foco em materialização antes de filtros/projeções/paginação. |
| `REL005` | Orientacao oficial do EF Core cobre o risco, mas nao foi confirmado analyzer consolidado com rastreamento local da mesma raiz de `DbContext`. | Manter pelo reconhecimento semantico de operacoes EF Core concorrentes no mesmo simbolo raiz. |
| `ARC001` | Não há substituto externo confirmado para exigir decisão explícita por endpoint. | Manter como política contextual de segurança. |
| `ARC002` | Arquitetura de camadas costuma ser coberta por ferramentas de arquitetura, não por analyzers genéricos equivalentes. | Manter por configuração de namespaces core/proibidos. |
| `ARC003` | Regras de roteamento ASP.NET podem validar formato, mas não a política de verbos em segmentos. | Manter como opt-in para APIs orientadas a recursos. |
| `ARC004` | Regras de design/imutabilidade são relacionadas, mas não identificam entidade de domínio com as mesmas opções. | Manter como opt-in para DDD. |
| `ARC005` | Não há substituto externo confirmado para comparar `AdditionalFiles` MSBuild com `Directory.Build.props`. | Manter como opt-in para repositórios que centralizam MSBuild. |
| `TST001` | Não há substituto externo confirmado para a convenção específica de `NSubstitute.Arg.Any()`. | Manter como opt-in para times que adotam a convenção. |
| `TST002` | Não há substituto externo confirmado para `Excluding*` dentro de `BeEquivalentTo()`. | Manter como opt-in para times que priorizam equivalência estrita. |

## Substitutos externos

- `REL001` e `REL002`: analyzers gerais de async podem complementar a cobertura, mas tendem a não restringir o alerta ao fluxo de request ASP.NET.
- `REL004`: analyzers LINQ e performance podem complementar, mas a regra local depende de origem `DbSet<T>` e materializadores EF Core.
- `REL005`: analyzers async genericos podem apontar padroes de concorrencia, mas a regra local exige operacoes EF Core e mesma raiz semantica de `DbContext`.
- `ARC001`, `ARC002`, `ARC003`, `ARC004`, `ARC005`, `TST001` e `TST002`: nenhum substituto direto foi confirmado nesta revisão.

## Apêndice: regras v1 removidas

As regras v1 sem sucessor não fazem parte da implementação ativa da v2. A lista completa e os mapeamentos ficam em [migração v2](../migration-v2.md).

Quando houver substitutos externos consolidados, prefira avaliá-los antes de recriar uma regra local. Exemplos de temas removidos com cobertura externa ou relacionada incluem logging estruturado, categoria de `ILogger<T>`, CORS permissivo, uso de `System.Threading.Lock`, provedores de tempo e lifetime de `HttpClient`.
