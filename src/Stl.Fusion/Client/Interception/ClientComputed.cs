using Stl.Fusion.Client.Internal;
using Stl.Fusion.Interception;
using Stl.Rpc.Caching;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Interception;

public interface IClientComputed : IComputed, IDisposable
{
    RpcOutboundCall? Call { get; }
    RpcCacheEntry? CacheEntry { get; }

    Task WhenCallCompleted();
}

public class ClientComputed<T> : ComputeMethodComputed<T>, IClientComputed
{
    private RpcOutboundComputeCall<T>? _call;
    private readonly SemaphoreSlim? _callIsBound;
    private Task? _whenCallCompleted;

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
            TryBindToCallFromLock(call);
        else
            _callIsBound = new SemaphoreSlim(0);
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
        bool isBound;
        lock (Lock)
            isBound = TryBindToCallFromLock(call);
        if (isBound)
            _callIsBound!.Release(); // Should go after exit from lock!
    }

    public Task WhenCallCompleted()
    {
        if (_whenCallCompleted != null)
            return _whenCallCompleted;

        lock (Lock)
            return _whenCallCompleted ??= _call != null
                ? WaitForCallCompletion(_call)
                : WaitForCallAndItsCompletion();

        static Task WaitForCallCompletion(RpcOutboundComputeCall<T> call)
            => call.ResultTask.IsCompleted || call.WhenInvalidated.IsCompleted
                ? Task.CompletedTask
                : Task.WhenAny(call.ResultTask, call.WhenInvalidated);

        Task WaitForCallAndItsCompletion() {
            var whenCallIsBound = _callIsBound!.WaitAsync();
            if (whenCallIsBound.IsCompleted)
                return WaitForCallCompletion(_call!);

            return whenCallIsBound.ContinueWith(static (_, state) => {
                var self = (ClientComputed<T>)state;
                return WaitForCallCompletion(self._call!);
            },
            this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }

    // Protected methods

    protected bool TryBindToCallFromLock(RpcOutboundComputeCall<T> call)
    {
        if (_call != null)
            return false; // Should never happen, but just in case

        var whenInvalidated = call.WhenInvalidated;
        if (whenInvalidated.IsCompleted) {
            Invalidate(true);
            _call = call;
            return true;
        }

        _call = call;
        _ = whenInvalidated.ContinueWith(
            _ => Invalidate(true),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return true;
    }

    protected override void OnInvalidated()
    {
        base.OnInvalidated();
        if (Function is IClientComputeMethodFunction fn)
            fn.OnInvalidated(this);
    }
}
