namespace Swa.Analyzers.SampleApp.Arch032;

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
    // Quando estes arquivos chegam como AdditionalFiles, ARCH032 reporta a
    // propriedade repetida no .csproj.
}
