# Adocao gradual do Swa.Analyzers

Este guia descreve uma estratÃ©gia segura para introduzir o `Swa.Analyzers` em projetos reais, inclusive bases legadas com muitos diagnÃ³sticos iniciais. A ideia principal Ã© separar aprendizado, priorizaÃ§Ã£o e bloqueio: primeiro tornar os problemas visÃ­veis, depois escolher regras crÃ­ticas e so entao exigir conformidade no CI.

## Principios

- Comece com severidades baixas para medir impacto sem interromper entregas.
- Promova poucas regras por vez, preferindo as mais objetivas e maduras.
- Trate legado como backlog tecnico explÃ­cito, nÃ£o como exceÃ§Ã£o permanente.
- Use suppressions para casos justificados e locais; evite desabilitar regras amplamente.
- Documente a decisÃ£o quando uma violacao for aceita por compatibilidade, contrato externo ou risco de mudanÃ§a.

## Configurando severidades

As severidades podem ser controladas por `.editorconfig` usando o ID do diagnÃ³stico:

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.ARCH020.severity = error
```

Valores comuns:

- `none`: desabilita o diagnÃ³stico.
- `silent`: mantÃ©m o diagnÃ³stico para tooling, sem aparecer como aviso.
- `suggestion`: sugere no IDE com baixo ruÃ­do.
- `info`: mostra informacao sem bloquear build.
- `warning`: aparece como aviso e pode bloquear quando warnings sÃ£o tratados como erro.
- `error`: bloqueia compilaÃ§Ã£o.

Para adoÃ§Ã£o gradual, prefira `suggestion` ou `info` no primeiro ciclo. Regras arquiteturais costumam refletir convenÃ§Ãµes de time e podem comeÃ§ar como `info` antes de virarem `warning` ou `error`.

Se o time quiser partir de uma polÃ­tica pronta, veja os [perfis de adoÃ§Ã£o via `.editorconfig`](editorconfig-profiles.md). Eles cobrem os perfis `recommended`, `strict`, `security`, `architecture`, `testing` e `legacy-safe`, com exemplos copiÃ¡veis e um mapa de risco de ruÃ­do por regra.

## Estrategia por fases

### 1. Modo informativo

Use esta fase para descobrir o tamanho do trabalho, entender falsos positivos e alinhar convenÃ§Ãµes do time.

```ini
[*.cs]
dotnet_diagnostic.ARCH020.severity = info
dotnet_diagnostic.ARCH027.severity = info
```

Nesta fase, nÃ£o bloqueie o CI por causa dos novos diagnÃ³sticos. Gere uma lista das regras mais frequentes, separe problemas reais de casos aceitos e ajuste configuraÃ§Ãµes pÃºblicas das regras quando houver suporte documentado.

### 2. Warnings em regras crÃ­ticas

Depois que o time conhece o impacto, promova regras de baixo falso positivo e alto risco operacional para `warning`.

```ini
[*.cs]
dotnet_diagnostic.ARCH017.severity = warning
dotnet_diagnostic.ARCH020.severity = warning
```

Boas candidatas costumam ser regras ligadas a confiabilidade, seguranÃ§a, async, autorizaÃ§Ã£o e observabilidade. Regras mais opinativas ou dependentes da arquitetura local podem continuar como `info` atÃ© amadurecerem no contexto do projeto.

### 3. Bloqueio em CI apenas para regras maduras

Promova para `error` somente regras que o time jÃ¡ validou como maduras para aquela base: baixo ruÃ­do, entendimento comum e plano claro para novas violaÃ§Ãµes.

```ini
[*.cs]
dotnet_diagnostic.ARCH020.severity = error

# Regras ainda em amadurecimento continuam visÃ­veis sem bloquear.
dotnet_diagnostic.ARCH027.severity = info
```

Evite transformar todas as regras em erro de uma vez. Isso tende a criar suppressions amplas e reduz a confianÃ§a no analyzer. O melhor bloqueio Ã© incremental: poucas regras, alto valor, comportamento bem compreendido.

### 4. Tratamento de legado

Em projetos legados, separe cÃ³digo novo de cÃ³digo histÃ³rico. Um padrÃ£o comum Ã© manter regras fortes para Ã¡reas novas e reduzir severidade em pastas legadas atÃ© que sejam corrigidas.

```ini
[*.cs]
dotnet_diagnostic.ARCH020.severity = warning

