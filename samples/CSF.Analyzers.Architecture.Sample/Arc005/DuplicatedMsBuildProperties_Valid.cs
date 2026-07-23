namespace CSF.Analyzers.SampleApp.Arc005;

internal static class DuplicatedMsBuildPropertiesValid
{
    // Exemplo documental:
    //
    // Directory.Build.props
    // <Nullable>enable</Nullable>
    // <ImplicitUsings>enable</ImplicitUsings>
    //
    // MyApp.csproj
    // <OutputType>Exe</OutputType>
    //
    // O projeto mantém apenas propriedades específicas dele.
}
