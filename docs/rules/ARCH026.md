# ARCH026: Evite configuracao insegura de CORS

## Objetivo

Detectar politicas CORS do ASP.NET Core que combinam origem wildcard com credenciais.

`AllowAnyOrigin()` junto com `AllowCredentials()` cria uma politica perigosa: cookies, certificados de cliente ou cabecalhos de autorizacao podem ser aceitos em uma configuracao que comunica permissao ampla demais. A alternativa segura e declarar origens explicitas com `WithOrigins(...)` quando credenciais forem necessarias.

## Codigo nao conforme

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

## Codigo conforme

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

## Configuracao

Por padrao, `AllowAnyOrigin()` isolado e permitido. Essa escolha evita falso positivo em APIs publicas que nao aceitam credenciais.

Projetos que querem uma politica mais rigida podem bloquear tambem qualquer uso de `AllowAnyOrigin()`:

```ini
[*.cs]
dotnet_diagnostic.ARCH026.disallow_any_origin = true
```

Valores ausentes, `false` ou invalidos mantem o comportamento padrao.

### Fallback das opcoes

- `disallow_any_origin`: booleano; default `false`. Somente `true` habilita a politica mais rigida. Valores booleanos aceitam casing variado; valor ausente, vazio ou invalido usa `false`.

O fallback e permissivo apenas para o bloqueio opcional de `AllowAnyOrigin()` isolado; a combinacao `AllowAnyOrigin()` com `AllowCredentials()` continua sendo reportada.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH026.severity = warning
```

## Heuristica

O analyzer usa analise semantica e reconhece apenas chamadas em `Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder`.

A regra reporta quando encontra:

- `AllowAnyOrigin()` e `AllowCredentials()` na mesma cadeia fluente, independentemente da ordem;
- `AllowAnyOrigin()` isolado, somente quando `dotnet_diagnostic.ARCH026.disallow_any_origin = true`.

Para reduzir falsos positivos, a regra ignora:

- `WithOrigins("https://...")` com `AllowCredentials()`;
- `AllowAnyOrigin()` sem credenciais quando a opcao rigida nao esta habilitada;
- codigo dentro de contexto de teste xUnit, NUnit ou MSTest reconhecido pelo projeto;
- metodos customizados com os mesmos nomes fora de `CorsPolicyBuilder`;
- codigo gerado, seguindo a configuracao padrao dos analyzers do projeto.

## Limitacoes conhecidas

- A regra analisa apenas cadeias fluentemente encadeadas no codigo fonte. Ela nao infere chamadas separadas por variaveis intermediarias ou metodos auxiliares.
- Configuracoes aplicadas dinamicamente por delegates, reflection ou extensoes customizadas nao sao expandidas nesta versao.
- A opcao `disallow_any_origin` e booleana: apenas `true` habilita a politica mais restritiva.

## Impacto esperado

- Menos risco de expor endpoints autenticados para origens amplas demais.
- Politicas CORS com credenciais ficam mais faceis de auditar.
- Times que exigem allowlist de origem podem reforcar essa decisao por `.editorconfig`.
