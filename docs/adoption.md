# Adocao gradual do Swa.Analyzers

Este guia descreve uma estratégia segura para introduzir o `Swa.Analyzers` em projetos reais, inclusive bases legadas com muitos diagnósticos iniciais. A ideia principal é separar aprendizado, priorização e bloqueio: primeiro tornar os problemas visíveis, depois escolher regras críticas e so entao exigir conformidade no CI.

## Principios

- Comece com severidades baixas para medir impacto sem interromper entregas.
- Promova poucas regras por vez, preferindo as mais objetivas e maduras.
- Trate legado como backlog tecnico explícito, não como exceção permanente.
- Use suppressions para casos justificados e locais; evite desabilitar regras amplamente.
- Documente a decisão quando uma violacao for aceita por compatibilidade, contrato externo ou risco de mudança.

## Configurando severidades

As severidades podem ser controladas por `.editorconfig` usando o ID do diagnóstico:

```ini
# .editorconfig
root = true

[*.cs]
dotnet_diagnostic.ARCH001.severity = info
dotnet_diagnostic.ARCH009.severity = warning
dotnet_diagnostic.ARCH020.severity = error
```

Valores comuns:

- `none`: desabilita o diagnóstico.
- `silent`: mantém o diagnóstico para tooling, sem aparecer como aviso.
- `suggestion`: sugere no IDE com baixo ruído.
- `info`: mostra informacao sem bloquear build.
- `warning`: aparece como aviso e pode bloquear quando warnings são tratados como erro.
- `error`: bloqueia compilação.

Para adoção gradual, prefira `suggestion` ou `info` no primeiro ciclo. Regras arquiteturais costumam refletir convenções de time e podem começar como `info` antes de virarem `warning` ou `error`.

Se o time quiser partir de uma política pronta, veja os [perfis de adoção via `.editorconfig`](editorconfig-profiles.md). Eles cobrem os perfis `recommended`, `strict`, `security`, `architecture`, `testing` e `legacy-safe`, com exemplos copiáveis e um mapa de risco de ruído por regra.

## Estrategia por fases

### 1. Modo informativo

Use esta fase para descobrir o tamanho do trabalho, entender falsos positivos e alinhar convenções do time.

```ini
[*.cs]
dotnet_diagnostic.ARCH001.severity = info
dotnet_diagnostic.ARCH009.severity = info
dotnet_diagnostic.ARCH010.severity = info
dotnet_diagnostic.ARCH020.severity = info
dotnet_diagnostic.ARCH027.severity = info
```

Nesta fase, não bloqueie o CI por causa dos novos diagnósticos. Gere uma lista das regras mais frequentes, separe problemas reais de casos aceitos e ajuste configurações públicas das regras quando houver suporte documentado.

### 2. Warnings em regras críticas

Depois que o time conhece o impacto, promova regras de baixo falso positivo e alto risco operacional para `warning`.

```ini
[*.cs]
dotnet_diagnostic.ARCH001.severity = warning
dotnet_diagnostic.ARCH009.severity = warning
dotnet_diagnostic.ARCH010.severity = warning
dotnet_diagnostic.ARCH017.severity = warning
dotnet_diagnostic.ARCH020.severity = warning
```

Boas candidatas costumam ser regras ligadas a confiabilidade, segurança, async, autorização e observabilidade. Regras mais opinativas ou dependentes da arquitetura local podem continuar como `info` até amadurecerem no contexto do projeto.

### 3. Bloqueio em CI apenas para regras maduras

Promova para `error` somente regras que o time já validou como maduras para aquela base: baixo ruído, entendimento comum e plano claro para novas violações.

```ini
[*.cs]
dotnet_diagnostic.ARCH001.severity = error
dotnet_diagnostic.ARCH009.severity = error
dotnet_diagnostic.ARCH020.severity = error

# Regras ainda em amadurecimento continuam visíveis sem bloquear.
dotnet_diagnostic.ARCH027.severity = info
dotnet_diagnostic.ARCH030.severity = info
```

Evite transformar todas as regras em erro de uma vez. Isso tende a criar suppressions amplas e reduz a confiança no analyzer. O melhor bloqueio é incremental: poucas regras, alto valor, comportamento bem compreendido.

### 4. Tratamento de legado

Em projetos legados, separe código novo de código histórico. Um padrão comum é manter regras fortes para áreas novas e reduzir severidade em pastas legadas até que sejam corrigidas.

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

