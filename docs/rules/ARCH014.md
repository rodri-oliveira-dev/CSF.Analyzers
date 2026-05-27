# ARCH014: Prefira Is.Equivalent em vez de NSubstitute Arg.Is

## Objetivo

Incentivar o uso de uma convenção de equivalência adotada pelo consumidor (`Is.Equivalent`) em vez de `Arg.Is` do NSubstitute para match de valores em testes.

`Is.Equivalent` não é fornecido por este pacote de analyzers. A regra assume que o projeto consumidor já possui uma API equivalente, seja como helper local de testes, biblioteca interna do time ou pacote externo adotado pela organização.

## Motivação

Usar uma API de equivalência padronizada em todo o time traz vários beneficios:

- **Consistência**: todos os testes usam os mesmos padrões de asserção, tornando-os mais fáceis de ler e manter
- **Melhores mensagens de erro**: a API adotada pelo time normalmente fornece mensagens de falha mais descritivas
- **Menor acoplamento**: os testes ficam menos dependentes de APIs específicas do NSubstitute
- **Melhor manutenção**: lógica de asserção centralizada e mais fácil de atualizar e evoluir

## Contrato de adoção

Para adotar a ARCH014, o consumidor deve disponibilizar uma API chamada, importada ou acessível como `Is.Equivalent(...)` nos projetos de teste onde a regra será habilitada. Essa API precisa ser definida pelo próprio consumidor ou por uma dependência escolhida por ele.

O analyzer não valida a existência, namespace, assinatura ou semântica de `Is.Equivalent`. O nome aparece na mensagem e na documentação apenas como a convenção esperada para substituir o uso de `NSubstitute.Arg.Is`.

Antes de elevar a severidade da regra, confirme que:

- os projetos de teste conseguem chamar `Is.Equivalent(...)`;
- a API cobre os cenários em que hoje se usa `Arg.Is(value)` ou `Arg.Is<T>(predicate)`;
- há orientação interna para casos em que o predicado de `Arg.Is` é complexo, possui efeitos colaterais ou não tem substituição direta.

## Código não conforme

```csharp
// Using NSubstitute Arg.Is for value matching
substitute.Received().Do(NSubstitute.Arg.Is<int>(x => x > 0));

// Using Arg.Is with simple value matching
substitute.Received().Process(NSubstitute.Arg.Is(42));
```

## Código conforme

```csharp
// Using the consumer's adopted equivalence convention
substitute.Received().Do(Is.Equivalent(42));

// Using the same convention when the local API supports predicates
substitute.Received().Do(Is.Equivalent(x => x > 0));
```

## Configuração

Esta regra não oferece opções de configuração.

## Limitações conhecidas

- A regra detecta apenas chamadas ao método `Is` do tipo `NSubstitute.Arg`, incluindo usos via alias.
- A regra só é ativada quando a compilação referência um tipo `NSubstitute.Arg` e também contém atributos de métodos de teste conhecidos.
- A regra reporta diagnósticos apenas dentro de tipos de teste, ou seja, tipos que contêm métodos reconhecidos como testes.
- A regra não verifica se `Is.Equivalent` existe no projeto consumidor.
- A regra não fornece code fix porque a substituição adequada depende do caso de uso específico e da API de equivalência adotada pelo consumidor.

## Quando não usar

Esta regra pode não ser adequada se:

- Seu time não tem uma API de equivalência padronizada
- Seu time prefere explícitamente a API `Arg.Is` do NSubstitute
- Você está trabalhando em código legado onde a migração seria custosa demais

## Impacto esperado

- **Qualidade de código**: mais consistência e legibilidade entre suítes de teste
- **Manutencao**: mais fácilidade para atualizar padrões de asserção de forma centralizada
- **Padrões do time**: reforca a adoção de convenções de teste compartilhádas pelo time

## Observações

- Esta regra reporta diagnósticos para qualquer uso de `NSubstitute.Arg.Is` dentro de um tipo de teste, independentemente de o método específico ter um atributo de teste.
- A regra detecta as sobrecargas `Arg.Is<T>(predicate)` e `Arg.Is(value)` quando resolvidas semanticamente para `NSubstitute.Arg`.
- A regra funciona com os frameworks de teste comuns reconhecidos pelo projeto, como xUnit, NUnit e MSTest.
- Chamadas a outros tipos chamados `Arg` ou a métodos `Is` de outras bibliotecas não são diagnosticadas.
