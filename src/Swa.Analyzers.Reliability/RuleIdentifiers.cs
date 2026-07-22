namespace Swa.Analyzers.Reliability;

internal static class RuleIdentifiers
{
    public const string AvoidTaskRunInAspNetRequestFlow = "REL001";
    public const string ProhibitFireAndForgetInRequestFlow = "REL002";
    public const string PreferAsNoTrackingForReadOnlyQueries = "REL003";
    public const string AvoidPrematureQueryMaterialization = "REL004";
}
