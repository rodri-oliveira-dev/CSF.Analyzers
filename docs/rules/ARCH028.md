# ARCH028: Proiba propriedades mutaveis em records

## Objetivo

Detectar propriedades com `set` mutavel em `record`, `record class`, `record struct` e `readonly record struct`.

Records normalmente representam estado imutavel, eventos, comandos, respostas ou modelos de transporte. Um setter mutavel permite alteracao depois da criacao do objeto e reduz a previsibilidade do codigo que consome esse valor.

## Codigo nao conforme

```csharp
public record Customer
{
    public string Name { get; set; } = "";
}
```

Tambem ha diagnostico para propriedades `required` quando o setter continua mutavel:

```csharp
public record Customer
{
    public required string Name { get; set; }
}
```

## Codigo conforme

Use construtor primario quando o valor faz parte do estado principal do record:

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

Use propriedade somente leitura quando o valor e calculado ou definido internamente:

```csharp
public record Customer
{
    public string Name { get; } = "";
}
```

Por padrao, setters nao publicos sao permitidos:

```csharp
public record Customer
{
    public string Name { get; private set; } = "";
}
```

## Configuracao

A regra aceita a opcao abaixo em `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.ARCH028.allow_non_public_setters = true
```

O valor padrao e `true`. Com esse valor, `private set`, `protected set`, `internal set` e `protected internal set` nao reportam diagnostico.

Quando configurado como `false`, qualquer setter explicito em record reporta diagnostico, exceto `init`.

```ini
[*.cs]
dotnet_diagnostic.ARCH028.allow_non_public_setters = false
```

Valores invalidos usam o padrao `true`.

### Fallback das opcoes

- `allow_non_public_setters`: booleano; default `true`. Valores booleanos aceitam casing variado; valor ausente, vazio ou invalido usa `true`.

O fallback e permissivo para setters nao publicos, preservando o comportamento padrao; setters publicos continuam sendo reportados.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH028.severity = warning
```

## Heuristica

O analyzer registra propriedades (`PropertyDeclaration`) e verifica o tipo mais proximo que contem a propriedade.

A regra reporta quando:

- o tipo mais proximo e um record;
- a propriedade declara accessor `set`;
- o setter nao e `init`;
- o setter e publico/sem modificador ou `allow_non_public_setters = false`.

Propriedades geradas pelo construtor primario do record nao aparecem como declaracoes de propriedade no codigo fonte e, por isso, nao sao reportadas.

## Limitacoes conhecidas

- A regra analisa declaracoes sintaticas de propriedades. Ela nao tenta inferir mutabilidade por metodos que alteram campos internos.
- Um setter sem modificador em propriedade nao publica ainda e reportado, porque representa mutabilidade explicita dentro do record.
- Setters com modificadores invalidos para o contexto ainda podem ser reportados pela sintaxe antes do erro de compilacao do C#.

## Impacto esperado

- Incentiva records com estado previsivel e imutavel.
- Evita alteracoes acidentais apos a criacao de objetos de transporte.
- Torna excecoes para setters nao publicos explicitas via `.editorconfig`.
