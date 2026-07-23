# Next analyzers readiness

Data: 2026-07-23

## Regras incluidas

| Regra | Pacote | Estado final |
| ----- | ------ | ------------ |
| `REL005` | `Swa.Analyzers.Reliability` | `Warning`, habilitada por padrao |
| `REL006` | `Swa.Analyzers.Reliability` | `Warning`, habilitada por padrao para tipos conhecidos e configurados |
| `ARC006` | `Swa.Analyzers.Architecture` | `Info`, opt-in |
| `TST001` | `Swa.Analyzers.Testing` | `Info`, opt-in; cobertura ampliada para APIs `AnyArgs` |

## Comportamento final auditado

- `REL005` reporta operacoes EF Core concorrentes sobre a mesma raiz de `DbContext` em `Task.WhenAll` e `Parallel.ForEachAsync`; nao reporta contextos distintos, execucao sequencial, APIs semelhantes sem EF Core, uso correto de `IDbContextFactory<TContext>` nem aliases locais fora do escopo documentado.
- `REL006` reporta `DbContext`/derivados, `IOptionsSnapshot<T>` e tipos configurados por `dotnet_diagnostic.REL006.scoped_type_patterns` capturados por `BackgroundService` ou `IHostedService`; nao reporta `IServiceScopeFactory`, `IDbContextFactory<TContext>`, `IOptionsMonitor<T>`, `IOptions<T>`, `IServiceProvider` nem tipos customizados nao configurados.
- `ARC006` reporta entidades de dominio diretamente expostas em parametros e retornos HTTP, incluindo wrappers `Task<T>`, `ValueTask<T>`, `ActionResult<T>`, colecoes, typed results e unions `Results<T1,...>`; nao reporta DTOs, tipos fora de endpoint HTTP, parametros de infraestrutura ou `[FromServices]`. O classificador de entidade e compartilhado com `ARC004`.
- `TST001` preserva o comportamento de `Arg.Any<T>()` e passa a reportar `ReturnsForAnyArgs`, `WhenForAnyArgs` e `ReceivedWithAnyArgs`, mantendo `DidNotReceiveWithAnyArgs` permitido.

## Documentacao

Foram revisados `README.md`, paginas dos tres pacotes, perfis de `.editorconfig`, guias de adocao/contribuicao, matriz de sobreposicao e as paginas `REL005`, `REL006`, `ARC006` e `TST001`. A matriz de sobreposicao foi atualizada com pesquisa externa revisada em 2026-07-23.

## Testes e cobertura

Validacao final executada:

- `dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1`: 246 testes aprovados, 0 falhas.
- `dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1 --settings ./coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory ./artifacts/TestResults-next-analyzers-readiness`: 246 testes aprovados, 0 falhas.
- Coverage consolidada da execucao limpa: 85.1% de linhas, 68.8% de branches e 94.4% de metodos.
- Cobertura por regra nova/evoluida: `REL005` 82.4%, `REL006` 96.6%, `ARC006` 84.1%, `TST001` 87.9%.

## Performance

`dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1 --filter FullyQualifiedName~Performance` passou com 6 testes de performance:

- Reliability: 3 testes aprovados, incluindo guardrails para `REL005` e `REL006`.
- Architecture: 1 teste aprovado, cobrindo o pacote com `ARC006`.
- Testing: 2 testes aprovados, incluindo `TST001`.

Nao houve evidencia de regressao relevante no tracking de simbolos de `REL005`, pattern matching de `REL006` ou unwrap de tipos de `ARC006`.

## Package isolation

Validado por teste automatizado e script:

- `Swa.Analyzers.Reliability`: somente `REL001`-`REL006`.
- `Swa.Analyzers.Architecture`: somente `ARC001`-`ARC006`.
- `Swa.Analyzers.Testing`: somente `TST001`-`TST002`.

Comandos executados:

- `dotnet test ./Swa.Analyzers.slnx --configuration Release --no-build -m:1`: `Swa.Analyzers.PackageValidation.Tests` aprovou 3/3.
- `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Validate-AnalyzerPackageIsolation.ps1 -PackageDirectory ./artifacts/final-validation -Version 1.0.0`: aprovado.

## Pack

`dotnet pack ./Swa.Analyzers.slnx --configuration Release --no-build --output ./artifacts/final-validation` gerou com sucesso:

- `Swa.Analyzers.Reliability.1.0.0.nupkg` e `.snupkg`
- `Swa.Analyzers.Architecture.1.0.0.nupkg` e `.snupkg`
- `Swa.Analyzers.Testing.1.0.0.nupkg` e `.snupkg`

`powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Inspect-NuGetPackages.ps1 -PackageDirectory ./artifacts/final-validation -Version 1.0.0` passou.

Tambem foi validado consumo local via feed NuGet em `artifacts/final-validation` com um projeto temporario `net10.0`; os tres pacotes foram restaurados e o consumidor compilou em `Release` com 0 erros.

## Limitacoes conhecidas

- `REL005`: sem analise interprocedural, sem prova completa de aliasing e sem deteccao de concorrencia criada por `Task.Run`, `Task.WhenAny`, threads, filas ou bibliotecas customizadas.
- `REL006`: nao calcula lifetimes customizados a partir de registro no container e nao segue factories, delegates ou service locators.
- `ARC006`: nao faz analise profunda de DTOs, nao infere payload apagado por `object`/`dynamic` e pode ignorar handlers Minimal API construidos dinamicamente.
- `TST001`: depende de reconhecimento semantico de NSubstitute e de contexto de teste reconhecido.

## Analise de sobreposicao

`REL005`, `REL006`, `ARC006` e a evolucao de `TST001` tem ferramentas e documentacao relacionadas, mas a auditoria nao identificou substituto direto que preserve o mesmo recorte contextual. A recomendacao e coexistir com analyzers e validacoes externas, mantendo estas regras como politicas locais opt-in ou default conforme severidade definida.

## Riscos restantes

- `REL005` pode deixar passar aliases locais ou concorrencia interprocedural intencionalmente fora da primeira versao.
- `REL006` depende de configuracao explicita para tipos scoped customizados.
- `ARC006` depende da calibragem do classificador de entidades de `ARC004`.
- `TST001` pode exigir suppressions em testes que adotam matching amplo como convencao local.

## Recomendacao final

`READY`.
