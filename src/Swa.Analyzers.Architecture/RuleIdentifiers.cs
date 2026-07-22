namespace Swa.Analyzers.Architecture;

internal static class RuleIdentifiers
{
    public const string ProhibitVerbsInHttpRoutes = "ARC003";
    public const string RequireExplicitAuthorizationOnHttpEndpoints = "ARC001";
    public const string PreventInfrastructureDependenciesInCoreLayers = "ARC002";
    public const string ProhibitPublicSettersInDomainEntities = "ARC004";
    public const string AvoidDuplicatedMsBuildProperties = "ARC005";
}
