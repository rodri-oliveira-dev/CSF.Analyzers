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
| ARCH001 | Reliability | Warning | Baixo | Boa candidata a `warning` cedo. |
| ARCH002 | Reliability | Warning | Médio | Pode existir em código legado com callbacks intencionais. |
| ARCH003 | TestQuality | Info | Médio | Opinativa para estilo de asserção em testes. |
| ARCH004 | TestQuality | Info | Alto | Convencao de nome; melhor promover por time. |
| ARCH005 | TestQuality | Info | Médio | Depende do padrão de mocks adotado. |
| ARCH006 | TestQuality | Info | Médio | Pode apontar exceções aceitaveis em testes de contrato. |
| ARCH007 | Performance | Info | Médio | Boa visibilidade, mas nem todo loop é crítico. |
| ARCH008 | Reliability | Info | Baixo | Geralmente objetiva em sinks de filesystem. |
| ARCH009 | Reliability | Warning | Baixo | Boa candidata a `warning` ou `error` após triagem. |
| ARCH010 | Reliability | Warning | Médio | Pode exigir desenho consistente de cancelamento. |
| ARCH011 | Reliability | Warning | Baixo | Boa candidata a `warning` cedo. |
| ARCH012 | Reliability | Info | Alto | Pode ser inviavel em contratos públicos ou modelos legados. |
| ARCH013 | TestQuality | Info | Alto | Fortemente ligada a padrão de framework de mock. |
| ARCH014 | TestQuality | Info | Médio | Depende da convenção de asserções do time. |
| ARCH015 | Design | Warning | Médio | Depende de idioma, domínio e estilo de rotas. |
| ARCH016 | Performance | Warning | Médio | Especifica para fluxo de request ASP.NET. |
| ARCH017 | Reliability | Warning | Baixo | Boa candidata a `warning` em APIs. |
| ARCH018 | Reliability | Warning | Médio | Pode haver adaptadores ou código de infraestrutura legado. |
| ARCH019 | Security | Warning | Baixo | Boa candidata a `warning` ou `error`. |
| ARCH020 | Security | Warning | Médio | Exige política explícita de endpoints públicos. |
| ARCH021 | Performance | Warning | Médio | Depende do uso de EF Core e intenção de tracking. |
| ARCH022 | Performance | Warning | Médio | Pode exigir leitura de intenção da consulta. |
| ARCH023 | Testability | Warning | Médio | Pode afetar código simples, logging e adaptadores de tempo. |
| ARCH024 | Observability | Warning | Baixo | Boa candidata a `warning` cedo. |
| ARCH025 | Observability | Warning | Baixo | Boa candidata a `warning` cedo. |
| ARCH026 | Security | Warning | Baixo | Boa candidata a `warning`; `error` em APIs expostas. |
| ARCH027 | Architecture | Warning | Alto | Configure namespaces antes de promover. |
| ARCH028 | Design | Warning | Médio | Depende da política de imutabilidade para records. |
| ARCH029 | Design | Warning | Alto | Configure namespaces/base types de entidades antes de promover. |
| ARCH030 | Maintainability | Info | Médio | Requer AdditionalFiles de projetos para maior utilidade. |
| ARCH031 | Performance | Warning | Médio | Depende de target framework e política de migração para .NET 9+. |
| ARCH032 | Maintainability | Info | Médio | Requer AdditionalFiles de projetos para maior utilidade. |
| ARCH033 | Reliability | Warning | Baixo | Boa candidata a `warning` em configuração de DI. |

## Perfil recommended

Use como ponto de partida para projetos ativos. Ele mantém como `warning` regras objetivas de confiabilidade, segurança e observabilidade, mas reduz regras de design, arquitetura, testes e performance que costumam precisar de calibragem local.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.ARCH001.severity = warning
dotnet_diagnostic.ARCH002.severity = warning
dotnet_diagnostic.ARCH003.severity = suggestion
dotnet_diagnostic.ARCH004.severity = suggestion
dotnet_diagnostic.ARCH005.severity = suggestion
dotnet_diagnostic.ARCH006.severity = suggestion
dotnet_diagnostic.ARCH007.severity = info
dotnet_diagnostic.ARCH008.severity = info
dotnet_diagnostic.ARCH009.severity = warning
dotnet_diagnostic.ARCH010.severity = warning
dotnet_diagnostic.ARCH011.severity = warning
dotnet_diagnostic.ARCH012.severity = info
dotnet_diagnostic.ARCH013.severity = suggestion
dotnet_diagnostic.ARCH014.severity = suggestion
dotnet_diagnostic.ARCH015.severity = info
dotnet_diagnostic.ARCH016.severity = info
dotnet_diagnostic.ARCH017.severity = warning
dotnet_diagnostic.ARCH018.severity = warning
dotnet_diagnostic.ARCH019.severity = warning
dotnet_diagnostic.ARCH020.severity = warning
dotnet_diagnostic.ARCH021.severity = info
dotnet_diagnostic.ARCH022.severity = info
dotnet_diagnostic.ARCH023.severity = info
dotnet_diagnostic.ARCH024.severity = warning
dotnet_diagnostic.ARCH025.severity = warning
dotnet_diagnostic.ARCH026.severity = warning
dotnet_diagnostic.ARCH027.severity = info
dotnet_diagnostic.ARCH028.severity = info
dotnet_diagnostic.ARCH029.severity = info
dotnet_diagnostic.ARCH031.severity = info
dotnet_diagnostic.ARCH033.severity = warning

