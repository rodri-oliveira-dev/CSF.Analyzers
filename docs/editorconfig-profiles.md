# Perfis de adocao via .editorconfig

Este guia oferece perfis prontos para adotar o `Swa.Analyzers` sem decidir a severidade regra por regra no primeiro dia. Os exemplos usam apenas severidades de `.editorconfig`; eles nao alteram a severidade padrao implementada no pacote.

Use os perfis como ponto de partida. Projetos legados, bibliotecas publicas, APIs com contratos externos e bases com convencoes ja consolidadas devem ajustar o escopo por pasta ou por projeto.

## Como escolher

| Perfil | Quando usar | Custo esperado |
| ------ | ----------- | -------------- |
| `recommended` | Politica inicial para projetos ativos que querem valor sem bloqueio agressivo. | Medio. Regras mais objetivas aparecem como `warning`; regras opinativas ficam mais baixas. |
| `strict` | Bases novas ou ja saneadas, com CI preparado para bloquear desvios relevantes. | Alto. Pode exigir correcoes antes de merge e suppressions justificadas. |
| `security` | APIs, servicos HTTP e componentes expostos que precisam priorizar autorizacao e CORS. | Medio. Foca seguranca e mantem regras adjacentes visiveis. |
| `architecture` | Solucoes com camadas bem definidas, dominio rico ou convencoes de design de API. | Medio a alto. Depende do alinhamento entre namespaces reais e convencoes do time. |
| `testing` | Repositorios que querem padronizar testes, assercoes e mocks. | Baixo a medio. Costuma exigir ajustes em testes existentes. |
| `legacy-safe` | Primeira execucao em bases grandes ou legadas, sem risco de quebrar build. | Baixo. Mantem problemas visiveis sem impor bloqueio. |

## Mapa das regras

O risco de ruido abaixo e uma recomendacao pratica para adocao. Ele considera o quanto a regra depende de convencoes locais, stubs, arquitetura do projeto ou migracao de legado; nao muda o comportamento do analyzer.

| ID | Categoria | Severidade padrao | Risco de ruido | Observacao para perfis |
| -- | --------- | ----------------- | -------------- | ---------------------- |
| ARCH001 | Reliability | Warning | Baixo | Boa candidata a `warning` cedo. |
| ARCH002 | Reliability | Warning | Medio | Pode existir em codigo legado com callbacks intencionais. |
| ARCH003 | TestQuality | Info | Medio | Opinativa para estilo de assercao em testes. |
| ARCH004 | TestQuality | Info | Alto | Convencao de nome; melhor promover por time. |
| ARCH005 | TestQuality | Info | Medio | Depende do padrao de mocks adotado. |
| ARCH006 | TestQuality | Info | Medio | Pode apontar excecoes aceitaveis em testes de contrato. |
| ARCH007 | Performance | Info | Medio | Boa visibilidade, mas nem todo loop e critico. |
| ARCH008 | Reliability | Info | Baixo | Geralmente objetiva em sinks de filesystem. |
| ARCH009 | Reliability | Warning | Baixo | Boa candidata a `warning` ou `error` apos triagem. |
| ARCH010 | Reliability | Warning | Medio | Pode exigir desenho consistente de cancelamento. |
| ARCH011 | Reliability | Warning | Baixo | Boa candidata a `warning` cedo. |
| ARCH012 | Reliability | Info | Alto | Pode ser inviavel em contratos publicos ou modelos legados. |
| ARCH013 | TestQuality | Info | Alto | Fortemente ligada a padrao de framework de mock. |
| ARCH014 | TestQuality | Info | Medio | Depende da convencao de assercoes do time. |
| ARCH015 | Design | Warning | Medio | Depende de idioma, dominio e estilo de rotas. |
| ARCH016 | Performance | Warning | Medio | Especifica para fluxo de request ASP.NET. |
| ARCH017 | Reliability | Warning | Baixo | Boa candidata a `warning` em APIs. |
| ARCH018 | Reliability | Warning | Medio | Pode haver adaptadores ou codigo de infraestrutura legado. |
| ARCH019 | Security | Warning | Baixo | Boa candidata a `warning` ou `error`. |
| ARCH020 | Security | Warning | Medio | Exige politica explicita de endpoints publicos. |
| ARCH021 | Performance | Warning | Medio | Depende do uso de EF Core e intencao de tracking. |
| ARCH022 | Performance | Warning | Medio | Pode exigir leitura de intencao da consulta. |
| ARCH023 | Testability | Warning | Medio | Pode afetar codigo simples, logging e adaptadores de tempo. |
| ARCH024 | Observability | Warning | Baixo | Boa candidata a `warning` cedo. |
| ARCH025 | Observability | Warning | Baixo | Boa candidata a `warning` cedo. |
| ARCH026 | Security | Warning | Baixo | Boa candidata a `warning`; `error` em APIs expostas. |
| ARCH027 | Architecture | Warning | Alto | Configure namespaces antes de promover. |
| ARCH028 | Design | Warning | Medio | Depende da politica de imutabilidade para records. |
| ARCH029 | Design | Warning | Alto | Configure namespaces/base types de entidades antes de promover. |
| ARCH030 | Maintainability | Info | Medio | Requer AdditionalFiles de projetos para maior utilidade. |
| ARCH031 | Performance | Warning | Medio | Depende de target framework e politica de migracao para .NET 9+. |
| ARCH032 | Maintainability | Info | Medio | Requer AdditionalFiles de projetos para maior utilidade. |
| ARCH033 | Reliability | Warning | Baixo | Boa candidata a `warning` em configuracao de DI. |

