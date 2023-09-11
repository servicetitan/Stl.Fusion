namespace Stl.Compatibility;

[StructLayout(LayoutKind.Auto)]
public readonly struct AsyncDisposableAdapter<T> : IAsyncDisposable
#if !NETSTANDARD2_0
    where T : IAsyncDisposable?
#else
    where T : IDisposable?
#endif
{
    public T Target { get; }

    public AsyncDisposableAdapter(T target)
        => Target = target;

    public ValueTask DisposeAsync()
    {
#if !NETSTANDARD2_0
        return Target?.DisposeAsync() ?? default;
#else
        if (Target is IAsyncDisposable ad)
            return ad.DisposeAsync();
        Target?.Dispose();
        return default;
#endif
    }

    public ConfiguredAsyncDisposableAdapter<T> ConfigureAwait(bool continueOnCapturedContext)
        => new(this, continueOnCapturedContext);
}