[*.csproj]
dotnet_diagnostic.ARCH030.severity = suggestion
dotnet_diagnostic.ARCH032.severity = suggestion
```

## Perfil strict

Use em bases novas, repositórios já saneados ou módulos em que o CI pode bloquear desvios. Este perfil eleva segurança e problemas async mais críticos para `error`, mantém a maior parte das regras como `warning` e deixa apenas convenções de teste muito opinativas em severidade menor.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.ARCH001.severity = error
dotnet_diagnostic.ARCH002.severity = warning
dotnet_diagnostic.ARCH003.severity = warning
dotnet_diagnostic.ARCH004.severity = info
dotnet_diagnostic.ARCH005.severity = warning
dotnet_diagnostic.ARCH006.severity = warning
dotnet_diagnostic.ARCH007.severity = warning
dotnet_diagnostic.ARCH008.severity = warning
dotnet_diagnostic.ARCH009.severity = error
dotnet_diagnostic.ARCH010.severity = error
dotnet_diagnostic.ARCH011.severity = warning
dotnet_diagnostic.ARCH012.severity = warning
dotnet_diagnostic.ARCH013.severity = info
dotnet_diagnostic.ARCH014.severity = warning
dotnet_diagnostic.ARCH015.severity = warning
dotnet_diagnostic.ARCH016.severity = warning
dotnet_diagnostic.ARCH017.severity = error
dotnet_diagnostic.ARCH018.severity = warning
dotnet_diagnostic.ARCH019.severity = error
dotnet_diagnostic.ARCH020.severity = error
dotnet_diagnostic.ARCH021.severity = warning
dotnet_diagnostic.ARCH022.severity = warning
dotnet_diagnostic.ARCH023.severity = warning
dotnet_diagnostic.ARCH024.severity = warning
dotnet_diagnostic.ARCH025.severity = warning
dotnet_diagnostic.ARCH026.severity = error
dotnet_diagnostic.ARCH027.severity = warning
dotnet_diagnostic.ARCH028.severity = warning
dotnet_diagnostic.ARCH029.severity = warning
dotnet_diagnostic.ARCH031.severity = warning
dotnet_diagnostic.ARCH033.severity = warning

[*.csproj]
dotnet_diagnostic.ARCH030.severity = warning
dotnet_diagnostic.ARCH032.severity = warning
```

Antes de aplicar `strict` em um repositório inteiro, valide as regras ARCH027, ARCH029, ARCH030, ARCH031 e ARCH032 com as opções documentadas nas páginas das regras. Elas dependem mais do formato da solução, target frameworks e convenções locais.

## Perfil security

Use quando a prioridade é reduzir risco em endpoints HTTP, autorização e CORS. Regras adjacentes ficam em `info` para ajudar revisões sem ampliar demais o escopo.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.ARCH017.severity = info
dotnet_diagnostic.ARCH018.severity = info
dotnet_diagnostic.ARCH019.severity = warning
dotnet_diagnostic.ARCH020.severity = warning
dotnet_diagnostic.ARCH024.severity = info
dotnet_diagnostic.ARCH025.severity = info
dotnet_diagnostic.ARCH026.severity = warning
dotnet_diagnostic.ARCH027.severity = info
dotnet_diagnostic.ARCH033.severity = info
```

Para APIs expostas externamente, depois da triagem inicial, considere promover ARCH019, ARCH020 e ARCH026 para `error`.

## Perfil architecture

Use em soluções com separação clara entre domínio, aplicação e infraestrutura. Configure ARCH027 e ARCH029 para os namespaces reais do projeto antes de elevar para `warning` em uma base existente.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.ARCH012.severity = info
dotnet_diagnostic.ARCH015.severity = warning
dotnet_diagnostic.ARCH018.severity = info
dotnet_diagnostic.ARCH023.severity = info
dotnet_diagnostic.ARCH024.severity = info
dotnet_diagnostic.ARCH025.severity = info
dotnet_diagnostic.ARCH027.severity = warning
dotnet_diagnostic.ARCH028.severity = warning
dotnet_diagnostic.ARCH029.severity = warning
dotnet_diagnostic.ARCH033.severity = warning

# Ajuste para os namespaces reais da solução.
dotnet_diagnostic.ARCH027.core_namespace_patterns = "*.Domain;*.Application"
dotnet_diagnostic.ARCH027.forbidden_namespace_patterns = "Microsoft.EntityFrameworkCore;Microsoft.AspNetCore;StackExchange.Redis;Npgsql"
dotnet_diagnostic.ARCH027.allowed_namespace_patterns =
dotnet_diagnostic.ARCH027.ignore_tests = true

dotnet_diagnostic.ARCH029.entity_namespaces = ["MyApp.Domain.Entities", "MyApp.Domain.Aggregates"]
dotnet_diagnostic.ARCH029.entity_base_types = ["Entity", "AggregateRoot"]
dotnet_diagnostic.ARCH029.allow_internal_setters = false

[*.csproj]
dotnet_diagnostic.ARCH030.severity = info
dotnet_diagnostic.ARCH032.severity = info
```