Use `none` com cuidado. Ele é aceitável quando uma área não será migrada no curto prazo ou quando há incompatibilidade conhecida, mas prefira `info` quando o diagnóstico ainda deve aparecer em revisões e relatórios.

## Suppressions locais

Suppressions devem explicar exceções reais, não esconder trabalho comum. Bons motivos incluem:

- compatibilidade com API pública existente;
- assinatura exigida por framework ou biblioteca externa;
- código gerado ou adaptador temporario;
- falso positivo conhecido enquanto a regra não cobre o caso;
- migração planejada em uma área legada.

Evite suppression quando o código pode ser ajustado de forma simples, quando a regra aponta risco de produção ou quando a justificativa é apenas "para o build passar".

### `#pragma warning disable/restore`

Use `#pragma` para exceções pequenas e próximas do código. Sempre limite o escopo e inclua um comentário curto quando a justificativa não for óbvia.

```csharp
#pragma warning disable ARCH001 // Assinatura exigida por componente legado.
public async void PublishAsync()
{
    await handler.HandleAsync();
}
#pragma warning restore ARCH001
```

Evite deixar `#pragma warning disable` aberto por muitas linhas ou por um arquivo inteiro. Quanto menor o escopo, mais facil revisar a exceção depois.

### `GlobalSuppressions.cs`

Use `GlobalSuppressions.cs` quando a exceção precisa ficar centralizada ou quando o alvo é um membro específico que não deve carregar pragmas no corpo do arquivo.

```csharp
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Reliability",
    "ARCH001:Avoid async void outside event handlers",
    Justification = "Contrato público legado; migração planejada no próximo ciclo.",
    Scope = "member",
    Target = "~M:Legacy.Notifier.PublishAsync")]
```

Prefira suppressions com `Scope` e `Target` quando possível. Suppressions globais sem alvo tornam mais difícil saber qual exceção ainda é válida.

### `NoWarn` no csproj

Use `NoWarn` apenas quando a decisão precisa ser aplicada ao projeto inteiro, por exemplo em projeto de testes, amostra, código gerado ou pacote legado que ainda não participa da política.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <NoWarn>$(NoWarn);ARCH003;ARCH004</NoWarn>
  </PropertyGroup>
</Project>
```

Evite `NoWarn` para regras críticas em projetos de produção. Se a exceção vale para poucas linhas, prefira `#pragma`; se vale para poucos membros, prefira `GlobalSuppressions.cs`; se vale para uma pasta, prefira escopo por `.editorconfig`.

## Exemplo de política inicial

Este exemplo combina adoção gradual, regras críticas e tratamento de legado:

```ini
root = true

[*.cs]
# Primeiro ciclo: regras visíveis para todo o time.
dotnet_diagnostic.ARCH003.severity = info
dotnet_diagnostic.ARCH004.severity = info
dotnet_diagnostic.ARCH027.severity = info

# Regras de confiabilidade e segurança já priorizadas.
dotnet_diagnostic.ARCH001.severity = warning
dotnet_diagnostic.ARCH009.severity = warning
dotnet_diagnostic.ARCH010.severity = warning
dotnet_diagnostic.ARCH020.severity = warning

[src/NewModules/**/*.cs]
# Código novo segue política mais forte.
dotnet_diagnostic.ARCH001.severity = error
dotnet_diagnostic.ARCH009.severity = error
dotnet_diagnostic.ARCH020.severity = error

[src/Legacy/**/*.cs]
# Legado permanece visível, mas não bloqueia enquanto é migrado.
dotnet_diagnostic.ARCH001.severity = info
dotnet_diagnostic.ARCH009.severity = info
dotnet_diagnostic.ARCH020.severity = info
```

## Evoluindo a política

Revise a configuração periodicamente. Quando uma regra passar algumas iterações sem falsos positivos relevantes e com correções bem compreendidas, promova a severidade:

```ini
[*.cs]
# Antes
dotnet_diagnostic.ARCH027.severity = info

# Depois
dotnet_diagnostic.ARCH027.severity = warning
```

Antes de promover uma regra para `error`, confirme que:

- a regra está documentada e entendida pelo time;
- os falsos positivos conhecidos foram corrigidos ou suprimidos com justificativa;
- o legado tem plano explícito;
- o CI falha apenas para violações que o time realmente quer bloquear.

Essa progressão mantém o analyzer útil desde o primeiro dia e evita que a adoção vire uma mudança grande demais para ser sustentada.
