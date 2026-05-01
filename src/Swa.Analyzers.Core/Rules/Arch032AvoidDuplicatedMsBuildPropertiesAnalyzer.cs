using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Swa.Analyzers.Core.Common;

namespace Swa.Analyzers.Core.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Arch032AvoidDuplicatedMsBuildPropertiesAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Maintainability";
    private const string IgnoredPropertiesOption = "dotnet_diagnostic.ARCH032.ignored_properties";
    private const string CompareValuesOption = "dotnet_diagnostic.ARCH032.compare_values";

    private static readonly DiagnosticDescriptor Rule = new(
        id: RuleIdentifiers.AvoidDuplicatedMsBuildProperties,
        title: "Evitar propriedades MSBuild duplicadas",
        messageFormat: "MSBuild property '{0}' is already defined in Directory.Build.props. Remove the duplicate from this project file.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "MSBuild properties repeated in project files and Directory.Build.props can drift and make configuration harder to maintain.",
        helpLinkUri: RuleHelpLinks.ForRule(RuleIdentifiers.AvoidDuplicatedMsBuildProperties),
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    private static readonly ImmutableArray<string> DefaultIgnoredProperties = ImmutableArray.Create(
        "TargetFramework",
        "TargetFrameworks",
        "AssemblyName",
        "RootNamespace",
        "PackageId",
        "Version",
        "Authors",
        "Description");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

#pragma warning disable RS1013 // ARCH032 intentionally inspects MSBuild AdditionalFiles once at compilation end.
        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var optionsProvider = compilationContext.Options.AnalyzerConfigOptionsProvider;
            var additionalFiles = compilationContext.Options.AdditionalFiles;

            compilationContext.RegisterCompilationEndAction(context => AnalyzeMsBuildFiles(context, optionsProvider, additionalFiles));
        });
