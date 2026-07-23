# CSF rename - packaging and release

## Estado inicial

- Worktree ja contem a migracao de source, tests e samples para `CSF.Analyzers.*`, ainda nao commitada.
- A solucao principal e `CSF.Analyzers.slnx`.
- Os projetos de analyzer estao em `src/CSF.Analyzers.{Reliability,Architecture,Testing}` e geram assemblies `CSF.Analyzers.*` por nome de projeto.
- `Directory.Build.props` reconhece `IsCsfAnalyzerPackage`, empacota `$(OutputPath)$(AssemblyName).dll` e `.pdb` em `analyzers/dotnet/cs`, mas ainda aponta `RepositoryUrl` e `PackageProjectUrl` para `https://github.com/rodri-oliveira-dev/Swa.Analyzers`.
- Os `PackageId` publicos ainda estao como `Swa.Analyzers.*`.
- `src/CSF.Analyzers.Architecture/buildTransitive/CSF.Analyzers.Architecture.targets` ja usa o nome de arquivo `CSF` e propriedade `CsfAnalyzersArchitectureDirectoryBuildProps`.
- Scripts, hooks e workflows ainda usam paths e validacoes com `Swa.Analyzers`.

## PackageIds

| Projeto | Atual | Alvo |
| ------- | ----- | ---- |
| `src/CSF.Analyzers.Reliability/CSF.Analyzers.Reliability.csproj` | `Swa.Analyzers.Reliability` | `CSF.Analyzers.Reliability` |
| `src/CSF.Analyzers.Architecture/CSF.Analyzers.Architecture.csproj` | `Swa.Analyzers.Architecture` | `CSF.Analyzers.Architecture` |
| `src/CSF.Analyzers.Testing/CSF.Analyzers.Testing.csproj` | `Swa.Analyzers.Testing` | `CSF.Analyzers.Testing` |

## Assemblies

Assemblies alvo ja sao derivados dos nomes dos projetos:

- `CSF.Analyzers.Reliability.dll`
- `CSF.Analyzers.Architecture.dll`
- `CSF.Analyzers.Testing.dll`

Os `.snupkg` devem conter os PDBs correspondentes:

- `CSF.Analyzers.Reliability.pdb`
- `CSF.Analyzers.Architecture.pdb`
- `CSF.Analyzers.Testing.pdb`

Nao deve existir assembly legado `Swa.Analyzers*.dll` em `analyzers/dotnet/cs`.

## buildTransitive e targets

- Arquivo esperado: `src/CSF.Analyzers.Architecture/buildTransitive/CSF.Analyzers.Architecture.targets`.
- Package path esperado: `buildTransitive/CSF.Analyzers.Architecture.targets`.
- Propriedade MSBuild esperada: `CsfAnalyzersArchitectureDirectoryBuildProps`.
- Comportamento preservado: adicionar o projeto consumidor e `Directory.Build.props` como `AdditionalFiles` para `ARC005`.

## Directory.Build

- `Directory.Build.props` deve manter `IsCsfAnalyzerPackage`.
- `RepositoryUrl` e `PackageProjectUrl` devem migrar para URL `CSF` quando a identidade publica do pacote for migrada.
- `IncludeBuildOutput=false`, `SuppressDependenciesWhenPacking=true`, `PackageReadmeFile`, `IncludeSymbols`, `IncludeSource`, `SymbolPackageFormat=snupkg` e `analyzers/dotnet/cs` devem ser preservados.
- `Directory.Build.targets` deve continuar instalando hooks a partir de `CSF.Analyzers.Reliability`.

## Scripts

Arquivos afetados:

- `scripts/Validate-Release.ps1`
- `scripts/Inspect-NuGetPackages.ps1`
- `scripts/Validate-AnalyzerPackageIsolation.ps1`

Mudancas esperadas:

