namespace Swa.Analyzers.SampleApp.Arch028;

public record RecordMutablePropertiesInvalid
{
    // ARCH028: records should prefer init-only or immutable state.
    public string Name { get; set; } = "";

    public int Age
    {
        get;
        set;
    }
}
