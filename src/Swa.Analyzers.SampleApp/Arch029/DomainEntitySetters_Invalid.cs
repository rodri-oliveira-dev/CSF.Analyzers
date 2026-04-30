namespace Swa.Analyzers.SampleApp.Arch029.Domain.Entities;

public abstract class Entity
{
}

public sealed class Customer : Entity
{
    // ARCH029: domain entities should protect invariants instead of exposing public setters.
    public string Name { get; set; } = "";
}

public sealed class Order
{
    public decimal Amount
    {
        get;
        set;
    }
}
