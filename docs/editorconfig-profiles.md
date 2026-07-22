# Perfis de adoção via .editorconfig

Este guia oferece perfis prontos para adotar o `Swa.Analyzers` sem decidir a severidade regra por regra no primeiro dia. Os exemplos usam apenas severidades de `.editorconfig`; eles não alteram a severidade padrão implementada no pacote.

Use os perfis como ponto de partida. Projetos legados, bibliotecas públicas, APIs com contratos externos e bases com convenções já consolidadas devem ajustar o escopo por pasta ou por projeto.

## Como escolher

| Perfil | Quando usar | Custo esperado |
| ------ | ----------- | -------------- |
| `recommended` | Politica inicial para projetos ativos que querem valor sem bloqueio agressivo. | Médio. Regras mais objetivas aparecem como `warning`; regras opinativas ficam mais baixas. |
| `strict` | Bases novas ou já saneadas, com CI preparado para bloquear desvios relevantes. | Alto. Pode exigir correções antes de merge e suppressions justificadas. |
| `security` | APIs, serviços HTTP e componentes expostos que precisam priorizar autorização e CORS. | Médio. Foca segurança e mantém regras adjacentes visíveis. |
| `architecture` | Soluções com camadas bem definidas, domínio rico ou convenções de design de API. | Médio a alto. Depende do alinhamento entre namespaces reais e convenções do time. |
| `testing` | Repositórios que querem padronizar testes, asserções e mocks. | Baixo a médio. Costuma exigir ajustes em testes existentes. |
| `legacy-safe` | Primeira execução em bases grandes ou legadas, sem risco de quebrar build. | Baixo. Mantem problemas visíveis sem impor bloqueio. |

## Mapa das regras

O risco de ruído abaixo é uma recomendação prática para adoção. Ele considera o quanto a regra depende de convenções locais, stubs, arquitetura do projeto ou migração de legado; não muda o comportamento do analyzer.

| ID | Categoria | Severidade padrão | Risco de ruído | Observacao para perfis |
| -- | --------- | ----------------- | -------------- | ---------------------- |
| TST001 | TestQuality | Info | Médio | Depende do padrão de mocks adotado. |
| TST002 | TestQuality | Info | Médio | Pode apontar exceções aceitaveis em testes de contrato. |
| ARC003 | Design | Info | Médio | Depende de idioma, domínio e estilo de rotas. |
| REL001 | Performance | Warning | Médio | Especifica para fluxo de request ASP.NET. |
| REL002 | Reliability | Warning | Baixo | Boa candidata a `warning` em APIs. |
| ARC001 | Security | Warning | Médio | Exige política explícita de endpoints públicos. |
| REL003 | Performance | Info | Médio | Depende do uso de EF Core e intenção de tracking. |
| REL004 | Performance | Warning | Médio | Pode exigir leitura de intenção da consulta. |
| ARC002 | Architecture | Warning | Alto | Configure namespaces antes de promover. |
| ARC004 | Design | Info | Alto | Configure namespaces/base types de entidades antes de promover. |
| ARC005 | Maintainability | Info | Médio | Requer AdditionalFiles de projetos para maior utilidade. |

## Perfil recommended

Use como ponto de partida para projetos ativos. Ele mantém como `warning` regras objetivas de confiabilidade, segurança e observabilidade, mas reduz regras de design, arquitetura, testes e performance que costumam precisar de calibragem local.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.TST001.severity = suggestion
dotnet_diagnostic.TST002.severity = suggestion
dotnet_diagnostic.ARC003.severity = info
dotnet_diagnostic.REL001.severity = info
dotnet_diagnostic.REL002.severity = warning
dotnet_diagnostic.ARC001.severity = warning
dotnet_diagnostic.REL003.severity = info
dotnet_diagnostic.REL004.severity = info
dotnet_diagnostic.ARC002.severity = info
dotnet_diagnostic.ARC004.severity = info

