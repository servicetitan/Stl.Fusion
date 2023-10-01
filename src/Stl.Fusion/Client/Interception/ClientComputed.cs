using Stl.Caching;
using Stl.Fusion.Client.Internal;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;
using Stl.Rpc.Caching;

namespace Stl.Fusion.Client.Interception;

#pragma warning disable VSTHRD104
#pragma warning disable MA0055

public interface IClientComputed : IComputed, IMaybeCachedValue, IDisposable
{
    Task WhenCallBound { get; }
    RpcCacheEntry? CacheEntry { get; }
}

public class ClientComputed<T> : ComputeMethodComputed<T>, IClientComputed
{
    internal readonly TaskCompletionSource<RpcOutboundComputeCall<T>?> CallSource;
    internal readonly TaskCompletionSource<Unit> SynchronizedSource;

    Task IClientComputed.WhenCallBound => CallSource.Task;
    public Task<RpcOutboundComputeCall<T>?> WhenCallBound => CallSource.Task;
    public RpcCacheEntry? CacheEntry { get; }
    public Task WhenSynchronized => SynchronizedSource.Task;

    public ClientComputed(
        ComputedOptions options,
        ComputeMethodInput input,
        Result<T> output,
        LTag version,
        RpcCacheEntry? cacheEntry,
        TaskCompletionSource<Unit>? synchronizedSource = null)
        : base(options, input, output, version, true, SkipComputedRegistration.Option)
    {
        CallSource = TaskCompletionSourceExt.New<RpcOutboundComputeCall<T>?>();
        CacheEntry = cacheEntry;
        SynchronizedSource = synchronizedSource ?? TaskCompletionSourceExt.New<Unit>();
        ComputedRegistry.Instance.Register(this);
        StartAutoInvalidation();
    }

    ~ClientComputed() => Dispose();

    public void Dispose()
    {
        if (!WhenCallBound.IsCompleted)
            return;

        var call = WhenCallBound.Result;
        call?.Unregister(!this.IsInvalidated());
    }

    // Internal methods

    internal bool BindToCall(RpcOutboundComputeCall<T>? call)
    {
        if (!CallSource.TrySetResult(call)) {
            if (call == null) {
                // Call from OnInvalidated - we need to cancel the old call
                var boundCall = WhenCallBound.Result;
                boundCall?.SetInvalidated(true);
            }
            else {
                // Normal BindToCall, we cancel the call to ensure its invalidation sub. is gone
                call.SetInvalidated(true);
            }
            return false;
        }
        if (call == null) // Invalidated before being bound to call - nothing else to do
            return true;

        var whenInvalidated = call.WhenInvalidated;
        if (whenInvalidated.IsCompleted) {
            // No call (call prepare error - e.g. if there is no such RPC service),
            // or the call result is already invalidated
            Invalidate(true);
            return true;
        }

        _ = whenInvalidated.ContinueWith(
            _ => Invalidate(true),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return true;
    }

    protected override void OnInvalidated()
    {
        BindToCall(null);
        // PseudoUnregister is used here just to trigger the
        // Unregistered event in ComputedRegistry.
        // We want to keep this computed while it's possible:
        // ClientComputed.Compute tries to use the existing
        // one to update. If this computed instance is gone
        // from registry, ClientComputed is going to be created
        // anew and will hit the cache.
        ComputedRegistry.Instance.PseudoUnregister(this);
        CancelTimeouts();
    }
}
