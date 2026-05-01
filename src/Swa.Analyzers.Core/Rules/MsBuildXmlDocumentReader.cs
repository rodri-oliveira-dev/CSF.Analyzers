using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis.Text;

namespace Swa.Analyzers.Core.Rules;

internal static class MsBuildXmlDocumentReader
{
    private const int MaxMsBuildFileLength = 1_000_000;
    private const long MaxXmlCharactersInDocument = 1_000_000;
    private const long MaxXmlCharactersFromEntities = 1024;

    public static bool TryRead(SourceText sourceText, LoadOptions loadOptions, CancellationToken cancellationToken, out XDocument document)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (sourceText.Length == 0 || sourceText.Length > MaxMsBuildFileLength)
        {
            document = null!;
            return false;
        }

        try
        {
            using var stringReader = new StringReader(sourceText.ToString());
            using var xmlReader = XmlReader.Create(stringReader, CreateSettings());

            document = XDocument.Load(xmlReader, loadOptions);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (XmlException)
        {
            document = null!;
            return false;
        }
        catch (InvalidOperationException)
        {
            document = null!;
            return false;
        }
    }

    private static XmlReaderSettings CreateSettings()
    {
        return new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            MaxCharactersInDocument = MaxXmlCharactersInDocument,
            MaxCharactersFromEntities = MaxXmlCharactersFromEntities,
            XmlResolver = null,
        };
    }
}
