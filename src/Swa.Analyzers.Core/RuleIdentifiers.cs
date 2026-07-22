namespace Swa.Analyzers.Core;

internal static class RuleIdentifiers
{
    public const string RestrictArgAnyUsage = "ARCH005";
    public const string WarnOnExcludingInBeEquivalentTo = "ARCH006";
    public const string ProhibitVerbsInHttpRoutes = "ARCH015";
    public const string AvoidTaskRunInAspNetRequestFlow = "ARCH016";
    public const string ProhibitFireAndForgetInRequestFlow = "ARCH017";
    public const string RequireExplicitAuthorizationOnHttpEndpoints = "ARCH020";
    public const string PreferAsNoTrackingForReadOnlyQueries = "ARCH021";
    public const string AvoidPrematureQueryMaterialization = "ARCH022";
    public const string PreventInfrastructureDependenciesInCoreLayers = "ARCH027";
    public const string ProhibitPublicSettersInDomainEntities = "ARCH029";
    public const string AvoidDuplicatedMsBuildProperties = "ARCH032";
}
