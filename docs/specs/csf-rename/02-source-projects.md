# CSF rename - source projects

## Estado inicial

- Worktree limpo antes da implementacao.
- A etapa anterior concluida e apenas documental: `docs/specs/csf-rename/plan.md` foi criada no commit `docs: specify CSF rename migration`.
- A solucao principal ainda e `Swa.Analyzers.slnx`.
- Os projetos de producao ainda estao em:
  - `src/Swa.Analyzers.Common`
  - `src/Swa.Analyzers.Reliability`
  - `src/Swa.Analyzers.Architecture`
  - `src/Swa.Analyzers.Testing`
- Os projetos de testes e samples ainda estao com nomes `Swa.Analyzers.*` e nao serao renomeados nesta etapa.
- `Directory.Build.props` usa a propriedade interna `IsSwaAnalyzerPackage` e inclui shared source por `src\Swa.Analyzers.Common\**\*.cs`.
- `Directory.Build.targets` instala hooks apenas quando `$(MSBuildProjectName)` e `Swa.Analyzers.Reliability`.
- `src/Swa.Analyzers.Architecture/buildTransitive/Swa.Analyzers.Architecture.targets` define propriedades MSBuild com prefixo `SwaAnalyzers`.
- `PackageId`, `RepositoryUrl`, `PackageProjectUrl` e links de help ainda apontam para `Swa.Analyzers`.

## Arquivos afetados

- `Swa.Analyzers.slnx`
- `CSF.Analyzers.slnx`
- `Directory.Build.props`
- `Directory.Build.targets`
- `Swa.Analyzers.code-workspace`
- `AGENTS.md`
- `docs/specs/csf-rename/plan.md`
- `docs/specs/csf-rename/02-source-projects.md`
- `src/Swa.Analyzers.Common/**`
- `src/Swa.Analyzers.Reliability/**`
- `src/Swa.Analyzers.Architecture/**`
- `src/Swa.Analyzers.Testing/**`
- `src/CSF.Analyzers.Common/**`
- `src/CSF.Analyzers.Reliability/**`
- `src/CSF.Analyzers.Architecture/**`
- `src/CSF.Analyzers.Testing/**`
- `tests/Swa.Analyzers.*.Tests/*.csproj`
- `tests/Swa.Analyzers.PackageValidation.Tests/*.csproj`
- `samples/Swa.Analyzers.*.Sample/*.csproj`
- Arquivos `.cs` de testes que importam namespaces de analyzers de producao.

## Renames planejados

| Origem | Destino |
| ------ | ------- |
| `Swa.Analyzers.slnx` | `CSF.Analyzers.slnx` |
| `src/Swa.Analyzers.Common` | `src/CSF.Analyzers.Common` |
| `src/Swa.Analyzers.Reliability` | `src/CSF.Analyzers.Reliability` |
| `src/Swa.Analyzers.Architecture` | `src/CSF.Analyzers.Architecture` |
| `src/Swa.Analyzers.Testing` | `src/CSF.Analyzers.Testing` |
| `src/Swa.Analyzers.Reliability/Swa.Analyzers.Reliability.csproj` | `src/CSF.Analyzers.Reliability/CSF.Analyzers.Reliability.csproj` |
| `src/Swa.Analyzers.Architecture/Swa.Analyzers.Architecture.csproj` | `src/CSF.Analyzers.Architecture/CSF.Analyzers.Architecture.csproj` |
| `src/Swa.Analyzers.Testing/Swa.Analyzers.Testing.csproj` | `src/CSF.Analyzers.Testing/CSF.Analyzers.Testing.csproj` |
| `src/Swa.Analyzers.Architecture/buildTransitive/Swa.Analyzers.Architecture.targets` | `src/CSF.Analyzers.Architecture/buildTransitive/CSF.Analyzers.Architecture.targets` |

`src/Swa.Analyzers.Common` nao possui `.csproj`; o rename e apenas do diretorio de shared source.

## Referencias atomicas

- A `.slnx` deve apontar para os novos paths de `src/CSF.Analyzers.*` e manter todos os projetos de tests e samples existentes.
- `ProjectReference` em tests, samples e package validation deve apontar para os novos `.csproj` de producao.
- Namespaces e usings de producao devem trocar `Swa.Analyzers.*` por `CSF.Analyzers.*`.
- Usings de testes que referenciam analyzers de producao devem trocar para `CSF.Analyzers.*`.
- `Directory.Build.props` deve reconhecer os novos nomes de projeto e incluir shared source de `src\CSF.Analyzers.Common\**\*.cs`.
- A propriedade interna `IsSwaAnalyzerPackage` deve virar `IsCsfAnalyzerPackage`.
- `Directory.Build.targets` deve instalar hooks pelo novo `$(MSBuildProjectName)` de Reliability.
- O target `buildTransitive` de Architecture deve trocar o nome de arquivo para `CSF.Analyzers.Architecture.targets` e propriedades internas para prefixo `CsfAnalyzers`.
- `AGENTS.md` deve acompanhar imediatamente o novo nome da solucao e dos projetos de source, mantendo tests e samples com nome antigo nesta etapa.

## Fora desta etapa

- Nao renomear `PackageId` publico dos pacotes NuGet.
- Nao alterar `RepositoryUrl`, `PackageProjectUrl` ou URLs do GitHub.
- Nao renomear diretorios ou `.csproj` de tests e samples.
- Nao fazer revisao geral de README, docs publicas, scripts de release ou workflows.
- Nao alterar comportamento dos analyzers.

## Riscos