- Package names, paths de source/tests/samples e descricao dos scripts devem usar `CSF.Analyzers`.
- Validacao de pacote deve esperar artifacts `CSF.Analyzers.*.<versao>.nupkg` e `CSF.Analyzers.*.<versao>.snupkg`.
- Validacao de isolamento deve apontar para `tests/CSF.Analyzers.PackageValidation.Tests/CSF.Analyzers.PackageValidation.Tests.csproj`.
- Inspecao deve procurar assemblies e PDBs `CSF.Analyzers.*`.
- Qualquer pacote ou assembly `Swa.Analyzers*` gerado deve falhar a validacao, exceto referencias historicas explicitamente documentadas.

## Workflows e hooks

Arquivos afetados:

- `.github/workflows/dotnet.yml`
- `.github/workflows/release.yml`
- `.github/workflows/codeql.yml`
- `.github/workflows/release-check.yml` apenas se necessario
- `.githooks/pre-push`

Mudancas esperadas:

- Restore, build, test e pack devem usar `./CSF.Analyzers.slnx`.
- Release pack deve apontar para os `.csproj` em `src/CSF.Analyzers.*`.
- Nome da GitHub Release deve ser `CSF.Analyzers {tag}`.
- Upload/attestation continuam usando `artifacts/packages/*.nupkg` e `*.snupkg`.
- Nome do artifact `nuget-packages` permanece estavel porque e um nome operacional do workflow, nao identidade publica do produto.
- Hook `pre-push` deve usar `CSF.Analyzers.slnx`.

## Validações de pacote e release

Deve ser validado:

- IDs dos pacotes no nuspec sao `CSF.Analyzers.Reliability`, `CSF.Analyzers.Architecture` e `CSF.Analyzers.Testing`.
- Versao no nuspec bate com `PackageVersion`.
- Metadata de README aponta para `README.md`.
- Repository URL usa a URL `CSF` esperada, salvo excecao temporaria abaixo.
- Cada `.nupkg` contem exatamente uma DLL de analyzer em `analyzers/dotnet/cs`.
- Cada `.snupkg` contem os PDBs esperados e nenhuma DLL.
- Nenhum pacote legado `Swa.Analyzers.*` e gerado.
- `Validate-Release.ps1` encontra rules, metadata, tests e samples pelos paths `CSF`.
- `AnalyzerPackageIsolationTests` espera os nomes de package `CSF`.

## Exceções temporárias de URL

- `https://github.com/rodri-oliveira-dev/Swa.Analyzers` podia permanecer somente onde documentasse historico da migracao ou onde fosse necessario manter links funcionais antes do rename remoto.
- Nesta etapa, a URL ativa de package metadata e help links foi migrada para `https://github.com/rodri-oliveira-dev/CSF.Analyzers`, mesmo que o rename remoto ainda nao tivesse ocorrido. A etapa 06 removeu esse risco ao renomear o repositorio GitHub.
- O plano historico em `docs/specs/csf-rename/plan.md` e specs anteriores podem manter referencias `Swa.Analyzers` como registro do estado anterior.

## Riscos de contrato externo

- Troca de `PackageId` e breaking change para consumidores que usam `PackageReference Include="Swa.Analyzers.*"`.
- Release workflow pode gerar artifacts com nome novo e qualquer automacao externa que filtre `Swa.Analyzers.*.nupkg` deixara de encontrar pacotes.
- Repository URL e help links `CSF` podem nao funcionar ate o rename remoto acontecer.
- Lock files devem ser atualizados para as chaves `csf.analyzers.*`.
- Validacoes podem falhar se artifacts antigos ficarem em `artifacts/packages`; o pack desta etapa deve limpar ou usar diretorio dedicado.
- O pacote Architecture deve continuar incluindo o `.targets` `CSF`, sem alterar o comportamento de `ARC005`.

## Critérios de aceite

