# CSF rename - tests and samples

## Estado inicial

- Worktree limpo antes da implementacao.
- A etapa anterior concluiu o rename da solucao e dos projetos de producao para `CSF.Analyzers.*`.
- A solucao principal e `CSF.Analyzers.slnx`, mas ainda referencia projetos em `tests/Swa.Analyzers.*` e `samples/Swa.Analyzers.*`.
- Os testes ainda usam namespaces proprios `Swa.Analyzers.Tests.*` e `Swa.Analyzers.PackageValidation.Tests`.
- Os samples ainda usam namespaces `Swa.Analyzers.SampleApp.*` e mensagens de console `Swa.Analyzers.* sample`.
- `InternalsVisibleTo` em `src/CSF.Analyzers.Common/Properties/AssemblyInfo.cs` ainda aponta para assemblies de teste `Swa.Analyzers.*`.
- `PackageId`, repository URLs e help links continuam com `Swa.Analyzers` por decisao da etapa anterior e permanecem fora desta etapa.

## Projetos afetados

- `tests/Swa.Analyzers.Reliability.Tests`
- `tests/Swa.Analyzers.Architecture.Tests`
- `tests/Swa.Analyzers.Testing.Tests`
- `tests/Swa.Analyzers.PackageValidation.Tests`
- `tests/Swa.Analyzers.TestSupport`
- `samples/Swa.Analyzers.Reliability.Sample`
- `samples/Swa.Analyzers.Architecture.Sample`
- `samples/Swa.Analyzers.Testing.Sample`

## Diretorios e arquivos a renomear

| Origem | Destino |
| ------ | ------- |
| `tests/Swa.Analyzers.Reliability.Tests` | `tests/CSF.Analyzers.Reliability.Tests` |
| `tests/Swa.Analyzers.Architecture.Tests` | `tests/CSF.Analyzers.Architecture.Tests` |
| `tests/Swa.Analyzers.Testing.Tests` | `tests/CSF.Analyzers.Testing.Tests` |
| `tests/Swa.Analyzers.PackageValidation.Tests` | `tests/CSF.Analyzers.PackageValidation.Tests` |
| `tests/Swa.Analyzers.TestSupport` | `tests/CSF.Analyzers.TestSupport` |
| `samples/Swa.Analyzers.Reliability.Sample` | `samples/CSF.Analyzers.Reliability.Sample` |
| `samples/Swa.Analyzers.Architecture.Sample` | `samples/CSF.Analyzers.Architecture.Sample` |
| `samples/Swa.Analyzers.Testing.Sample` | `samples/CSF.Analyzers.Testing.Sample` |
| `tests/Swa.Analyzers.Reliability.Tests/Swa.Analyzers.Reliability.Tests.csproj` | `tests/CSF.Analyzers.Reliability.Tests/CSF.Analyzers.Reliability.Tests.csproj` |
| `tests/Swa.Analyzers.Architecture.Tests/Swa.Analyzers.Architecture.Tests.csproj` | `tests/CSF.Analyzers.Architecture.Tests/CSF.Analyzers.Architecture.Tests.csproj` |
| `tests/Swa.Analyzers.Testing.Tests/Swa.Analyzers.Testing.Tests.csproj` | `tests/CSF.Analyzers.Testing.Tests/CSF.Analyzers.Testing.Tests.csproj` |
| `tests/Swa.Analyzers.PackageValidation.Tests/Swa.Analyzers.PackageValidation.Tests.csproj` | `tests/CSF.Analyzers.PackageValidation.Tests/CSF.Analyzers.PackageValidation.Tests.csproj` |
| `samples/Swa.Analyzers.Reliability.Sample/Swa.Analyzers.Reliability.Sample.csproj` | `samples/CSF.Analyzers.Reliability.Sample/CSF.Analyzers.Reliability.Sample.csproj` |
| `samples/Swa.Analyzers.Architecture.Sample/Swa.Analyzers.Architecture.Sample.csproj` | `samples/CSF.Analyzers.Architecture.Sample/CSF.Analyzers.Architecture.Sample.csproj` |
| `samples/Swa.Analyzers.Testing.Sample/Swa.Analyzers.Testing.Sample.csproj` | `samples/CSF.Analyzers.Testing.Sample/CSF.Analyzers.Testing.Sample.csproj` |

## Referencias cruzadas

- `CSF.Analyzers.slnx` deve trocar todos os paths de tests e samples para `CSF.Analyzers.*`.
- Os `.csproj` de testes devem atualizar `Compile Include="..\Swa.Analyzers.TestSupport\Verifier.cs"` para `..\CSF.Analyzers.TestSupport\Verifier.cs`.
- `ProjectReference` para os projetos de producao ja apontam para `src/CSF.Analyzers.*` e devem ser preservados.
- `ProjectReference` dos samples deve continuar usando `OutputItemType="Analyzer"` e `ReferenceOutputAssembly="false"`.
- `packages.lock.json` pode continuar contendo chaves lowercase `swa.analyzers.*` enquanto `PackageId` publico permanecer temporariamente antigo.
- `AnalyzerPackageIsolationTests` deve manter as strings de `PackageId` e repository/help URLs antigas, pois package identity publica e URLs estao fora do escopo desta etapa.
- `AGENTS.md` e as skills locais devem trocar referencias operacionais de tests, samples e comandos para os novos paths.
- O workspace rastreado deve deixar de exibir nome antigo do produto se a referencia for operacional e nao historica.

