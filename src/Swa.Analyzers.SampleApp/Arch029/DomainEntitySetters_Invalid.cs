namespace Swa.Analyzers.SampleApp.Arch029.Domain.Entities;

public abstract class Entity
{
}

public sealed class Customer : Entity
{
    // ARCH029: entidades de domínio devem proteger invariantes em vez de expor setters públicos.
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
