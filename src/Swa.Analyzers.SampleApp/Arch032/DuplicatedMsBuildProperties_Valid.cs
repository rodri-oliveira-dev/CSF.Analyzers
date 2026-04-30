namespace Swa.Analyzers.SampleApp.Arch032;

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
    // O projeto mantem apenas propriedades especificas dele.
}
