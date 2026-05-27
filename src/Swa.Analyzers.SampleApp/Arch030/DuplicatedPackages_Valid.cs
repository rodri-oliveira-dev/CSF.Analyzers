namespace Swa.Analyzers.SampleApp.Arch030;

internal static class DuplicatedPackagesValid
{
    // Exemplo documental:
    //
    // MyApp.Infrastructure.csproj
    // <PackageReference Include="Serilog" />
    //
    // MyApp.Application.csproj
    // <ProjectReference Include="..\MyApp.Abstractions\MyApp.Abstractions.csproj" />
    //
    // A dependência fica no projeto que realmente a consome.
}
