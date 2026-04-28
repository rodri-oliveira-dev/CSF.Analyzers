# ARCH014: Prefira Is.Equivalent em vez de NSubstitute Arg.Is

## Objetivo

Incentivar o uso da biblioteca padrão de asserções do time (`Is.Equivalent`) em vez de `Arg.Is` do NSubstitute para match de valores em asserções de teste.

## Motivação

Usar uma biblioteca de asserções padronizada em todo o time traz vários beneficios:

- **Consistência**: todos os testes usam os mesmos padrões de asserção, tornando-os mais fáceis de ler e manter
- **Melhores mensagens de erro**: a biblioteca padrão do time normalmente fornece mensagens de falha mais descritivas
- **Menor acoplamento**: os testes ficam menos dependentes de APIs específicas do NSubstitute
- **Melhor manutenção**: lógica de asserção centralizada e mais fácil de atualizar e evoluir

## Código não conforme

```csharp
// Using NSubstitute Arg.Is for value matching
substitute.Received().Do(NSubstitute.Arg.Is<int>(x => x > 0));

// Using Arg.Is with simple value matching
substitute.Received().Process(NSubstitute.Arg.Is(42));
```

## Código conforme

```csharp
// Using the team's standard library
substitute.Received().Do(Is.Equivalent(42));

// Using the team's standard library with predicates
substitute.Received().Do(Is.Equivalent(x => x > 0));
```

## Configuração

Esta regra não oferece opções de configuração.

## Limitações conhecidas

- A regra detecta apenas chamadas `Arg.Is` do namespace `NSubstitute`
- A regra reporta diagnósticos apenas dentro de tipos de teste (classes que contém métodos de teste)
- A regra não fornece code fix porque a substituição adequada depende do caso de uso específico e da API da biblioteca padrão do time

## Quando não usar

Esta regra pode não ser adequada se:

- Seu time não tem uma biblioteca de asserções padronizada
- Seu time prefere explícitamente a API `Arg.Is` do NSubstitute
- Você está trabalhando em código legado onde a migração seria custosa demais

## Impacto esperado

- **Qualidade de código**: mais consistência e legibilidade entre suítes de teste
- **Manutencao**: mais fácilidade para atualizar padrões de asserção de forma centralizada
- **Padroes do time**: reforca a adoção de convenções de teste compartilhádas pelo time

## Observações

- Esta regra reporta diagnósticos para qualquer uso de `Arg.Is` dentro de um tipo de teste, independentemente de o método específico ter um atributo de teste
- A regra detecta as sobrecargas `Arg.Is<T>(predicate)` e `Arg.Is(value)`
- A regra funciona com os frameworks de teste comuns (xUnit, NUnit, MSTest)
