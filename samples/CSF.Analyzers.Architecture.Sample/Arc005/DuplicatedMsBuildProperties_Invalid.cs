namespace CSF.Analyzers.SampleApp.Arc005;

internal static class DuplicatedMsBuildPropertiesInvalid
{
    // Exemplo documental:
    //
    // Directory.Build.props
    // <Nullable>enable</Nullable>
    //
    // MyApp.csproj
    // <Nullable>enable</Nullable>
    //
    // Quando estes arquivos chegam como AdditionalFiles, ARC005 reporta a
    // propriedade repetida no .csproj.
}
