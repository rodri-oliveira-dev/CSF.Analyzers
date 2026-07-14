<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>

          <Exclude>
            [*]Microsoft.AspNetCore.OpenApi.*,
            [*]*OpenApi*
          </Exclude>

          <ExcludeByAttribute>
            ExcludeFromCodeCoverageAttribute,
            GeneratedCodeAttribute,
            CompilerGeneratedAttribute
          </ExcludeByAttribute>

          <ExcludeByFile>**/obj/**/*.cs,**/*.g.cs,**/*.generated.cs,**/Microsoft.AspNetCore.OpenApi.SourceGenerators/**/*.cs,**/Riok.Mapperly/**/*.cs</ExcludeByFile>

        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>



ReportGenerator(
    coverageFiles,
    coverageReport,
    new ReportGeneratorSettings
    {
        ReportTypes =
        [
            ReportGeneratorReportType.TextSummary,
            ReportGeneratorReportType.HtmlInline_AzurePipelines_Dark
        ],

        ClassFilters = new List<string>
        {
            "-Microsoft.AspNetCore.OpenApi*",
            "-System.Runtime.CompilerServices*"
        },

        ArgumentCustomization = args => args
            .Append("-filefilters:\"-**/obj/**;-**/*.g.cs;-**/*.generated.cs\"")
    });



reportgenerator `
  -reports:"./TestResults/**/*.cobertura.xml" `
  -targetdir:"coverage" `
  -reporttypes:"Html;TextSummary" `
  -filefilters:"-**/obj/**;-**/*.g.cs;-**/*.generated.cs"


    
