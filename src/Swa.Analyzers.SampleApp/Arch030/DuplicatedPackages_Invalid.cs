namespace Swa.Analyzers.SampleApp.Arch030;

internal static class DuplicatedPackagesInvalid
{
    // Exemplo documental:
    //
    // MyApp.Domain.csproj
    // <PackageReference Include="Serilog" />
    //
    // MyApp.Application.csproj
    // <PackageReference Include="Serilog" />
    //
    // Quando estes .csproj chegam como AdditionalFiles, ARCH030 reporta o
    // pacote repetido para revisao.
}
