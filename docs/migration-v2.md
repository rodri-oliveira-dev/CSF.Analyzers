# Migracao para a v2

## Aviso de breaking change

A versao 2.0 renumera os diagnosticos ativos e divide o pacote unico em tres pacotes NuGet independentes. IDs antigos `ARCH###` nao sao emitidos pela implementacao v2.

Consumidores devem atualizar referencias de pacote, `.editorconfig`, suppressions, `NoWarn`, baselines e documentacao interna para os novos IDs.

## Pacotes de destino

| Pacote | IDs ativos na v2 |
| ------ | ---------------- |
| `Swa.Analyzers.Reliability` | `REL001`, `REL002`, `REL003`, `REL004` |
| `Swa.Analyzers.Architecture` | `ARC001`, `ARC002`, `ARC003`, `ARC004`, `ARC005` |
| `Swa.Analyzers.Testing` | `TST001`, `TST002` |

Nao ha metapacote `Swa.Analyzers` na v2 inicial.

## Mapeamento de IDs mantidos

| ID v1 | ID v2 | Pacote v2 | Estado padrao v2 |
| ----- | ----- | --------- | ---------------- |
| `ARCH016` | `REL001` | `Swa.Analyzers.Reliability` | habilitada, warning |
| `ARCH017` | `REL002` | `Swa.Analyzers.Reliability` | habilitada, warning |
| `ARCH021` | `REL003` | `Swa.Analyzers.Reliability` | opt-in, info |
| `ARCH022` | `REL004` | `Swa.Analyzers.Reliability` | habilitada, warning |
| `ARCH020` | `ARC001` | `Swa.Analyzers.Architecture` | habilitada, warning |
| `ARCH027` | `ARC002` | `Swa.Analyzers.Architecture` | habilitada, warning |
| `ARCH015` | `ARC003` | `Swa.Analyzers.Architecture` | opt-in, info |
| `ARCH029` | `ARC004` | `Swa.Analyzers.Architecture` | opt-in, info |
| `ARCH032` | `ARC005` | `Swa.Analyzers.Architecture` | opt-in, info |
| `ARCH005` | `TST001` | `Swa.Analyzers.Testing` | opt-in, info |
| `ARCH006` | `TST002` | `Swa.Analyzers.Testing` | opt-in, info |

## Regras removidas

As regras abaixo nao possuem analyzer ativo na v2:

| ID v1 | Destino |
| ----- | ------- |
| `ARCH001` | removida |
| `ARCH002` | removida |
| `ARCH003` | removida |
| `ARCH004` | removida |
| `ARCH007` | removida |
| `ARCH008` | removida |
| `ARCH009` | removida |
| `ARCH010` | removida |
| `ARCH011` | removida |
| `ARCH012` | removida |
| `ARCH013` | removida |
| `ARCH014` | removida |
| `ARCH018` | removida |
| `ARCH019` | removida |
| `ARCH023` | removida |
| `ARCH024` | removida |
| `ARCH025` | removida |
| `ARCH026` | removida |
| `ARCH028` | removida |
| `ARCH030` | removida |
| `ARCH031` | removida |
| `ARCH033` | removida |

## Mudancas de `.editorconfig`

Atualize a parte do identificador do diagnostico e preserve o nome especifico da opcao.

| Chave v1 | Chave v2 |
| -------- | -------- |
| `dotnet_diagnostic.ARCH015.route_language` | `dotnet_diagnostic.ARC003.route_language` |
| `dotnet_diagnostic.ARCH015.additional_verbs` | `dotnet_diagnostic.ARC003.additional_verbs` |
| `dotnet_diagnostic.ARCH020.allowed_routes` | `dotnet_diagnostic.ARC001.allowed_routes` |
| `dotnet_diagnostic.ARCH020.allowed_methods` | `dotnet_diagnostic.ARC001.allowed_methods` |
| `dotnet_diagnostic.ARCH020.ignored_namespaces` | `dotnet_diagnostic.ARC001.ignored_namespaces` |
| `dotnet_diagnostic.ARCH027.core_namespace_patterns` | `dotnet_diagnostic.ARC002.core_namespace_patterns` |
| `dotnet_diagnostic.ARCH027.forbidden_namespace_patterns` | `dotnet_diagnostic.ARC002.forbidden_namespace_patterns` |
| `dotnet_diagnostic.ARCH027.allowed_namespace_patterns` | `dotnet_diagnostic.ARC002.allowed_namespace_patterns` |
| `dotnet_diagnostic.ARCH027.ignore_tests` | `dotnet_diagnostic.ARC002.ignore_tests` |
| `dotnet_diagnostic.ARCH029.entity_namespaces` | `dotnet_diagnostic.ARC004.entity_namespaces` |
| `dotnet_diagnostic.ARCH029.entity_base_types` | `dotnet_diagnostic.ARC004.entity_base_types` |
| `dotnet_diagnostic.ARCH029.allow_internal_setters` | `dotnet_diagnostic.ARC004.allow_internal_setters` |
| `dotnet_diagnostic.ARCH032.ignored_properties` | `dotnet_diagnostic.ARC005.ignored_properties` |
| `dotnet_diagnostic.ARCH032.compare_values` | `dotnet_diagnostic.ARC005.compare_values` |

Exemplos:

```ini
[*.cs]
dotnet_diagnostic.REL001.severity = warning
dotnet_diagnostic.ARC002.core_namespace_patterns = "*.Domain;*.Application"
dotnet_diagnostic.TST001.severity = info
```

Regras opt-in precisam de severidade explicita para emitir diagnosticos:

```ini
[*.cs]
dotnet_diagnostic.REL003.severity = info
dotnet_diagnostic.ARC003.severity = info
dotnet_diagnostic.ARC004.severity = info
dotnet_diagnostic.TST001.severity = info
```

## Suppressions, NoWarn e baselines

Suppressions em codigo, `GlobalSuppressions.cs`, `NoWarn`, baselines de CI e arquivos SARIF antigos que referenciam `ARCH###` nao afetam os novos diagnosticos. Migre cada suppression para o novo ID correspondente quando a regra foi mantida.

Para regras removidas, remova suppressions e entradas `NoWarn` antigas depois de confirmar que nenhum pacote v2 emite o ID removido.

Baselines gerados por ferramenta devem ser recriados ou transformados com a tabela de mapeamento acima. Um baseline com IDs antigos nao suprime `REL###`, `ARC###` ou `TST###`.
