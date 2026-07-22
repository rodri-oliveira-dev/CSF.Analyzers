namespace Swa.Analyzers.Common;

internal static class RuleHelpLinks
{
    private const string RulesBaseUrl = "https://github.com/rodri-oliveira-dev/Swa.Analyzers/blob/main/docs/rules/";

    public static string ForRule(string ruleId) => RulesBaseUrl + ruleId + ".md";
}
