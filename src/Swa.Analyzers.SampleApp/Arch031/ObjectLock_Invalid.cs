namespace Swa.Analyzers.SampleApp.Arch031;

public sealed class ObjectLock_Invalid
{
    private readonly object _gate = new();

    public void Execute()
    {
        // ARCH031: prefira System.Threading.Lock para monitores dedicados.
        lock (_gate)
        {
        }
    }

    public void ExecuteWithNewObject()
    {
        // ARCH031: um object novo no lock não sincroniza chamadas diferentes.
        lock (new object())
        {
        }
    }
}