## Perfil testing

Use para padronizar testes, mocks e asserções. O perfil deixa a convenção `_sut` como `info` porque ela tende a refletir preferência local de nome, mas promove regras relacionadas a qualidade de asserção e mocks.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.ARCH003.severity = warning
dotnet_diagnostic.ARCH004.severity = info
dotnet_diagnostic.ARCH005.severity = warning
dotnet_diagnostic.ARCH006.severity = warning
dotnet_diagnostic.ARCH013.severity = warning
dotnet_diagnostic.ARCH014.severity = warning
dotnet_diagnostic.ARCH023.severity = info
```

Se o repositório usa outro framework de mock por decisão explícita, mantenha ARCH013 como `info`, `suggestion` ou `none` até a migração ser aprovada.

## Perfil legacy-safe

Use para o primeiro ciclo em bases grandes ou antigas. Ele evita `warning` e `error`, reduz regras de convenção para `silent` e mantém riscos operacionais como `info` para inventário.

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.ARCH001.severity = info
dotnet_diagnostic.ARCH002.severity = info
dotnet_diagnostic.ARCH003.severity = silent
dotnet_diagnostic.ARCH004.severity = silent
dotnet_diagnostic.ARCH005.severity = silent
dotnet_diagnostic.ARCH006.severity = silent
dotnet_diagnostic.ARCH007.severity = silent
dotnet_diagnostic.ARCH008.severity = info
dotnet_diagnostic.ARCH009.severity = info
dotnet_diagnostic.ARCH010.severity = info
dotnet_diagnostic.ARCH011.severity = info
dotnet_diagnostic.ARCH012.severity = silent
dotnet_diagnostic.ARCH013.severity = silent
dotnet_diagnostic.ARCH014.severity = silent
dotnet_diagnostic.ARCH015.severity = silent
dotnet_diagnostic.ARCH016.severity = info
dotnet_diagnostic.ARCH017.severity = info
dotnet_diagnostic.ARCH018.severity = info
dotnet_diagnostic.ARCH019.severity = info
dotnet_diagnostic.ARCH020.severity = info
dotnet_diagnostic.ARCH021.severity = silent
dotnet_diagnostic.ARCH022.severity = silent
dotnet_diagnostic.ARCH023.severity = silent
dotnet_diagnostic.ARCH024.severity = info
dotnet_diagnostic.ARCH025.severity = info
dotnet_diagnostic.ARCH026.severity = info
dotnet_diagnostic.ARCH027.severity = silent
dotnet_diagnostic.ARCH028.severity = silent
dotnet_diagnostic.ARCH029.severity = silent
dotnet_diagnostic.ARCH031.severity = silent
dotnet_diagnostic.ARCH033.severity = info

[*.csproj]
dotnet_diagnostic.ARCH030.severity = silent
dotnet_diagnostic.ARCH032.severity = silent
```

Depois do inventário, promova poucas regras por vez. Um caminho comum e mover ARCH001, ARCH009, ARCH019, ARCH020 e ARCH026 de `info` para `warning` em código novo, mantendo pastas legadas com severidade menor.

## Escopo por pasta

Os perfis podem ser combinados com escopos de `.editorconfig`. Exemplo: política recomendada no repositório inteiro, mas legado apenas informativo.

```ini
root = true

[*.cs]
dotnet_diagnostic.ARCH001.severity = warning
dotnet_diagnostic.ARCH009.severity = warning
dotnet_diagnostic.ARCH020.severity = warning
dotnet_diagnostic.ARCH026.severity = warning

[src/Legacy/**/*.cs]
dotnet_diagnostic.ARCH001.severity = info
dotnet_diagnostic.ARCH009.severity = info
dotnet_diagnostic.ARCH020.severity = info
dotnet_diagnostic.ARCH026.severity = info
```

Prefira escopo por pasta a `NoWarn` quando a regra continua útil para código novo. Use `none` somente quando uma regra não se aplica ao projeto ou quando há uma decisão documentada de não adotá-la.
