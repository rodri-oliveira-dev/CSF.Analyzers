namespace Swa.Analyzers.Architecture;

internal static class RuleIdentifiers
{
    public const string ProhibitVerbsInHttpRoutes = "ARCH015";
    public const string RequireExplicitAuthorizationOnHttpEndpoints = "ARCH020";
    public const string PreventInfrastructureDependenciesInCoreLayers = "ARCH027";
    public const string ProhibitPublicSettersInDomainEntities = "ARCH029";
    public const string AvoidDuplicatedMsBuildProperties = "ARCH032";
}
