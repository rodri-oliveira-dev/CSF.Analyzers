using System.Net.Http;

namespace Swa.Analyzers.SampleApp.Arch018;

public sealed class DirectHttpClientInstantiationInvalid
{
    public void Send()
    {
        // ARCH018: evite instanciação direta de HttpClient.
        using var client = new HttpClient();
    }
}