#pragma warning restore RS1013
    }

    private static void AnalyzeMsBuildFiles(
        CompilationAnalysisContext context,
        AnalyzerConfigOptionsProvider optionsProvider,
        ImmutableArray<AdditionalText> additionalFiles)
    {
        var propsFiles = new List<MsBuildFileProperties>();
        var projectFiles = new List<MsBuildFileProperties>();

        foreach (var additionalFile in additionalFiles)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (!IsProjectFile(additionalFile.Path) && !IsDirectoryBuildProps(additionalFile.Path))
            {
                continue;
            }

            var sourceText = additionalFile.GetText(context.CancellationToken);
            if (sourceText is null
                || !MsBuildXmlDocumentReader.TryRead(sourceText, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo, context.CancellationToken, out var document))
            {
                continue;
            }

            var properties = GetProperties(document, additionalFile.Path, sourceText).ToImmutableArray();
            if (properties.IsEmpty)
            {
                continue;
            }

            var fileProperties = new MsBuildFileProperties(additionalFile, properties);
            if (IsDirectoryBuildProps(additionalFile.Path))
            {
                propsFiles.Add(fileProperties);
            }
            else
            {
                projectFiles.Add(fileProperties);
            }
        }

        if (propsFiles.Count == 0 || projectFiles.Count == 0)
        {
            return;
        }

        foreach (var projectFile in projectFiles.OrderBy(static project => project.Path, StringComparer.OrdinalIgnoreCase))
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var options = DuplicatedMsBuildPropertyOptions.Create(optionsProvider, projectFile.AdditionalText);
            var nearestPropsFile = FindNearestDirectoryBuildProps(projectFile.Path, propsFiles);
            if (nearestPropsFile is null)
            {
                continue;
            }

            var propsProperties = nearestPropsFile.Value.Properties
                .GroupBy(static property => property.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(static group => group.Key, static group => group.ToImmutableArray(), StringComparer.OrdinalIgnoreCase);

            foreach (var projectProperty in projectFile.Properties)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (options.IsIgnored(projectProperty.Name)
                    || !propsProperties.TryGetValue(projectProperty.Name, out var matchingPropsProperties))
                {
                    continue;
                }

                if (options.CompareValues
                    && !matchingPropsProperties.Any(propsProperty => string.Equals(propsProperty.Value, projectProperty.Value, StringComparison.Ordinal)))
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    projectProperty.Location,
                    projectProperty.Name));
            }
        }
    }

    private static IEnumerable<MsBuildProperty> GetProperties(XDocument document, string path, SourceText sourceText)
    {
        var root = document.Root;
        if (root is null)
        {
            yield break;
        }

        foreach (var propertyGroup in root.Elements().Where(static element => element.Name.LocalName == "PropertyGroup"))
        {
            if (HasCondition(propertyGroup))
            {
                continue;
            }

            foreach (var propertyElement in propertyGroup.Elements())
            {
                if (HasCondition(propertyElement))
                {
                    continue;
                }

                var propertyName = propertyElement.Name.LocalName;
                var propertyValue = propertyElement.Value.Trim();
                if (string.IsNullOrWhiteSpace(propertyName) || propertyValue.Length == 0)
                {
                    continue;
                }

                yield return new MsBuildProperty(
                    propertyName,
                    propertyValue,
                    CreateElementLocation(path, sourceText, propertyElement, propertyName));
            }
        }
    }

    private static bool HasCondition(XElement element)
    {
        return element.Attributes().Any(static attribute => attribute.Name.LocalName == "Condition");
    }

    private static Location CreateElementLocation(string path, SourceText sourceText, XElement element, string propertyName)
    {
        if (element is IXmlLineInfo lineInfo
            && lineInfo.HasLineInfo()
            && lineInfo.LineNumber > 0
            && lineInfo.LinePosition > 0
            && lineInfo.LineNumber <= sourceText.Lines.Count)
        {
            var start = sourceText.Lines.GetPosition(new LinePosition(lineInfo.LineNumber - 1, lineInfo.LinePosition - 1));
            var length = Math.Min(propertyName.Length + 2, sourceText.Length - start);
            var span = new TextSpan(start, Math.Max(length, 0));
            return Location.Create(path, span, sourceText.Lines.GetLinePositionSpan(span));
        }

        var fallbackSpan = new TextSpan(0, 0);
        return Location.Create(path, fallbackSpan, sourceText.Lines.GetLinePositionSpan(fallbackSpan));
    }

    private static MsBuildFileProperties? FindNearestDirectoryBuildProps(string projectPath, IEnumerable<MsBuildFileProperties> propsFiles)
    {
        var projectDirectory = GetDirectory(projectPath);

        var candidates = propsFiles
            .Where(propsFile => IsAncestorOrSame(GetDirectory(propsFile.Path), projectDirectory))
            .OrderByDescending(static propsFile => NormalizePath(GetDirectory(propsFile.Path)).Length)
            .ToArray();

        return candidates.Length > 0 ? candidates[0] : null;
    }

    private static bool IsAncestorOrSame(string ancestorDirectory, string directory)
    {
        var normalizedAncestor = NormalizePath(ancestorDirectory).TrimEnd('/');
        var normalizedDirectory = NormalizePath(directory).TrimEnd('/');

        if (normalizedAncestor.Length == 0)
        {
            return true;
        }

        return normalizedDirectory.Equals(normalizedAncestor, StringComparison.OrdinalIgnoreCase)
            || normalizedDirectory.StartsWith(normalizedAncestor + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDirectory(string path)
    {
        var normalizedPath = NormalizePath(path);
        var lastSeparator = normalizedPath.LastIndexOf('/');
        return lastSeparator >= 0 ? normalizedPath.Substring(0, lastSeparator) : string.Empty;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static bool IsProjectFile(string path)
    {
        return path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDirectoryBuildProps(string path)
    {
        var normalizedPath = NormalizePath(path);
        return normalizedPath.EndsWith("/Directory.Build.props", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.Equals("Directory.Build.props", StringComparison.OrdinalIgnoreCase);
    }

    private readonly struct MsBuildFileProperties
    {
        public MsBuildFileProperties(AdditionalText additionalText, ImmutableArray<MsBuildProperty> properties)
        {
            AdditionalText = additionalText;
            Properties = properties;
        }

        public AdditionalText AdditionalText
        {
            get;
        }

        public string Path
        {
            get => AdditionalText.Path;
        }

        public ImmutableArray<MsBuildProperty> Properties
        {
            get;
        }
    }

    private readonly struct MsBuildProperty
    {
        public MsBuildProperty(string name, string value, Location location)
        {
            Name = name;
            Value = value;
            Location = location;
        }

        public string Name
        {
            get;
        }

        public string Value
        {
            get;
        }

        public Location Location
        {
            get;
        }
    }

    private readonly struct DuplicatedMsBuildPropertyOptions
    {
        private DuplicatedMsBuildPropertyOptions(ImmutableHashSet<string> ignoredProperties, bool compareValues)
        {
            IgnoredProperties = ignoredProperties;
            CompareValues = compareValues;
        }

        private ImmutableHashSet<string> IgnoredProperties
        {
            get;
        }

        public bool CompareValues
        {
            get;
        }

        public bool IsIgnored(string propertyName)
        {
            return IgnoredProperties.Contains(propertyName);
        }

        public static DuplicatedMsBuildPropertyOptions Create(AnalyzerConfigOptionsProvider provider, AdditionalText additionalText)
        {
            var options = provider.GetOptions(additionalText);

            return new DuplicatedMsBuildPropertyOptions(
                ReadStringArray(options, IgnoredPropertiesOption, DefaultIgnoredProperties).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase),
                ReadBoolean(options, CompareValuesOption, defaultValue: true));
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

            return JsonStringArrayOptionParser.TryParse(configuredValue, out var parsedValues)
                ? parsedValues.Select(static value => value.Trim()).Where(static value => value.Length > 0)
                : defaultValue;
        }

        private static bool ReadBoolean(AnalyzerConfigOptions options, string optionName, bool defaultValue)
        {
            return options.TryGetValue(optionName, out var configuredValue)
                && bool.TryParse(configuredValue, out var parsedValue)
                    ? parsedValue
                    : defaultValue;
        }

    }
}
