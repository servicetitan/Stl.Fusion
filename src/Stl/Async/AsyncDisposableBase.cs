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

    /// <summary>
    /// The async part of the dispose process.
    /// It's called only when the object is disposed via <see cref="DisposeAsync"/>.
    /// </summary>
    /// <returns>Dispose task.</returns>
    protected abstract ValueTask DisposeAsyncCore();

    /// <summary>
    /// This is the synchronous part of the dispose process.
    /// It's called in any case; async part runs first.
    /// </summary>
    /// <param name="disposing"><code>true</code> if this method
    /// is called from <see cref="Dispose"/>;
    /// otherwise, <code>false</code>.</param>
    protected abstract void Dispose(bool disposing);
}
