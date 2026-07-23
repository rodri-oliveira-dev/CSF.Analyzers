# CSF rename SDD plan

## Objetivo

Definir e encerrar a migracao da identidade anterior do repositorio para `CSF.Analyzers`, preservando os IDs de regras `REL###`, `ARC###` e `TST###`.

## Estado final

- Identidade canonica: `CSF`.
- Produto principal: `CSF.Analyzers`.
- Repositorio canonico: `rodri-oliveira-dev/CSF.Analyzers`.
- Solucao principal: `CSF.Analyzers.slnx`.
- Pacotes:
  - `CSF.Analyzers.Reliability`
  - `CSF.Analyzers.Architecture`
  - `CSF.Analyzers.Testing`
- Projetos de analyzer em `src/CSF.Analyzers.*`.
- Testes em `tests/CSF.Analyzers.*`.
- Samples em `samples/CSF.Analyzers.*.Sample`.
- Namespaces publicos e internos usam `CSF.Analyzers.*`.
- Scripts, hooks e workflows usam `CSF`.
- Metadados NuGet e help links apontam para `https://github.com/rodri-oliveira-dev/CSF.Analyzers`.

## Escopo concluido

- Renomeacao de solucao, projetos, assemblies, namespaces, tests, samples e workspace.
- Migracao de PackageIds, targets, scripts de validacao, workflows e hooks.
- Atualizacao de documentacao publica, docs de pacote, docs de regras, specs e instrucoes de agentes.
- Confirmacao do slug canonico do repositorio GitHub.
- Remocao do indice de arquivos `.vs/**` indevidamente rastreados.
- Sanitizacao das specs de migracao para atender ao criterio final de zero ocorrencia literal da identidade anterior.

## Status das etapas

- Etapa 1: especificacao SDD inicial, concluida.
- Etapa 2: source projects, concluida.
- Etapa 3: tests e samples, concluida.
- Etapa 4: packaging e release, concluida.
- Etapa 5: documentacao, concluida.
- Etapa 6: GitHub repository, concluida.
- Etapa 7: auditoria final e encerramento, concluida nesta revisao.

## Criterios finais de aceite

- Filesystem rastreado nao contem nomes com a identidade anterior.
- Conteudo rastreado nao contem literais da identidade anterior.
- Solution e `CSF.Analyzers.slnx`.
- Namespaces sao `CSF.Analyzers.*`.
- Projetos sao `CSF.Analyzers.*`.
- PackageIds sao `CSF.Analyzers.*`.
- Tests e samples usam `CSF`.
- Scripts e CI usam `CSF`.
- Documentacao usa `CSF`.
- URLs proprias usam `rodri-oliveira-dev/CSF.Analyzers`.
- Build passa.
- Testes passam.
- Pack passa.
- Validacoes de release passam.
- Spec registra conclusao.

## Validacao final esperada

```powershell
dotnet restore ./CSF.Analyzers.slnx --locked-mode
dotnet build ./CSF.Analyzers.slnx --configuration Release --no-restore
dotnet test ./CSF.Analyzers.slnx --configuration Release -m:1
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-Release.ps1
dotnet pack ./CSF.Analyzers.slnx --configuration Release --no-build --output ./artifacts/csf-rename-final /p:PackageVersion=0.0.0-csf-rename.final
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Inspect-NuGetPackages.ps1 -PackageDirectory ./artifacts/csf-rename-final -Version '0.0.0-csf-rename.final'
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-AnalyzerPackageIsolation.ps1 -PackageDirectory ./artifacts/csf-rename-final -Version '0.0.0-csf-rename.final'
```

## Encerramento

A migracao para `CSF.Analyzers` esta concluida. O relatorio de auditoria final fica em `docs/specs/csf-rename/completion.md`.
