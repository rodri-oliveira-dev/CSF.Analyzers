# ARCH026: Evite configuração insegura de CORS

## Objetivo

Detectar políticas CORS do ASP.NET Core que combinam origem wildcard com credenciais.

`AllowAnyOrigin()` junto com `AllowCredentials()` cria uma política perigosa: cookies, certificados de cliente ou cabecalhos de autorização podem ser aceitos em uma configuração que comunica permissao ampla demais. A alternativa segura e declarar origens explicitas com `WithOrigins(...)` quando credenciais forem necessárias.

## Código não conforme

```csharp
using Microsoft.AspNetCore.Cors.Infrastructure;

public static class CorsConfiguration
{
    public static void Configure(CorsPolicyBuilder policy)
    {
        policy.AllowAnyOrigin()
            .AllowCredentials();

        policy.AllowCredentials()
            .AllowAnyOrigin();
    }
}
```

## Código conforme

```csharp
using Microsoft.AspNetCore.Cors.Infrastructure;

public static class CorsConfiguration
{
    public static void Configure(CorsPolicyBuilder policy)
    {
        policy.WithOrigins("https://app.example.com")
            .AllowCredentials();

        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    }
}
```

## Configuração

Por padrão, `AllowAnyOrigin()` isolado é permitido. Essa escolha evita falso positivo em APIs públicas que não aceitam credenciais.

Projetos que querem uma política mais rigida podem bloquear também qualquer uso de `AllowAnyOrigin()`:

```ini
[*.cs]
dotnet_diagnostic.ARCH026.disallow_any_origin = true
```

Valores ausentes, `false` ou inválidos mantém o comportamento padrão.

### Fallback das opções

- `disallow_any_origin`: booleano; default `false`. Somente `true` habilita a política mais rigida. Valores booleanos aceitam casing variado; valor ausente, vazio ou inválido usa `false`.

O fallback é permissivo apenas para o bloqueio opcional de `AllowAnyOrigin()` isolado; a combinacao `AllowAnyOrigin()` com `AllowCredentials()` continua sendo reportada.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH026.severity = warning
```

## Heurística

O analyzer usa análise semântica e reconhece apenas chamadas em `Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder`.

A regra reporta quando encontra:

- `AllowAnyOrigin()` e `AllowCredentials()` na mesma cadeia fluente, independentemente da ordem;
- `AllowAnyOrigin()` isolado, somente quando `dotnet_diagnostic.ARCH026.disallow_any_origin = true`.

Para reduzir falsos positivos, a regra ignora:

- `WithOrigins("https://...")` com `AllowCredentials()`;
- `AllowAnyOrigin()` sem credenciais quando a opção rigida não está habilitada;
- código dentro de contexto de teste xUnit, NUnit ou MSTest reconhecido pelo projeto;
- métodos customizados com os mesmos nomes fora de `CorsPolicyBuilder`;
- código gerado, seguindo a configuração padrão dos analyzers do projeto.

## Limitações conhecidas

- A regra analisa apenas cadeias fluentemente encadeadas no código fonte. Ela não infere chamadas separadas por variáveis intermediarias ou métodos auxiliares.
- Configuracoes aplicadas dinamicamente por delegates, reflection ou extensões customizadas não são expandidas nesta versão.
- A opção `disallow_any_origin` é booleana: apenas `true` habilita a política mais restritiva.

## Impacto esperado

- Menos risco de expor endpoints autenticados para origens amplas demais.
- Politicas CORS com credenciais ficam mais faceis de auditar.
- Times que exigem allowlist de origem podem reforcar essa decisão por `.editorconfig`.
