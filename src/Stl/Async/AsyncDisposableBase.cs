namespace Stl.Async;

/// <summary>
/// A template from
/// https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync
/// </summary>
public abstract class AsyncDisposableBase : IAsyncDisposable, IDisposable
{
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(false);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
    }

    protected abstract ValueTask DisposeAsyncCore();
    protected abstract void Dispose(bool disposing);
}
