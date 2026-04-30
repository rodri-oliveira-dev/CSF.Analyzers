using System.Collections.Immutable;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch030AvoidDuplicatedPackageReferencesAcrossProjectsAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Maintainability";
    private const string AllowedPackagesOption = "dotnet_diagnostic.ARCH030.allowed_packages";
    private const string AllowedProjectPatternsOption = "dotnet_diagnostic.ARCH030.allowed_project_patterns";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AvoidDuplicatedPackageReferencesAcrossProjects,
        title: "Evitar PackageReference duplicado entre projetos",
        messageFormat: "Package '{0}' is referenced by multiple projects ({1}). Consider centralizing the dependency or using project references when appropriate.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Package references repeated across projects can increase coupling and hide dependencies that should be centralized or modeled as project references.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.AvoidDuplicatedPackageReferencesAcrossProjects),
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    private static readonly ImmutableArray<string> DefaultAllowedPackages = ImmutableArray.Create(
        "Microsoft.NET.Test.Sdk",
        "xunit",
        "xunit.runner.visualstudio",
        "coverlet.collector",
        "FluentAssertions",
        "NSubstitute",
        "Microsoft.CodeAnalysis.CSharp.Analyzer.Testing");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

#pragma warning disable RS1013 // ARCH030 intentionally inspects all project AdditionalFiles once at compilation end.
        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var optionsProvider = compilationContext.Options.AnalyzerConfigOptionsProvider;
            var additionalFiles = compilationContext.Options.AdditionalFiles;

            compilationContext.RegisterCompilationEndAction(context => AnalyzeProjects(context, optionsProvider, additionalFiles));
        });
