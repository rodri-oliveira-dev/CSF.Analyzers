namespace CSF.Analyzers.SampleApp.Arc004.Domain.Entities;

public abstract class Entity
{
}

public sealed class Customer : Entity
{
    // ARC004: entidades de domínio devem proteger invariantes em vez de expor setters públicos.
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
