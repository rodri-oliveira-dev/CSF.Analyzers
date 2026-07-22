# Perfis de adoÃ§Ã£o via .editorconfig

Este guia oferece perfis prontos para adotar o `Swa.Analyzers` sem decidir a severidade regra por regra no primeiro dia. Os exemplos usam apenas severidades de `.editorconfig`; eles nÃ£o alteram a severidade padrÃ£o implementada no pacote.

Use os perfis como ponto de partida. Projetos legados, bibliotecas pÃºblicas, APIs com contratos externos e bases com convenÃ§Ãµes jÃ¡ consolidadas devem ajustar o escopo por pasta ou por projeto.

## Como escolher

| Perfil | Quando usar | Custo esperado |
| ------ | ----------- | -------------- |
| `recommended` | Politica inicial para projetos ativos que querem valor sem bloqueio agressivo. | MÃ©dio. Regras mais objetivas aparecem como `warning`; regras opinativas ficam mais baixas. |
| `strict` | Bases novas ou jÃ¡ saneadas, com CI preparado para bloquear desvios relevantes. | Alto. Pode exigir correÃ§Ãµes antes de merge e suppressions justificadas. |
| `security` | APIs, serviÃ§os HTTP e componentes expostos que precisam priorizar autorizaÃ§Ã£o e CORS. | MÃ©dio. Foca seguranÃ§a e mantÃ©m regras adjacentes visÃ­veis. |
| `architecture` | SoluÃ§Ãµes com camadas bem definidas, domÃ­nio rico ou convenÃ§Ãµes de design de API. | MÃ©dio a alto. Depende do alinhamento entre namespaces reais e convenÃ§Ãµes do time. |
| `testing` | RepositÃ³rios que querem padronizar testes, asserÃ§Ãµes e mocks. | Baixo a mÃ©dio. Costuma exigir ajustes em testes existentes. |
| `legacy-safe` | Primeira execuÃ§Ã£o em bases grandes ou legadas, sem risco de quebrar build. | Baixo. Mantem problemas visÃ­veis sem impor bloqueio. |

## Mapa das regras

O risco de ruÃ­do abaixo Ã© uma recomendaÃ§Ã£o prÃ¡tica para adoÃ§Ã£o. Ele considera o quanto a regra depende de convenÃ§Ãµes locais, stubs, arquitetura do projeto ou migraÃ§Ã£o de legado; nÃ£o muda o comportamento do analyzer.

| ID | Categoria | Severidade padrÃ£o | Risco de ruÃ­do | Observacao para perfis |
| -- | --------- | ----------------- | -------------- | ---------------------- |
| ARCH005 | TestQuality | Info | MÃ©dio | Depende do padrÃ£o de mocks adotado. |
| ARCH006 | TestQuality | Info | MÃ©dio | Pode apontar exceÃ§Ãµes aceitaveis em testes de contrato. |
| ARCH015 | Design | Warning | MÃ©dio | Depende de idioma, domÃ­nio e estilo de rotas. |
| ARCH016 | Performance | Warning | MÃ©dio | Especifica para fluxo de request ASP.NET. |
| ARCH017 | Reliability | Warning | Baixo | Boa candidata a `warning` em APIs. |
| ARCH020 | Security | Warning | MÃ©dio | Exige polÃ­tica explÃ­cita de endpoints pÃºblicos. |
| ARCH021 | Performance | Warning | MÃ©dio | Depende do uso de EF Core e intenÃ§Ã£o de tracking. |
| ARCH022 | Performance | Warning | MÃ©dio | Pode exigir leitura de intenÃ§Ã£o da consulta. |
| ARCH027 | Architecture | Warning | Alto | Configure namespaces antes de promover. |
| ARCH029 | Design | Warning | Alto | Configure namespaces/base types de entidades antes de promover. |
| ARCH032 | Maintainability | Info | MÃ©dio | Requer AdditionalFiles de projetos para maior utilidade. |

## Perfil recommended

Use como ponto de partida para projetos ativos. Ele mantÃ©m como `warning` regras objetivas de confiabilidade, seguranÃ§a e observabilidade, mas reduz regras de design, arquitetura, testes e performance que costumam precisar de calibragem local.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.ARCH005.severity = suggestion
dotnet_diagnostic.ARCH006.severity = suggestion
dotnet_diagnostic.ARCH015.severity = info
dotnet_diagnostic.ARCH016.severity = info
dotnet_diagnostic.ARCH017.severity = warning
dotnet_diagnostic.ARCH020.severity = warning
dotnet_diagnostic.ARCH021.severity = info
dotnet_diagnostic.ARCH022.severity = info
dotnet_diagnostic.ARCH027.severity = info
dotnet_diagnostic.ARCH029.severity = info