#pragma warning restore RS1013
    }

    private static void AnalyzeProjects(
        CompilationAnalysisContext context,
        AnalyzerConfigOptionsProvider optionsProvider,
        ImmutableArray<AdditionalText> additionalFiles)
    {
        var packageReferences = new Dictionary<string, List<ProjectPackageReference>>(StringComparer.OrdinalIgnoreCase);

        foreach (var additionalFile in additionalFiles)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (!additionalFile.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var options = DuplicatedPackageReferenceOptions.Create(optionsProvider, additionalFile);
            if (options.IsAllowedProject(additionalFile.Path))
            {
                continue;
            }

            var sourceText = additionalFile.GetText(context.CancellationToken);
            if (sourceText is null || !TryParseProjectFile(sourceText, out var document))
            {
                continue;
            }

            foreach (var packageName in GetPackageReferenceNames(document))
            {
                if (options.IsAllowedPackage(packageName))
                {
                    continue;
                }

                if (!packageReferences.TryGetValue(packageName, out var references))
                {
                    references = new List<ProjectPackageReference>();
                    packageReferences.Add(packageName, references);
                }

                if (!references.Any(reference => string.Equals(reference.ProjectPath, additionalFile.Path, StringComparison.OrdinalIgnoreCase)))
                {
                    references.Add(new ProjectPackageReference(additionalFile.Path, packageName, sourceText));
                }
            }
        }

        foreach (var packageReferenceGroup in packageReferences.OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (packageReferenceGroup.Value.Count <= 1)
            {
                continue;
            }

            var firstReference = packageReferenceGroup.Value
                .OrderBy(static reference => GetProjectDisplayName(reference.ProjectPath), StringComparer.OrdinalIgnoreCase)
                .First();
            var projectNames = string.Join(
                ", ",
                packageReferenceGroup.Value
                    .Select(static reference => GetProjectDisplayName(reference.ProjectPath))
                    .OrderBy(static projectName => projectName, StringComparer.OrdinalIgnoreCase));
            var packageName = packageReferenceGroup.Value
                .Select(static reference => reference.PackageName)
                .OrderBy(static name => name, StringComparer.Ordinal)
                .First();

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                CreateStartLocation(firstReference.ProjectPath, firstReference.SourceText),
                packageName,
                projectNames));
        }
    }

    private static bool TryParseProjectFile(SourceText sourceText, out XDocument document)
    {
        try
        {
            document = XDocument.Parse(sourceText.ToString(), LoadOptions.PreserveWhitespace);
            return true;
        }
        catch (XmlException)
        {
            document = null!;
            return false;
        }
    }

    private static IEnumerable<string> GetPackageReferenceNames(XDocument document)
    {
        foreach (var element in document.Descendants().Where(static element => element.Name.LocalName == "PackageReference"))
        {
            var packageName = (string?)element.Attribute("Include") ?? (string?)element.Attribute("Update");

            if (!string.IsNullOrWhiteSpace(packageName))
            {
                yield return packageName!.Trim();
            }
        }
    }

    private static Location CreateStartLocation(string path, SourceText sourceText)
    {
        var span = new TextSpan(0, 0);
        return Location.Create(path, span, sourceText.Lines.GetLinePositionSpan(span));
    }

    private static string GetProjectDisplayName(string projectPath)
    {
        var normalizedPath = projectPath.Replace('\\', '/');
        var lastSeparator = normalizedPath.LastIndexOf('/');
        return lastSeparator >= 0 ? normalizedPath.Substring(lastSeparator + 1) : normalizedPath;
    }

    private readonly struct ProjectPackageReference
    {
        public ProjectPackageReference(string projectPath, string packageName, SourceText sourceText)
        {
            ProjectPath = projectPath;
            PackageName = packageName;
            SourceText = sourceText;
        }

        public string ProjectPath
        {
            get;
        }

        public string PackageName
        {
            get;
        }

        public SourceText SourceText
        {
            get;
        }
    }

    private readonly struct DuplicatedPackageReferenceOptions
    {
        private DuplicatedPackageReferenceOptions(
            ImmutableHashSet<string> allowedPackages,
            ImmutableArray<string> allowedProjectPatterns)
        {
            AllowedPackages = allowedPackages;
            AllowedProjectPatterns = allowedProjectPatterns;
        }

        private ImmutableHashSet<string> AllowedPackages
        {
            get;
        }

        private ImmutableArray<string> AllowedProjectPatterns
        {
            get;
        }

        public bool IsAllowedPackage(string packageName)
        {
            return AllowedPackages.Contains(packageName);
        }

        public bool IsAllowedProject(string projectPath)
        {
            var normalizedPath = projectPath.Replace('\\', '/');
            var projectName = GetProjectDisplayName(projectPath);

            foreach (var pattern in AllowedProjectPatterns)
            {
                if (MatchesWildcard(projectName, pattern) || MatchesWildcard(normalizedPath, pattern.Replace('\\', '/')))
                {
                    return true;
                }
            }

            return false;
        }

        public static DuplicatedPackageReferenceOptions Create(AnalyzerConfigOptionsProvider provider, AdditionalText additionalText)
        {
            var options = provider.GetOptions(additionalText);

            return new DuplicatedPackageReferenceOptions(
                ReadStringArray(options, AllowedPackagesOption, DefaultAllowedPackages).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase),
                ReadStringArray(options, AllowedProjectPatternsOption, ImmutableArray<string>.Empty).ToImmutableArray());
        }

        private static IEnumerable<string> ReadStringArray(
            AnalyzerConfigOptions options,
            string optionName,
            ImmutableArray<string> defaultValue)
        {
            if (!options.TryGetValue(optionName, out var configuredValue))
            {
                return defaultValue;
            }

            return TryParseJsonStringArray(configuredValue, out var parsedValues)
                ? parsedValues.Select(static value => value.Trim()).Where(static value => value.Length > 0)
                : defaultValue;
        }

        private static bool TryParseJsonStringArray(string value, out ImmutableArray<string> items)
        {
            var parser = new JsonStringArrayParser(value);
            return parser.TryParse(out items);
        }

        private static bool MatchesWildcard(string value, string pattern)
        {
            var valueIndex = 0;
            var patternIndex = 0;
            var starIndex = -1;
            var matchIndex = 0;

            while (valueIndex < value.Length)
            {
                if (patternIndex < pattern.Length
                    && (pattern[patternIndex] == value[valueIndex]
                        || char.ToUpperInvariant(pattern[patternIndex]) == char.ToUpperInvariant(value[valueIndex])))
                {
                    valueIndex++;
                    patternIndex++;
                    continue;
                }

                if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
                {
                    starIndex = patternIndex++;
                    matchIndex = valueIndex;
                    continue;
                }

                if (starIndex != -1)
                {
                    patternIndex = starIndex + 1;
                    valueIndex = ++matchIndex;
                    continue;
                }

                return false;
            }

            while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                patternIndex++;
            }

            return patternIndex == pattern.Length;
        }
    }

    private struct JsonStringArrayParser
    {
        private readonly string _value;
        private int _position;

        public JsonStringArrayParser(string value)
        {
            _value = value;
            _position = 0;
        }

        public bool TryParse(out ImmutableArray<string> items)
        {
            var builder = ImmutableArray.CreateBuilder<string>();

            SkipWhitespace();

            if (!TryRead('['))
            {
                items = ImmutableArray<string>.Empty;
                return false;
            }

            SkipWhitespace();

            if (TryRead(']'))
            {
                SkipWhitespace();
                if (_position != _value.Length)
                {
                    items = ImmutableArray<string>.Empty;
                    return false;
                }

                items = builder.ToImmutable();
                return true;
            }

            while (true)
            {
                SkipWhitespace();

                if (!TryReadString(out var item))
                {
                    items = ImmutableArray<string>.Empty;
                    return false;
                }

                builder.Add(item);
                SkipWhitespace();

                if (TryRead(']'))
                {
                    SkipWhitespace();
                    if (_position != _value.Length)
                    {
                        items = ImmutableArray<string>.Empty;
                        return false;
                    }

                    items = builder.ToImmutable();
                    return true;
                }

                if (!TryRead(','))
                {
                    items = ImmutableArray<string>.Empty;
                    return false;
                }
            }
        }

        private bool TryReadString(out string value)
        {
            var builder = new StringBuilder();

            if (!TryRead('"'))
            {
                value = string.Empty;
                return false;
            }

            while (_position < _value.Length)
            {
                var current = _value[_position++];

                if (current == '"')
                {
                    value = builder.ToString();
                    return true;
                }

                if (current == '\\')
                {
                    if (_position >= _value.Length)
                    {
                        value = string.Empty;
                        return false;
                    }

                    var escaped = _value[_position++];

                    switch (escaped)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            builder.Append(escaped);
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        default:
                            value = string.Empty;
                            return false;
                    }

                    continue;
                }

                builder.Append(current);
            }

            value = string.Empty;
            return false;
        }

        private bool TryRead(char expected)
        {
            if (_position >= _value.Length || _value[_position] != expected)
            {
                return false;
            }

            _position++;
            return true;
        }

        private void SkipWhitespace()
        {
            while (_position < _value.Length && char.IsWhiteSpace(_value[_position]))
            {
                _position++;
            }
        }
    }
}
