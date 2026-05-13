# Adocao gradual do Swa.Analyzers

Este guia descreve uma estrategia segura para introduzir o `Swa.Analyzers` em projetos reais, inclusive bases legadas com muitos diagnosticos iniciais. A ideia principal e separar aprendizado, priorizacao e bloqueio: primeiro tornar os problemas visiveis, depois escolher regras criticas e so entao exigir conformidade no CI.

## Principios

- Comece com severidades baixas para medir impacto sem interromper entregas.
- Promova poucas regras por vez, preferindo as mais objetivas e maduras.
- Trate legado como backlog tecnico explicito, nao como excecao permanente.
- Use suppressions para casos justificados e locais; evite desabilitar regras amplamente.
- Documente a decisao quando uma violacao for aceita por compatibilidade, contrato externo ou risco de mudanca.

## Configurando severidades

As severidades podem ser controladas por `.editorconfig` usando o ID do diagnostico:

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.ARCH001.severity = info
dotnet_diagnostic.ARCH009.severity = warning
dotnet_diagnostic.ARCH020.severity = error
```

Valores comuns:

- `none`: desabilita o diagnostico.
- `silent`: mantem o diagnostico para tooling, sem aparecer como aviso.
- `suggestion`: sugere no IDE com baixo ruido.
- `info`: mostra informacao sem bloquear build.
- `warning`: aparece como aviso e pode bloquear quando warnings sao tratados como erro.
- `error`: bloqueia compilacao.

Para adocao gradual, prefira `suggestion` ou `info` no primeiro ciclo. Regras arquiteturais costumam refletir convencoes de time e podem comecar como `info` antes de virarem `warning` ou `error`.

Se o time quiser partir de uma politica pronta, veja os [perfis de adocao via `.editorconfig`](editorconfig-profiles.md). Eles cobrem os perfis `recommended`, `strict`, `security`, `architecture`, `testing` e `legacy-safe`, com exemplos copiaveis e um mapa de risco de ruido por regra.

## Estrategia por fases

### 1. Modo informativo

Use esta fase para descobrir o tamanho do trabalho, entender falsos positivos e alinhar convencoes do time.

```ini
[*.cs]
dotnet_diagnostic.ARCH001.severity = info
dotnet_diagnostic.ARCH009.severity = info
dotnet_diagnostic.ARCH010.severity = info
dotnet_diagnostic.ARCH020.severity = info
dotnet_diagnostic.ARCH027.severity = info
```

Nesta fase, nao bloqueie o CI por causa dos novos diagnosticos. Gere uma lista das regras mais frequentes, separe problemas reais de casos aceitos e ajuste configuracoes publicas das regras quando houver suporte documentado.

### 2. Warnings em regras criticas

Depois que o time conhece o impacto, promova regras de baixo falso positivo e alto risco operacional para `warning`.

```ini
[*.cs]
dotnet_diagnostic.ARCH001.severity = warning
dotnet_diagnostic.ARCH009.severity = warning
dotnet_diagnostic.ARCH010.severity = warning
dotnet_diagnostic.ARCH017.severity = warning
dotnet_diagnostic.ARCH020.severity = warning
```

Boas candidatas costumam ser regras ligadas a confiabilidade, seguranca, async, autorizacao e observabilidade. Regras mais opinativas ou dependentes da arquitetura local podem continuar como `info` ate amadurecerem no contexto do projeto.

### 3. Bloqueio em CI apenas para regras maduras

Promova para `error` somente regras que o time ja validou como maduras para aquela base: baixo ruido, entendimento comum e plano claro para novas violacoes.

```ini
[*.cs]
dotnet_diagnostic.ARCH001.severity = error
dotnet_diagnostic.ARCH009.severity = error
dotnet_diagnostic.ARCH020.severity = error

# Regras ainda em amadurecimento continuam visiveis sem bloquear.
dotnet_diagnostic.ARCH027.severity = info
dotnet_diagnostic.ARCH030.severity = info
```

Evite transformar todas as regras em erro de uma vez. Isso tende a criar suppressions amplas e reduz a confianca no analyzer. O melhor bloqueio e incremental: poucas regras, alto valor, comportamento bem compreendido.

### 4. Tratamento de legado

Em projetos legados, separe codigo novo de codigo historico. Um padrao comum e manter regras fortes para areas novas e reduzir severidade em pastas legadas ate que sejam corrigidas.

```ini
[*.cs]
dotnet_diagnostic.ARCH001.severity = warning
dotnet_diagnostic.ARCH009.severity = warning
dotnet_diagnostic.ARCH020.severity = warning

