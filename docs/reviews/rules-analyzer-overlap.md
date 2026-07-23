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

| Regra | Sobreposicao conhecida | Decisao |
| ----- | ---------------------- | ------- |
| `REL001` | Regras genericas de async de Meziantou.Analyzer e SonarAnalyzer sao relacionadas. | Manter pelo recorte ASP.NET request flow e exclusoes para testes/hosted services. |
| `REL002` | Meziantou.Analyzer tem regra relacionada a tarefas nao observadas. | Manter pelo recorte de request ASP.NET, `Task`/`ValueTask` descartadas e ciclo de vida de request. |
| `REL003` | Nao ha substituto externo confirmado para a heuristica local. | Manter como opt-in por depender de politica EF Core de leitura. |
| `REL004` | Regras LINQ genericas podem apontar eficiencia, mas nao cobrem o mesmo recorte EF Core. | Manter pelo foco em materializacao antes de filtros/projecoes/paginacao. |
| `REL005` | Orientacao oficial do EF Core cobre o risco, mas nao foi confirmado analyzer consolidado com rastreamento local da mesma raiz de `DbContext`. | Manter pelo reconhecimento semantico de operacoes EF Core concorrentes no mesmo simbolo raiz. |
| `REL006` | Validacao de DI pode detectar alguns lifetimes em runtime, mas nao cobre a politica contextual de hosted services e tipos scoped conhecidos/configurados. | Manter pelo recorte de captura de scoped services em hosted services. |
| `ARC001` | Nao ha substituto externo confirmado para exigir decisao explicita por endpoint. | Manter como politica contextual de seguranca. |
| `ARC002` | Arquitetura de camadas costuma ser coberta por ferramentas de arquitetura, nao por analyzers genericos equivalentes. | Manter por configuracao de namespaces core/proibidos. |
| `ARC003` | Regras de roteamento ASP.NET podem validar formato, mas nao a politica de verbos em segmentos. | Manter como opt-in para APIs orientadas a recursos. |
| `ARC004` | Regras de design/imutabilidade sao relacionadas, mas nao identificam entidade de dominio com as mesmas opcoes. | Manter como opt-in para DDD. |
| `ARC005` | Nao ha substituto externo confirmado para comparar `AdditionalFiles` MSBuild com `Directory.Build.props`. | Manter como opt-in para repositorios que centralizam MSBuild. |
| `ARC006` | Analyzers ASP.NET podem validar binding, retornos ou serializacao, mas nao a politica local sobre tipos de dominio em contratos HTTP. | Manter como opt-in por depender de convencao arquitetural. |
| `TST001` | NSubstitute.Analyzers cobre usos incorretos do framework, mas nao a convencao local de evitar `Arg.Any()` e APIs `*AnyArgs` em setups e expectativas positivas. | Manter como opt-in para times que adotam a convencao, preservando verificacoes negativas permissivas. |
| `TST002` | Nao ha substituto externo confirmado para `Excluding*` dentro de `BeEquivalentTo()`. | Manter como opt-in para times que priorizam equivalencia estrita. |

## Substitutos externos

- `REL001` e `REL002`: analyzers gerais de async podem complementar a cobertura, mas tendem a nao restringir o alerta ao fluxo de request ASP.NET.
- `REL004`: analyzers LINQ e performance podem complementar, mas a regra local depende de origem `DbSet<T>` e materializadores EF Core.
- `REL005`: analyzers async genericos podem apontar padroes de concorrencia, mas a regra local exige operacoes EF Core e mesma raiz semantica de `DbContext`.
- `TST001`: NSubstitute.Analyzers pode complementar a cobertura de uso correto do framework, mas a regra local e uma convencao de precisao de teste para `Arg.Any()` e APIs `AnyArgs`.
- `ARC001`, `ARC002`, `ARC003`, `ARC004`, `ARC005`, `ARC006` e `TST002`: nenhum substituto direto foi confirmado nesta revisao.

## Apendice: regras v1 removidas

As regras v1 sem sucessor nao fazem parte da implementacao ativa da v2. A lista completa e os mapeamentos ficam em [migracao v2](../migration-v2.md).

Quando houver substitutos externos consolidados, prefira avalia-los antes de recriar uma regra local. Exemplos de temas removidos com cobertura externa ou relacionada incluem logging estruturado, categoria de `ILogger<T>`, CORS permissivo, uso de `System.Threading.Lock`, provedores de tempo e lifetime de `HttpClient`.