## Perfil recommended

Use como ponto de partida para projetos ativos. Ele mantem como `warning` regras objetivas de confiabilidade, seguranca e observabilidade, mas reduz regras de design, arquitetura, testes e performance que costumam precisar de calibragem local.

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

Use em bases novas, repositorios ja saneados ou modulos em que o CI pode bloquear desvios. Este perfil eleva seguranca e problemas async mais criticos para `error`, mantem a maior parte das regras como `warning` e deixa apenas convencoes de teste muito opinativas em severidade menor.

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

Antes de aplicar `strict` em um repositorio inteiro, valide as regras ARCH027, ARCH029, ARCH030, ARCH031 e ARCH032 com as opcoes documentadas nas paginas das regras. Elas dependem mais do formato da solucao, target frameworks e convencoes locais.

## Perfil security

Use quando a prioridade e reduzir risco em endpoints HTTP, autorizacao e CORS. Regras adjacentes ficam em `info` para ajudar revisoes sem ampliar demais o escopo.

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

Use em solucoes com separacao clara entre dominio, aplicacao e infraestrutura. Configure ARCH027 e ARCH029 para os namespaces reais do projeto antes de elevar para `warning` em uma base existente.

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

# Ajuste para os namespaces reais da solucao.
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

Use para padronizar testes, mocks e assercoes. O perfil deixa a convencao `_sut` como `info` porque ela tende a refletir preferencia local de nome, mas promove regras relacionadas a qualidade de assercao e mocks.

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

Se o repositorio usa outro framework de mock por decisao explicita, mantenha ARCH013 como `info`, `suggestion` ou `none` ate a migracao ser aprovada.

## Perfil legacy-safe

Use para o primeiro ciclo em bases grandes ou antigas. Ele evita `warning` e `error`, reduz regras de convencao para `silent` e mantem riscos operacionais como `info` para inventario.

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

Depois do inventario, promova poucas regras por vez. Um caminho comum e mover ARCH001, ARCH009, ARCH019, ARCH020 e ARCH026 de `info` para `warning` em codigo novo, mantendo pastas legadas com severidade menor.

## Escopo por pasta

Os perfis podem ser combinados com escopos de `.editorconfig`. Exemplo: politica recomendada no repositorio inteiro, mas legado apenas informativo.

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

Prefira escopo por pasta a `NoWarn` quando a regra continua util para codigo novo. Use `none` somente quando uma regra nao se aplica ao projeto ou quando ha uma decisao documentada de nao adota-la.
