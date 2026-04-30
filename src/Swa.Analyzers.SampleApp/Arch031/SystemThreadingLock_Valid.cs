namespace Swa.Analyzers.SampleApp.Arch031;

public sealed class SystemThreadingLock_Valid
{
    private readonly System.Threading.Lock _gate = new();

    public void Execute()
    {
        lock (_gate)
        {
        }
    }
}
