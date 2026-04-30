namespace Swa.Analyzers.SampleApp.Arch028;

public record CustomerSnapshot(string Name, int Age);

public record CustomerWithInit
{
    public string Name { get; init; } = "";
}

public record CustomerWithReadOnlyProperty
{
    public string Name { get; } = "";
}

public record CustomerWithPrivateSetter
{
    public string Name { get; private set; } = "";
}