## InternalsVisibleTo

`src/CSF.Analyzers.Common/Properties/AssemblyInfo.cs` deve trocar:

- `Swa.Analyzers.Tests` para `CSF.Analyzers.Tests`
- `Swa.Analyzers.Reliability.Tests` para `CSF.Analyzers.Reliability.Tests`
- `Swa.Analyzers.Architecture.Tests` para `CSF.Analyzers.Architecture.Tests`
- `Swa.Analyzers.Testing.Tests` para `CSF.Analyzers.Testing.Tests`

Nao ha necessidade de adicionar `InternalsVisibleTo` para package validation, pois esse projeto nao acessa internals compartilhados diretamente.

## Namespaces

- Test support deve usar `CSF.Analyzers.Tests`.
- Testes de regras, performance e framework references devem usar `CSF.Analyzers.Tests.*`.
- Package validation deve usar `CSF.Analyzers.PackageValidation.Tests`.
- Samples devem usar `CSF.Analyzers.SampleApp.*`.
- Usings para analyzers de producao ja devem permanecer `CSF.Analyzers.*`.

## Riscos

- A solucao pode ficar inconsistente se um path antigo permanecer em `CSF.Analyzers.slnx`.
- Os testes podem perder acesso a membros internos se `InternalsVisibleTo` e assembly names de teste nao forem alterados juntos.
- Includes de `Verifier.cs` podem quebrar se `TestSupport` for renomeado sem atualizar os `.csproj`.
- Renomeacao mecanica pode alterar strings de PackageId ou URLs que estao fora do escopo.
- Lock files podem manter nomes antigos por causa dos PackageIds temporarios, o que e esperado nesta etapa.
- Samples invalidos podem continuar emitindo warnings de analyzer durante build, o que e comportamento existente.

## Criterios de aceite

- Todos os diretorios e `.csproj` de `tests/Swa.Analyzers.*` e `samples/Swa.Analyzers.*` foram renomeados para `CSF.Analyzers.*`.
- `CSF.Analyzers.slnx` nao referencia paths antigos de tests ou samples.
- Namespaces proprios de tests e samples usam `CSF.Analyzers.*`.
- `InternalsVisibleTo` aponta para assemblies de teste `CSF.Analyzers.*`.
- `Compile Include` para `Verifier.cs` aponta para `tests/CSF.Analyzers.TestSupport`.
- `ProjectReference` continua apontando para `src/CSF.Analyzers.*`.
- IDs `REL###`, `ARC###` e `TST###` permanecem inalterados.
- PackageIds e URLs publicas antigas permanecem apenas como excecoes temporarias desta etapa.
- `AGENTS.md` e skills locais nao orientam novos trabalhos para paths de tests/samples antigos.

## Validacoes

```powershell
dotnet restore ./CSF.Analyzers.slnx
dotnet build ./CSF.Analyzers.slnx --configuration Release --no-restore
dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1
dotnet sln ./CSF.Analyzers.slnx list
rg -n "tests[/\\]Swa\.Analyzers|samples[/\\]Swa\.Analyzers|Swa\.Analyzers\.(Reliability|Architecture|Testing|PackageValidation)\.Tests\.csproj|Swa\.Analyzers\.(Reliability|Architecture|Testing)\.Sample\.csproj" CSF.Analyzers.slnx tests samples AGENTS.md .agents/skills Swa.Analyzers.code-workspace
rg -n "InternalsVisibleTo\(\"Swa\.Analyzers|namespace Swa\.Analyzers\.(Tests|PackageValidation|SampleApp)" src tests samples --glob "!**/bin/**" --glob "!**/obj/**"
```

## Evidencias de validacao

Executado em 2026-07-23 durante a validacao acumulada das etapas 3 e 4:

| Comando | Resultado |
| ------- | --------- |
| `dotnet restore ./CSF.Analyzers.slnx --locked-mode` | Aprovado. |
| `dotnet build ./CSF.Analyzers.slnx --configuration Release --no-restore` | Aprovado; warnings esperados dos samples invalidos e `EnableGenerateDocumentationFile`. |
| `dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1` | Aprovado; 246 testes, 0 falhas. |

## Resultado final

- Todos os diretorios e `.csproj` de tests e samples foram renomeados para `CSF.Analyzers.*`.
- `CSF.Analyzers.slnx` referencia os paths novos de tests e samples.
- Namespaces proprios de tests e samples usam `CSF.Analyzers.*`.
- `InternalsVisibleTo` aponta para assemblies de teste `CSF.Analyzers.*`.
- Includes de `Verifier.cs` apontam para `tests/CSF.Analyzers.TestSupport`.
- IDs `REL###`, `ARC###` e `TST###` permaneceram inalterados.