[*.csproj]
dotnet_diagnostic.ARCH032.severity = suggestion
```

## Perfil strict

Use em bases novas, repositÃ³rios jÃ¡ saneados ou mÃ³dulos em que o CI pode bloquear desvios. Este perfil eleva seguranÃ§a e problemas async mais crÃ­ticos para `error`, mantÃ©m a maior parte das regras como `warning` e deixa apenas convenÃ§Ãµes de teste muito opinativas em severidade menor.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.ARCH005.severity = warning
dotnet_diagnostic.ARCH006.severity = warning
dotnet_diagnostic.ARCH015.severity = warning
dotnet_diagnostic.ARCH016.severity = warning
dotnet_diagnostic.ARCH017.severity = error
dotnet_diagnostic.ARCH020.severity = error
dotnet_diagnostic.ARCH021.severity = warning
dotnet_diagnostic.ARCH022.severity = warning
dotnet_diagnostic.ARCH027.severity = warning
dotnet_diagnostic.ARCH029.severity = warning

[*.csproj]
dotnet_diagnostic.ARCH032.severity = warning
```


## Perfil security

Use quando a prioridade Ã© reduzir risco em endpoints HTTP, autorizaÃ§Ã£o e CORS. Regras adjacentes ficam em `info` para ajudar revisÃµes sem ampliar demais o escopo.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.ARCH017.severity = info
dotnet_diagnostic.ARCH020.severity = warning
dotnet_diagnostic.ARCH027.severity = info
```


## Perfil architecture

Use em soluÃ§Ãµes com separaÃ§Ã£o clara entre domÃ­nio, aplicaÃ§Ã£o e infraestrutura. Configure ARCH027 e ARCH029 para os namespaces reais do projeto antes de elevar para `warning` em uma base existente.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.ARCH015.severity = warning
dotnet_diagnostic.ARCH027.severity = warning
dotnet_diagnostic.ARCH029.severity = warning

# Ajuste para os namespaces reais da soluÃ§Ã£o.
dotnet_diagnostic.ARCH027.core_namespace_patterns = "*.Domain;*.Application"
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = "Microsoft.EntityFrameworkCore;Microsoft.AspNetCore;StackExchange.Redis;Npgsql"
dotnet_diagnostic.ARCH027.allowed_namespace_patterns =
dotnet_diagnostic.ARCH027.ignore_tests = true

dotnet_diagnostic.ARCH029.entity_namespaces = ["MyApp.Domain.Entities", "MyApp.Domain.Aggregates"]
dotnet_diagnostic.ARCH029.entity_base_types = ["Entity", "AggregateRoot"]
dotnet_diagnostic.ARCH029.allow_internal_setters = false

[*.csproj]
dotnet_diagnostic.ARCH032.severity = info
```

## Perfil testing

Use para padronizar testes, mocks e asserÃ§Ãµes. O perfil deixa a convenÃ§Ã£o `_sut` como `info` porque ela tende a refletir preferÃªncia local de nome, mas promove regras relacionadas a qualidade de asserÃ§Ã£o e mocks.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.ARCH005.severity = warning
dotnet_diagnostic.ARCH006.severity = warning
```


## Perfil legacy-safe

Use para o primeiro ciclo em bases grandes ou antigas. Ele evita `warning` e `error`, reduz regras de convenÃ§Ã£o para `silent` e mantÃ©m riscos operacionais como `info` para inventÃ¡rio.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.ARCH005.severity = silent
dotnet_diagnostic.ARCH006.severity = silent
dotnet_diagnostic.ARCH015.severity = silent
dotnet_diagnostic.ARCH016.severity = info
dotnet_diagnostic.ARCH017.severity = info
dotnet_diagnostic.ARCH020.severity = info
dotnet_diagnostic.ARCH021.severity = silent
dotnet_diagnostic.ARCH022.severity = silent
dotnet_diagnostic.ARCH027.severity = silent
dotnet_diagnostic.ARCH029.severity = silent

[*.csproj]
dotnet_diagnostic.ARCH032.severity = silent
```


## Escopo por pasta

Os perfis podem ser combinados com escopos de `.editorconfig`. Exemplo: polÃ­tica recomendada no repositÃ³rio inteiro, mas legado apenas informativo.

```ini
root = true

[*.cs]
dotnet_diagnostic.ARCH020.severity = warning

[src/Legacy/**/*.cs]
dotnet_diagnostic.ARCH020.severity = info
```

Prefira escopo por pasta a `NoWarn` quando a regra continua Ãºtil para cÃ³digo novo. Use `none` somente quando uma regra nÃ£o se aplica ao projeto ou quando hÃ¡ uma decisÃ£o documentada de nÃ£o adotÃ¡-la.
