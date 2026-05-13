# Testes de performance

Estes testes sao guardrails conservadores contra regressoes graves no tempo de execucao dos analyzers. Eles nao sao benchmarks e nao devem orientar decisoes de micro-otimizacao.

Os cenarios usam fontes multiarquivo geradas com stubs de ASP.NET Core, EF Core e logging para que a suite regular exercite analise semantica sem adicionar pacotes externos.