[Legacy/**/*.cs]
dotnet_diagnostic.ARCH020.severity = none
```

Use `none` com cuidado. Ele Ã© aceitÃ¡vel quando uma Ã¡rea nÃ£o serÃ¡ migrada no curto prazo ou quando hÃ¡ incompatibilidade conhecida, mas prefira `info` quando o diagnÃ³stico ainda deve aparecer em revisÃµes e relatÃ³rios.

## Suppressions locais

Suppressions devem explicar exceÃ§Ãµes reais, nÃ£o esconder trabalho comum. Bons motivos incluem:

- compatibilidade com API pÃºblica existente;
- assinatura exigida por framework ou biblioteca externa;
- cÃ³digo gerado ou adaptador temporario;
- falso positivo conhecido enquanto a regra nÃ£o cobre o caso;
- migraÃ§Ã£o planejada em uma Ã¡rea legada.

Evite suppression quando o cÃ³digo pode ser ajustado de forma simples, quando a regra aponta risco de produÃ§Ã£o ou quando a justificativa Ã© apenas "para o build passar".

### `#pragma warning disable/restore`

Use `#pragma` para exceÃ§Ãµes pequenas e prÃ³ximas do cÃ³digo. Sempre limite o escopo e inclua um comentÃ¡rio curto quando a justificativa nÃ£o for Ã³bvia.

```csharp
public async void PublishAsync()
{
    await handler.HandleAsync();
}
```

Evite deixar `#pragma warning disable` aberto por muitas linhas ou por um arquivo inteiro. Quanto menor o escopo, mais facil revisar a exceÃ§Ã£o depois.

### `GlobalSuppressions.cs`

Use `GlobalSuppressions.cs` quando a exceÃ§Ã£o precisa ficar centralizada ou quando o alvo Ã© um membro especÃ­fico que nÃ£o deve carregar pragmas no corpo do arquivo.

```csharp
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Reliability",
    Justification = "Contrato pÃºblico legado; migraÃ§Ã£o planejada no prÃ³ximo ciclo.",
    Scope = "member",
    Target = "~M:Legacy.Notifier.PublishAsync")]
```

Prefira suppressions com `Scope` e `Target` quando possÃ­vel. Suppressions globais sem alvo tornam mais difÃ­cil saber qual exceÃ§Ã£o ainda Ã© vÃ¡lida.

### `NoWarn` no csproj

Use `NoWarn` apenas quando a decisÃ£o precisa ser aplicada ao projeto inteiro, por exemplo em projeto de testes, amostra, cÃ³digo gerado ou pacote legado que ainda nÃ£o participa da polÃ­tica.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
  </PropertyGroup>
</Project>
```

Evite `NoWarn` para regras crÃ­ticas em projetos de produÃ§Ã£o. Se a exceÃ§Ã£o vale para poucas linhas, prefira `#pragma`; se vale para poucos membros, prefira `GlobalSuppressions.cs`; se vale para uma pasta, prefira escopo por `.editorconfig`.

## Exemplo de polÃ­tica inicial

Este exemplo combina adoÃ§Ã£o gradual, regras crÃ­ticas e tratamento de legado:

```ini
root = true

[*.cs]
# Primeiro ciclo: regras visÃ­veis para todo o time.
dotnet_diagnostic.ARCH027.severity = info

# Regras de confiabilidade e seguranÃ§a jÃ¡ priorizadas.
dotnet_diagnostic.ARCH020.severity = warning

[src/NewModules/**/*.cs]
# CÃ³digo novo segue polÃ­tica mais forte.
dotnet_diagnostic.ARCH020.severity = error

[src/Legacy/**/*.cs]
# Legado permanece visÃ­vel, mas nÃ£o bloqueia enquanto Ã© migrado.
dotnet_diagnostic.ARCH020.severity = info
```

## Evoluindo a polÃ­tica

Revise a configuraÃ§Ã£o periodicamente. Quando uma regra passar algumas iteraÃ§Ãµes sem falsos positivos relevantes e com correÃ§Ãµes bem compreendidas, promova a severidade:

```ini
[*.cs]
# Antes
dotnet_diagnostic.ARCH027.severity = info

# Depois
dotnet_diagnostic.ARCH027.severity = warning
```

Antes de promover uma regra para `error`, confirme que:

- a regra estÃ¡ documentada e entendida pelo time;
- os falsos positivos conhecidos foram corrigidos ou suprimidos com justificativa;
- o legado tem plano explÃ­cito;
- o CI falha apenas para violaÃ§Ãµes que o time realmente quer bloquear.

Essa progressÃ£o mantÃ©m o analyzer Ãºtil desde o primeiro dia e evita que a adoÃ§Ã£o vire uma mudanÃ§a grande demais para ser sustentada.
