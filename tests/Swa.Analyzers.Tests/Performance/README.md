# Testes de performance

Estes testes são guardrails conservadores contra regressões graves no tempo de execução dos analyzers. Eles não são benchmarks e não devem orientar decisões de micro-otimização.

Os cenários usam fontes multiarquivo geradas com stubs de ASP.NET Core, EF Core e logging para que a suite regular exercite análise semântica sem adicionar pacotes externos.
