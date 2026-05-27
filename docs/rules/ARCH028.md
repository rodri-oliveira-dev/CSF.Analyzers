# ARCH028: Proiba propriedades mutáveis em records

## Objetivo

Detectar propriedades com `set` mutável em `record`, `record class`, `record struct` e `readonly record struct`.

Records normalmente representam estado imutável, eventos, comandos, respostas ou modelos de transporte. Um setter mutável permite alteração depois da criação do objeto e reduz a previsibilidade do código que consome esse valor.

## Código não conforme

```csharp
public record Customer
{
    public string Name { get; set; } = "";
}
```

Também há diagnóstico para propriedades `required` quando o setter continua mutável:

```csharp
public record Customer
{
    public required string Name { get; set; }
}
```

## Código conforme

Use construtor primário quando o valor faz parte do estado principal do record:

```csharp
public record Customer(string Name);
```

Use `init` quando a propriedade precisa ser atribuida por inicializador:

```csharp
public record Customer
{
    public string Name { get; init; } = "";
}
```

Use propriedade somente leitura quando o valor é calculado ou definido internamente:

```csharp
public record Customer
{
    public string Name { get; } = "";
}
```

Por padrão, setters não públicos são permitidos:

```csharp
public record Customer
{
    public string Name { get; private set; } = "";
}
```

## Configuração

A regra aceita a opção abaixo em `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.ARCH028.allow_non_public_setters = true
```

O valor padrão e `true`. Com esse valor, `private set`, `protected set`, `internal set` e `protected internal set` não reportam diagnóstico.

Quando configurado como `false`, qualquer setter explícito em record reporta diagnóstico, exceto `init`.

```ini
[*.cs]
dotnet_diagnostic.ARCH028.allow_non_public_setters = false
```

Valores inválidos usam o padrão `true`.

### Fallback das opções

- `allow_non_public_setters`: booleano; default `true`. Valores booleanos aceitam casing variado; valor ausente, vazio ou inválido usa `true`.

O fallback é permissivo para setters não públicos, preservando o comportamento padrão; setters públicos continuam sendo reportados.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH028.severity = warning
```

## Heurística

O analyzer registra propriedades (`PropertyDeclaration`) e verifica o tipo mais próximo que contém a propriedade.

A regra reporta quando:

- o tipo mais próximo é um record;
- a propriedade declara accessor `set`;
- o setter não é `init`;
- o setter é público/sem modificador ou `allow_non_public_setters = false`.

Propriedades geradas pelo construtor primário do record não aparecem como declarações de propriedade no código fonte e, por isso, não são reportadas.

## Limitações conhecidas

- A regra analisa declarações sintaticas de propriedades. Ela não tenta inferir mutabilidade por métodos que alteram campos internos.
- Um setter sem modificador em propriedade não pública ainda é reportado, porque representa mutabilidade explícita dentro do record.
- Setters com modificadores inválidos para o contexto ainda podem ser reportados pela sintaxe antes do erro de compilação do C#.

## Impacto esperado

- Incentiva records com estado previsível e imutável.
- Evita alterações acidentais após a criação de objetos de transporte.
- Torna exceções para setters não públicos explicitas via `.editorconfig`.
