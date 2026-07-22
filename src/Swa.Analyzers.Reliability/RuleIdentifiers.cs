namespace Swa.Analyzers.Reliability;

internal static class RuleIdentifiers
{
    public const string AvoidTaskRunInAspNetRequestFlow = "ARCH016";
    public const string ProhibitFireAndForgetInRequestFlow = "ARCH017";
    public const string PreferAsNoTrackingForReadOnlyQueries = "ARCH021";
    public const string AvoidPrematureQueryMaterialization = "ARCH022";
}
