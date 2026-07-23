# TST001: Expandir cobertura para APIs AnyArgs do NSubstitute

## Estado planejado

| Campo | Valor |
| ----- | ----- |
| ID | `TST001` |
| Pacote | `CSF.Analyzers.Testing` |
| Categoria | TestQuality |
| Severidade | Info |
| Estado padrao | Opt-in |
| Code fix | Nao |
| Novo diagnostico | Nao |

## Problema

`TST001` hoje restringe `NSubstitute.Arg.Any<T>()`, permitindo apenas a convencao de assercao negativa com `DidNotReceive()` ou `DidNotReceiveWithAnyArgs()`. O mesmo problema conceitual aparece em APIs NSubstitute que explicitamente ignoram todos os argumentos de uma chamada.

Fontes NSubstitute:

- <https://nsubstitute.github.io/help/argument-matchers/>
- <https://nsubstitute.github.io/help/return-for-any-args/>
- <https://nsubstitute.github.io/help/received-calls/>
- <https://nsubstitute.github.io/help/callbacks/>

## Escopo da expansao

Manter o mesmo `DiagnosticDescriptor` e reportar tambem usos de:

- `ReturnsForAnyArgs`;
- `WhenForAnyArgs`;
- `ReceivedWithAnyArgs`.

Manter permitido:

- `DidNotReceiveWithAnyArgs`.

Preservar todo o comportamento atual de:

- `Arg.Any<T>()`;
- `DidNotReceive()`;
- `DidNotReceiveWithAnyArgs()`.

## Resolucao semantica obrigatoria

A regra deve resolver simbolos do NSubstitute. Nao detectar apenas por nome textual.

Um metodo `*AnyArgs` deve ser alvo somente quando:

- o simbolo resolvido pertence ao namespace raiz `NSubstitute` ou namespace oficial de extensoes do NSubstitute; e
- o metodo corresponde a API publica esperada do pacote NSubstitute.

Metodos customizados homonimos nao devem gerar diagnostico.

## Comportamento por API

### `ReturnsForAnyArgs`

Reportar em setups que configuram retorno ignorando argumentos:

```csharp
calculator.Add(1, 2).ReturnsForAnyArgs(100);
```

Motivo: equivalente conceitual a matcher amplo em setup positivo.

### `WhenForAnyArgs`

Reportar callback configurado ignorando argumentos:

```csharp
calculator.WhenForAnyArgs(x => x.Add(0, 0)).Do(_ => called = true);
```

Motivo: callback fica associado a qualquer combinacao de argumentos, reduzindo precisao do teste.

### `ReceivedWithAnyArgs`

Reportar assercao positiva que ignora argumentos:

```csharp
calculator.ReceivedWithAnyArgs().Add(default, default);
```

Motivo: assercao positiva deve expressar argumentos relevantes ou matchers especificos.

### `DidNotReceiveWithAnyArgs`

Continuar permitido:

```csharp
calculator.DidNotReceiveWithAnyArgs().Add(default, default);
```

Motivo: a convencao atual aceita matching amplo em assercoes negativas, onde a intencao e confirmar ausencia de chamada independentemente dos argumentos.

## `ReturnsForAll<T>`

Nao incluir `ReturnsForAll<T>` nesta evolucao.

A documentacao oficial descreve `ReturnsForAll<T>` como retorno padrao para todas as chamadas que retornam um tipo especifico e ainda nao foram configuradas. Isso e amplo, mas nao e a mesma semantica de "ignorar argumentos desta chamada". Se o time quiser restringir defaults globais de substitutes, isso deve ser outra decisao de produto.

Fonte: <https://nsubstitute.github.io/help/return-for-all/>

## Contexto de teste

Preservar a restricao atual: a regra so roda em contexto de teste reconhecido por atributos conhecidos, usando `TestContextHelper`.

Nao reportar em projetos sem NSubstitute referenciado.

## Localizacao do diagnostico

Preferir reportar no nome do metodo NSubstitute:

- `ReturnsForAnyArgs`;
- `WhenForAnyArgs`;
- `ReceivedWithAnyArgs`;
- `Arg.Any` segue localizacao atual.

## Casos que nao devem gerar diagnostico

```csharp
calculator.DidNotReceiveWithAnyArgs().Add(default, default);
```

```csharp
custom.ReceivedWithAnyArgs();
```

quando `custom.ReceivedWithAnyArgs` nao for simbolo do NSubstitute.

```csharp
sub.ReturnsForAll<string>("value");
```

na primeira versao desta expansao.

## Sobreposicao com NSubstitute.Analyzers

NSubstitute.Analyzers foca usos incorretos do framework, como substituir membros nao virtuais e outros erros de uso. Esta expansao permanece diferenciada porque codifica convencao de precisao de testes do `CSF.Analyzers.Testing`: evitar constructs que deixam argumentos irrelevantes em setups e asserts positivos.

Fonte: <https://nsubstitute.github.io/help/nsubstitute-analysers/>

## Testes esperados

- Reporta `ReturnsForAnyArgs` do NSubstitute.
- Reporta `WhenForAnyArgs` do NSubstitute.
- Reporta `ReceivedWithAnyArgs` do NSubstitute.
- Nao reporta `DidNotReceiveWithAnyArgs`.
- Nao reporta `ReturnsForAll<T>`.
- Nao reporta metodos homonimos fora de NSubstitute.
- Preserva todos os testes atuais de `Arg.Any<T>()`.
- Continua opt-in via severidade de `TST001`.

## Documentacao publica futura

Quando implementada, atualizar [TST001](../../rules/testing/TST001.md), sample `samples/CSF.Analyzers.Testing.Sample/Tst001`, README, pagina do pacote Testing e `AnalyzerReleases.Unshipped.md` se a politica de release exigir registrar alteracao de regra existente.
