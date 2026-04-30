# ARCH029: Proiba setters publicos em entidades de dominio

## Objetivo

Detectar propriedades mutaveis em entidades de dominio quando elas expõem `public set` ou `internal set` nao autorizado.

Entidades de dominio devem proteger invariantes. Alterar estado livremente por setters publicos tende a espalhar regras de negocio para fora da entidade e favorece modelos anemicos.

## Codigo nao conforme

```csharp
namespace MyApp.Domain.Entities;

public sealed class Customer
{
    public string Name { get; set; } = "";
}
```

Tambem ha diagnostico quando a entidade e identificada por tipo base:

```csharp
public sealed class Order : Entity
{
    public decimal Amount { get; set; }
}
```

## Codigo conforme

Prefira setter privado e metodos de dominio para preservar invariantes:

```csharp
public sealed class Customer
{
    public string Name { get; private set; } = "";

    public void Rename(string name)
    {
        Name = name;
    }
}
```

Use propriedade somente leitura quando o valor e definido no construtor ou calculado:

```csharp
public sealed class Customer
{
    public string Name { get; }
}
```

`init` tambem nao e reportado:

```csharp
public sealed class Customer
{
    public string Name { get; init; } = "";
}
```

## Heuristica de entidade

A regra e conservadora. Uma classe e considerada entidade quando pelo menos uma das condicoes abaixo for verdadeira:

- o namespace contem `.Domain.Entities`, `.Domain.Entity`, `.Domain.Aggregates` ou `.Domain.Aggregate`;
- algum tipo base se chama `Entity`, `Entity<T>`, `AggregateRoot` ou `AggregateRoot<T>`;
- alguma interface implementada se chama `IEntity`, `IEntity<T>`, `IAggregateRoot` ou `IAggregateRoot<T>`;
- o namespace corresponde a um prefixo configurado em `.editorconfig`;
- algum tipo base ou interface corresponde a um nome configurado em `.editorconfig`.

A regra ignora records, structs, propriedades estaticas, propriedades sem setter, `private set`, `protected set`, `init`, classes fora da heuristica de entidade e classes de teste.

## Configuracao

A regra aceita as opcoes abaixo em `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.ARCH029.entity_namespaces = ["MyApp.Domain.Entities", "MyApp.Domain.Aggregates"]
dotnet_diagnostic.ARCH029.entity_base_types = ["Entity", "AggregateRoot"]
dotnet_diagnostic.ARCH029.allow_internal_setters = false
```

`entity_namespaces` adiciona namespaces considerados dominio. O valor configurado vale para o namespace exato e para namespaces filhos.

`entity_base_types` adiciona nomes de classes base ou interfaces que identificam entidades. Use o nome simples do tipo, sem namespace e sem aridade generica.

`allow_internal_setters` controla `internal set` e `protected internal set`. O valor padrao e `false`, portanto esses setters reportam diagnostico. Quando configurado como `true`, eles sao aceitos.

Arrays JSON invalidos sao ignorados e os padroes continuam em uso. Valores booleanos invalidos usam o padrao `false`.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH029.severity = warning
```

## Limitacoes conhecidas

- A regra usa nomes simples para classes base e interfaces, sem validar o namespace do tipo.
- A regra nao tenta inferir entidades por atributos, campos `Id` ou convencoes de nome como `Customer`.
- A regra nao analisa mutabilidade indireta por campos, colecoes ou metodos que alteram estado interno.
- DTOs fora da heuristica de entidade nao sao reportados, mesmo que tenham setters publicos.

## Impacto esperado

- Reduz alteracoes livres no estado de entidades.
- Incentiva construtores, setters privados e metodos de comportamento.
- Mantem baixo ruido ao restringir diagnosticos a tipos com sinais claros de entidade de dominio.