- O build pode falhar se algum `ProjectReference`, include de shared source ou path da `.slnx` ficar com nome antigo.
- A troca de assembly name pode exigir ajustes em testes que inspecionam nomes de assemblies.
- A manutencao temporaria dos `PackageId` antigos pode parecer inconsistente com os nomes de assembly, mas isola a mudanca publica de NuGet para etapa futura.
- `InternalsVisibleTo` precisa continuar apontando para assemblies de teste ainda nao renomeados.
- Arquivos ignorados em `bin`, `obj` e `artifacts` podem manter nomes antigos e nao devem guiar a validacao.

## Criterios de aceite

- `CSF.Analyzers.slnx` existe e `Swa.Analyzers.slnx` nao existe mais como arquivo rastreado.
- A solucao inclui todos os projetos existentes.
- Os projetos de producao e o shared source estao em `src/CSF.Analyzers.*`.
- Os `.csproj` de producao foram renomeados para `CSF.Analyzers.*.csproj`.
- Namespaces/usings de producao e usings de testes para analyzers apontam para `CSF.Analyzers.*`.
- `PackageId` permanece `Swa.Analyzers.*` como excecao temporaria deliberada.
- `InternalsVisibleTo` para os assemblies de testes `Swa.Analyzers.*.Tests` permanece como excecao temporaria deliberada.
- Restore, build e test passam nos comandos definidos nesta spec.
- `git status` mostra exatamente as alteracoes desta etapa antes do commit.

## Comandos de validacao

```powershell
dotnet restore ./CSF.Analyzers.slnx
dotnet build ./CSF.Analyzers.slnx --configuration Release --no-restore
dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1
dotnet sln ./CSF.Analyzers.slnx list
rg -n "src\\\\Swa\\.Analyzers|src/Swa\\.Analyzers|Swa\\.Analyzers\\.slnx|IsSwaAnalyzerPackage|SwaAnalyzersArchitectureDirectoryBuildProps" --glob "!bin/**" --glob "!obj/**" --glob "!artifacts/**"
```

## Decisoes tomadas

- A identidade de compilacao sera renomeada para `CSF.Analyzers.*`.
- A identidade NuGet publica ficou temporariamente como `Swa.Analyzers.*`.
- As URLs publicas do repositorio ficaram inalteradas.
- Tests e samples mantiveram diretorios, nomes de projeto, namespaces proprios e assembly names antigos; somente `ProjectReference` e usings para source foram atualizados.
- `Swa.Analyzers.slnx` foi renomeada para `CSF.Analyzers.slnx`.
- `IsSwaAnalyzerPackage` foi renomeada para `IsCsfAnalyzerPackage`.
- O target transitive de Architecture foi renomeado para `CSF.Analyzers.Architecture.targets`, mantendo o comportamento de incluir o projeto e o `Directory.Build.props` como `AdditionalFiles`.
- `AGENTS.md` foi atualizado somente nas referencias operacionais imediatas a solucao e diretorios de source.
- `Swa.Analyzers.code-workspace` manteve o nome de arquivo antigo, mas passou a apontar `dotnet.defaultSolution` para `CSF.Analyzers.slnx`.

## Excecoes temporarias

- `PackageId` permanece `Swa.Analyzers.Reliability`, `Swa.Analyzers.Architecture` e `Swa.Analyzers.Testing`.
- `InternalsVisibleTo` permanece apontando para `Swa.Analyzers.Reliability.Tests`, `Swa.Analyzers.Architecture.Tests` e `Swa.Analyzers.Testing.Tests`.
- Namespaces proprios de tests e samples podem permanecer `Swa.Analyzers.*` quando nao forem referencias aos analyzers de producao.
- Repository URLs e help links permanecem com `rodri-oliveira-dev/Swa.Analyzers`.
- Scripts de release, workspace IDE e revisao geral de documentacao ainda podem conter `Swa.Analyzers.slnx` ou paths antigos; eles ficam para etapas futuras do plano.

## Validacoes executadas

| Comando | Resultado |
| ------- | --------- |
| `dotnet restore ./CSF.Analyzers.slnx` | Aprovado. |
| `dotnet build ./CSF.Analyzers.slnx --configuration Release --no-restore` | Aprovado; warnings esperados dos samples invalidos e `EnableGenerateDocumentationFile`. |
| `dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1` | Aprovado; 246 testes, 0 falhas. |
| `dotnet sln ./CSF.Analyzers.slnx list` | Aprovado; 10 projetos listados, incluindo todos os projetos existentes. |
| `rg -n "ProjectReference Include=.*Swa\.Analyzers" tests samples` | Aprovado; sem `ProjectReference` para source antigo. |
| `rg -n "using Swa\.Analyzers\.(Reliability\|Architecture\|Testing\|Common)\|namespace Swa\.Analyzers\.(Reliability\|Architecture\|Testing\|Common)" src tests --glob "*.cs"` | Aprovado; sem namespaces/usings de source antigo. |

## Resultado final

- Solucao principal: `CSF.Analyzers.slnx`.
- Source de producao:
  - `src/CSF.Analyzers.Common`
  - `src/CSF.Analyzers.Reliability`
  - `src/CSF.Analyzers.Architecture`
  - `src/CSF.Analyzers.Testing`
- Assemblies de producao gerados pelo build:
  - `CSF.Analyzers.Reliability.dll`
  - `CSF.Analyzers.Architecture.dll`
  - `CSF.Analyzers.Testing.dll`
- Tests e samples continuam com identidade `Swa.Analyzers.*` por limite desta etapa.
- A etapa nao alterou comportamento dos analyzers.
