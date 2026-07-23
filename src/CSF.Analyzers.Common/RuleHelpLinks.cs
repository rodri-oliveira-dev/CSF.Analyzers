namespace CSF.Analyzers.Common;

internal static class RuleHelpLinks
{
    private const string RulesBaseUrl = "https://github.com/rodri-oliveira-dev/CSF.Analyzers/blob/main/docs/rules/";

    public static string ForRule(string ruleId) => RulesBaseUrl + GetRuleGroup(ruleId) + "/" + ruleId + ".md";

    private static string GetRuleGroup(string ruleId)
    {
        if (ruleId.StartsWith("REL", StringComparison.Ordinal))
        {
            return "reliability";
        }

        if (ruleId.StartsWith("ARC", StringComparison.Ordinal))
        {
            return "architecture";
        }

        if (ruleId.StartsWith("TST", StringComparison.Ordinal))
        {
            return "testing";
        }

        return string.Empty;
    }
}