- `PackageId` dos tres analyzers usa `CSF.Analyzers.*`.
- Scripts, workflows, hook e validacoes locais nao dependem mais de paths ou artifacts `Swa.Analyzers.*`.
- `dotnet restore ./CSF.Analyzers.slnx --locked-mode` passa.
- `dotnet build ./CSF.Analyzers.slnx --configuration Release --no-restore` passa.
- `dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1` passa.
- `dotnet pack` gera exatamente os tres pacotes `CSF.Analyzers.*` e seus tres symbol packages.
- `scripts/Validate-Release.ps1`, `scripts/Validate-AnalyzerPackageIsolation.ps1` e `scripts/Inspect-NuGetPackages.ps1` passam.
- Inspecao manual dos `.nupkg` confirma metadata e conteudo `CSF`.
- Nao ha alteracao de comportamento dos analyzers.

## Evidências de validação

Executado em 2026-07-23:

| Comando | Resultado |
| ------- | --------- |
| `dotnet restore ./CSF.Analyzers.slnx` | Aprovado; usado para atualizar `packages.lock.json` apos troca de `PackageId`. |
| `dotnet restore ./CSF.Analyzers.slnx --locked-mode` | Aprovado. |
| `dotnet build ./CSF.Analyzers.slnx --configuration Release --no-restore` | Aprovado; warnings esperados dos samples invalidos e `EnableGenerateDocumentationFile`. |
| `dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1` | Aprovado; 246 testes, 0 falhas. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1` | Aprovado; `release-check: validacoes aprovadas`. |
| `dotnet pack ./CSF.Analyzers.slnx --configuration Release --no-build --output ./artifacts/csf-rename-04-packages-20260723170542 /p:PackageVersion=0.0.0-csf-rename.4` | Aprovado; 3 `.nupkg` e 3 `.snupkg` gerados com prefixo `CSF.Analyzers.*`. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Inspect-NuGetPackages.ps1 -PackageDirectory ./artifacts/csf-rename-04-packages-20260723170542 -Version '0.0.0-csf-rename.4'` | Aprovado; package inspection aprovada. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-AnalyzerPackageIsolation.ps1 -PackageDirectory ./artifacts/csf-rename-04-packages-20260723170542 -Version '0.0.0-csf-rename.4'` | Aprovado; 3 testes de isolamento e package inspection aprovados. |

Inspecao manual dos `.nupkg`:

| Pacote | Nuspec id | Repository URL | Entradas relevantes |
| ------ | --------- | -------------- | ------------------- |
| `CSF.Analyzers.Reliability.0.0.0-csf-rename.4.nupkg` | `CSF.Analyzers.Reliability` | `https://github.com/rodri-oliveira-dev/CSF.Analyzers` | `analyzers/dotnet/cs/CSF.Analyzers.Reliability.dll`, `analyzers/dotnet/cs/CSF.Analyzers.Reliability.pdb`, `README.md` |
| `CSF.Analyzers.Architecture.0.0.0-csf-rename.4.nupkg` | `CSF.Analyzers.Architecture` | `https://github.com/rodri-oliveira-dev/CSF.Analyzers` | `analyzers/dotnet/cs/CSF.Analyzers.Architecture.dll`, `analyzers/dotnet/cs/CSF.Analyzers.Architecture.pdb`, `buildTransitive/CSF.Analyzers.Architecture.targets`, `README.md` |
| `CSF.Analyzers.Testing.0.0.0-csf-rename.4.nupkg` | `CSF.Analyzers.Testing` | `https://github.com/rodri-oliveira-dev/CSF.Analyzers` | `analyzers/dotnet/cs/CSF.Analyzers.Testing.dll`, `analyzers/dotnet/cs/CSF.Analyzers.Testing.pdb`, `README.md` |

## Resultado final

- `PackageId`, nuspec id, artifact names, scripts, workflows, hooks e docs operacionais de pacote/release usam `CSF.Analyzers.*`.
- `RepositoryUrl`, `PackageProjectUrl` e help links ativos apontam para `https://github.com/rodri-oliveira-dev/CSF.Analyzers`.
- O pacote `CSF.Analyzers.Architecture` inclui `buildTransitive/CSF.Analyzers.Architecture.targets`.
- Validacoes falham explicitamente se pacotes ou arquivos de analyzer legados `Swa.Analyzers*` forem gerados.
- Nao houve alteracao de comportamento dos analyzers.
