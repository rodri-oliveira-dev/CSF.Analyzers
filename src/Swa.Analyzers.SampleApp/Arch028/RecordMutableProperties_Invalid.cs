namespace Swa.Analyzers.SampleApp.Arch028;

public record RecordMutablePropertiesInvalid
{
    // ARCH028: records devem preferir estado imutável ou init-only.
    public string Name { get; set; } = "";

    public int Age
    {
        get;
        set;
    }
}