[*.csproj]
dotnet_diagnostic.ARC005.severity = suggestion
```

## Perfil strict

Use em bases novas, repositórios já saneados ou módulos em que o CI pode bloquear desvios. Este perfil eleva segurança e problemas async mais críticos para `error`, mantém a maior parte das regras como `warning` e deixa apenas convenções de teste muito opinativas em severidade menor.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.TST001.severity = warning
dotnet_diagnostic.TST002.severity = warning
dotnet_diagnostic.ARC003.severity = warning
dotnet_diagnostic.REL001.severity = warning
dotnet_diagnostic.REL002.severity = error
dotnet_diagnostic.ARC001.severity = error
dotnet_diagnostic.REL003.severity = warning
dotnet_diagnostic.REL004.severity = warning
dotnet_diagnostic.ARC002.severity = warning
dotnet_diagnostic.ARC004.severity = warning

[*.csproj]
dotnet_diagnostic.ARC005.severity = warning
```


## Perfil security

Use quando a prioridade é reduzir risco em endpoints HTTP, autorização e CORS. Regras adjacentes ficam em `info` para ajudar revisões sem ampliar demais o escopo.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.REL002.severity = info
dotnet_diagnostic.ARC001.severity = warning
dotnet_diagnostic.ARC002.severity = info
```


## Perfil architecture

Use em soluções com separação clara entre domínio, aplicação e infraestrutura. Configure ARC002 e ARC004 para os namespaces reais do projeto antes de elevar para `warning` em uma base existente.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.ARC003.severity = warning
dotnet_diagnostic.ARC002.severity = warning
dotnet_diagnostic.ARC004.severity = warning

# Ajuste para os namespaces reais da solução.
dotnet_diagnostic.ARC002.core_namespace_patterns = "*.Domain;*.Application"
dotnet_diagnostic.ARC002.forbidden_namespace_patterns = "Microsoft.EntityFrameworkCore;Microsoft.AspNetCore;StackExchange.Redis;Npgsql"
dotnet_diagnostic.ARC002.allowed_namespace_patterns =
dotnet_diagnostic.ARC002.ignore_tests = true

dotnet_diagnostic.ARC004.entity_namespaces = ["MyApp.Domain.Entities", "MyApp.Domain.Aggregates"]
dotnet_diagnostic.ARC004.entity_base_types = ["Entity", "AggregateRoot"]
dotnet_diagnostic.ARC004.allow_internal_setters = false

[*.csproj]
dotnet_diagnostic.ARC005.severity = info
```

## Perfil testing

Use para padronizar testes, mocks e asserções. O perfil deixa a convenção `_sut` como `info` porque ela tende a refletir preferência local de nome, mas promove regras relacionadas a qualidade de asserção e mocks.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.TST001.severity = warning
dotnet_diagnostic.TST002.severity = warning
```


## Perfil legacy-safe

Use para o primeiro ciclo em bases grandes ou antigas. Ele evita `warning` e `error`, reduz regras de convenção para `silent` e mantém riscos operacionais como `info` para inventário.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.TST001.severity = silent
dotnet_diagnostic.TST002.severity = silent
dotnet_diagnostic.ARC003.severity = silent
dotnet_diagnostic.REL001.severity = info
dotnet_diagnostic.REL002.severity = info
dotnet_diagnostic.ARC001.severity = info
dotnet_diagnostic.REL003.severity = silent
dotnet_diagnostic.REL004.severity = silent
dotnet_diagnostic.ARC002.severity = silent
dotnet_diagnostic.ARC004.severity = silent

[*.csproj]
dotnet_diagnostic.ARC005.severity = silent
```


## Escopo por pasta

Os perfis podem ser combinados com escopos de `.editorconfig`. Exemplo: política recomendada no repositório inteiro, mas legado apenas informativo.

```ini
root = true

[*.cs]
dotnet_diagnostic.ARC001.severity = warning

[src/Legacy/**/*.cs]
dotnet_diagnostic.ARC001.severity = info
```

Prefira escopo por pasta a `NoWarn` quando a regra continua útil para código novo. Use `none` somente quando uma regra não se aplica ao projeto ou quando há uma decisão documentada de não adotá-la.
