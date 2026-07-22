# Contribuindo com regras

Novas regras devem entrar no pacote que representa seu domínio real:

- `REL###` para confiabilidade e performance operacional.
- `ARC###` para políticas arquiteturais e de design.
- `TST###` para qualidade de testes.

Antes de propor uma regra, documente o problema concreto, público-alvo, falso positivo esperado, severidade padrão, opções públicas e relação com analyzers externos.

Toda regra ativa deve incluir:

- analyzer com `DiagnosticDescriptor` e `RuleHelpLinks.ForRule(...)`;
- testes automatizados focados;
- sample manual no pacote correspondente;
- documentação em `docs/rules/<pacote>/<ID>.md`;
- entrada em `AnalyzerReleases.Unshipped.md`;
- menção no README e na página do pacote.

Regras opt-in são preferíveis quando a política depende de convenção organizacional, como DDD, estilo de rotas ou padrão de testes.
