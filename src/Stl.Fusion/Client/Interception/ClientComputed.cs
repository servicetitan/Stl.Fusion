using Stl.Fusion.Client.Internal;
using Stl.Fusion.Interception;
using Stl.Rpc.Caching;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Interception;

public interface IClientComputed : IComputed, IDisposable
{
    RpcOutboundCall? Call { get; }
    RpcCacheEntry? CacheEntry { get; }

    Task WhenSynchronized();
}

public class ClientComputed<T> : ComputeMethodComputed<T>, IClientComputed
{
    private readonly TaskCompletionSource<Unit>? _whenSynchronizedSource;
    private RpcOutboundComputeCall<T>? _call;

    RpcOutboundCall? IClientComputed.Call => _call;
    public RpcOutboundComputeCall<T>? Call => _call;
    public RpcCacheEntry? CacheEntry { get; }

    public ClientComputed(
        ComputedOptions options,
        ComputeMethodInput input,
        Result<T> output,
        LTag version,
        bool isConsistent,
        RpcOutboundComputeCall<T>? call = null,
        RpcCacheEntry? cacheEntry = null)
        : base(options, input, output, version, isConsistent)
    {
        CacheEntry = cacheEntry;
        if (call != null)
            BindToCallFromLock(call);
        else
            _whenSynchronizedSource = TaskCompletionSourceExt.New<Unit>();
        StartAutoInvalidation();
    }

#pragma warning disable MA0055
    ~ClientComputed() => Dispose();
#pragma warning restore MA0055

    public void Dispose()
    {
        RpcOutboundComputeCall<T>? call;
        lock (Lock)
            call = _call;
        call?.Unregister(!this.IsInvalidated());
    }

    public void BindToCall(RpcOutboundComputeCall<T> call)
    {
        lock (Lock)
            BindToCallFromLock(call);
        _whenSynchronizedSource?.TrySetResult(default); // Should go after exit from lock!
    }

    public Task WhenSynchronized()
        => _whenSynchronizedSource?.Task ?? Task.CompletedTask;

    protected void BindToCallFromLock(RpcOutboundComputeCall<T> call)
    {
        if (_call != null)
            return; // Should never happen, but just in case

        var whenInvalidated = call.WhenInvalidated;
        if (whenInvalidated.IsCompleted) {
            Invalidate(true);
            _call = call;
        }

        _call = call;
        _ = whenInvalidated.ContinueWith(
            _ => Invalidate(true),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    protected override void OnInvalidated()
    {
        _whenSynchronizedSource?.TrySetResult(default);
        base.OnInvalidated();
        if (Function is IClientComputeMethodFunction fn)
            fn.OnInvalidated(this);
    }
}