[Legacy/**/*.cs]
dotnet_diagnostic.ARCH001.severity = info
dotnet_diagnostic.ARCH009.severity = info
dotnet_diagnostic.ARCH020.severity = none
```

Use `none` com cuidado. Ele e aceitavel quando uma area nao sera migrada no curto prazo ou quando ha incompatibilidade conhecida, mas prefira `info` quando o diagnostico ainda deve aparecer em revisoes e relatorios.

## Suppressions locais

Suppressions devem explicar excecoes reais, nao esconder trabalho comum. Bons motivos incluem:

- compatibilidade com API publica existente;
- assinatura exigida por framework ou biblioteca externa;
- codigo gerado ou adaptador temporario;
- falso positivo conhecido enquanto a regra nao cobre o caso;
- migracao planejada em uma area legada.

Evite suppression quando o codigo pode ser ajustado de forma simples, quando a regra aponta risco de producao ou quando a justificativa e apenas "para o build passar".

### `#pragma warning disable/restore`

Use `#pragma` para excecoes pequenas e proximas do codigo. Sempre limite o escopo e inclua um comentario curto quando a justificativa nao for obvia.

```csharp
#pragma warning disable ARCH001 // Assinatura exigida por componente legado.
public async void PublishAsync()
{
    await handler.HandleAsync();
}
#pragma warning restore ARCH001
```

Evite deixar `#pragma warning disable` aberto por muitas linhas ou por um arquivo inteiro. Quanto menor o escopo, mais facil revisar a excecao depois.

### `GlobalSuppressions.cs`

Use `GlobalSuppressions.cs` quando a excecao precisa ficar centralizada ou quando o alvo e um membro especifico que nao deve carregar pragmas no corpo do arquivo.

```csharp
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Reliability",
    "ARCH001:Avoid async void outside event handlers",
    Justification = "Contrato publico legado; migracao planejada no proximo ciclo.",
    Scope = "member",
    Target = "~M:Legacy.Notifier.PublishAsync")]
```

Prefira suppressions com `Scope` e `Target` quando possivel. Suppressions globais sem alvo tornam mais dificil saber qual excecao ainda e valida.

### `NoWarn` no csproj

Use `NoWarn` apenas quando a decisao precisa ser aplicada ao projeto inteiro, por exemplo em projeto de testes, amostra, codigo gerado ou pacote legado que ainda nao participa da politica.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <NoWarn>$(NoWarn);ARCH003;ARCH004</NoWarn>
  </PropertyGroup>
</Project>
```

Evite `NoWarn` para regras criticas em projetos de producao. Se a excecao vale para poucas linhas, prefira `#pragma`; se vale para poucos membros, prefira `GlobalSuppressions.cs`; se vale para uma pasta, prefira escopo por `.editorconfig`.

## Exemplo de politica inicial

Este exemplo combina adocao gradual, regras criticas e tratamento de legado:

```ini
root = true

[*.cs]
# Primeiro ciclo: regras visiveis para todo o time.
dotnet_diagnostic.ARCH003.severity = info
dotnet_diagnostic.ARCH004.severity = info
dotnet_diagnostic.ARCH027.severity = info

# Regras de confiabilidade e seguranca ja priorizadas.
dotnet_diagnostic.ARCH001.severity = warning
dotnet_diagnostic.ARCH009.severity = warning
dotnet_diagnostic.ARCH010.severity = warning
dotnet_diagnostic.ARCH020.severity = warning

[src/NewModules/**/*.cs]
# Codigo novo segue politica mais forte.
dotnet_diagnostic.ARCH001.severity = error
dotnet_diagnostic.ARCH009.severity = error
dotnet_diagnostic.ARCH020.severity = error

[src/Legacy/**/*.cs]
# Legado permanece visivel, mas nao bloqueia enquanto e migrado.
dotnet_diagnostic.ARCH001.severity = info
dotnet_diagnostic.ARCH009.severity = info
dotnet_diagnostic.ARCH020.severity = info
```

## Evoluindo a politica

Revise a configuracao periodicamente. Quando uma regra passar algumas iteracoes sem falsos positivos relevantes e com correcoes bem compreendidas, promova a severidade:

```ini
[*.cs]
# Antes
dotnet_diagnostic.ARCH027.severity = info

# Depois
dotnet_diagnostic.ARCH027.severity = warning
```

Antes de promover uma regra para `error`, confirme que:

- a regra esta documentada e entendida pelo time;
- os falsos positivos conhecidos foram corrigidos ou suprimidos com justificativa;
- o legado tem plano explicito;
- o CI falha apenas para violacoes que o time realmente quer bloquear.

Essa progressao mantem o analyzer util desde o primeiro dia e evita que a adocao vire uma mudanca grande demais para ser sustentada.
