namespace Swa.Analyzers.SampleApp.Arch029.Domain.Entities;

public sealed class CustomerWithPrivateSetter
{
    public string Name { get; private set; } = "";

    public void Rename(string name)
    {
        Name = name;
    }
}

public sealed class CustomerWithReadOnlyProperty
{
    public string Name { get; } = "";
}

public sealed class CustomerWithInit
{
    public string Name { get; init; } = "";
}
