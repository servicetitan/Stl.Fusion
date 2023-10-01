using Cysharp.Text;
using Stl.Fusion.Client.Caching;
using Stl.Fusion.Client.Internal;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;
using Stl.Rpc.Caching;
using Stl.Rpc.Infrastructure;
using Stl.Versioning;
using Errors = Stl.Internal.Errors;

namespace Stl.Fusion.Client.Interception;

#pragma warning disable VSTHRD103

public interface IClientComputeMethodFunction : IComputeMethodFunction
{ }

public class ClientComputeMethodFunction<T>(
    ComputeMethodDef methodDef,
    VersionGenerator<LTag> versionGenerator,
    IClientComputedCache? cache,
    IServiceProvider services
    ) : ComputeFunctionBase<T>(methodDef, services), IClientComputeMethodFunction
{
    private string? _toString;

    protected readonly VersionGenerator<LTag> VersionGenerator = versionGenerator;
    protected readonly IClientComputedCache? Cache = cache;

    public override string ToString()
        => _toString ??= ZString.Concat('*', base.ToString());

    protected override ValueTask<Computed<T>> Compute(
        ComputedInput input, Computed<T>? existing,
        CancellationToken cancellationToken)
    {
        var typedInput = (ComputeMethodInput)input;
        var cache = GetCache(typedInput);
        return existing == null && cache != null
            ? ComputeCachedOrRpc(typedInput, cache, cancellationToken)
            : ComputeRpc(typedInput, cache, (ClientComputed<T>)existing!, cancellationToken).ToValueTask();
    }

    private async Task<Computed<T>> ComputeRpc(
        ComputeMethodInput input,
        IClientComputedCache? cache,
        ClientComputed<T>? existing,
        CancellationToken cancellationToken)
    {
        var cacheInfoCapture = cache != null ? new RpcCacheInfoCapture() : null;
        var call = SendRpcCall(input, cacheInfoCapture, cancellationToken);

        var typedResultTask = (Task<T>)call.ResultTask;
        var result = typedResultTask.IsCompletedSuccessfully()
            ? (Result<T>)typedResultTask.Result
            : await typedResultTask.ToResultAsync().ConfigureAwait(false);
        var cacheEntry = cacheInfoCapture == null ? null :
            await UpdateCache(cache!, cacheInfoCapture, result).ConfigureAwait(false);

        var synchronizedSource = existing?.SynchronizedSource ?? AlreadySynchronized.Source;
        var computed = new ClientComputed<T>(
            input.MethodDef.ComputedOptions,
            input, result, VersionGenerator.NextVersion(),
            cacheEntry, synchronizedSource);
        computed.BindToCall(call);
        synchronizedSource.TrySetResult(default);
        return computed;
    }

    private async ValueTask<Computed<T>> ComputeCachedOrRpc(
        ComputeMethodInput input,
        IClientComputedCache cache,
        CancellationToken cancellationToken)
    {
        var cacheInfoCapture = new RpcCacheInfoCapture(RpcCacheInfoCaptureMode.KeyOnly);
        // This is a fake call that only captures the cache key.
        // No actual call happens at this point.
        SendRpcCall(input, cacheInfoCapture, cancellationToken);
        if (cacheInfoCapture.Key is not { } cacheKey)
            return await ComputeRpc(input, cache, null, cancellationToken).ConfigureAwait(false);

        var cacheResultOpt = await cache.Get<T>(input, cacheKey, cancellationToken).ConfigureAwait(false);
        if (cacheResultOpt is not { } cacheResult)
            return await ComputeRpc(input, cache, null, cancellationToken).ConfigureAwait(false);

        var cacheEntry = new RpcCacheEntry(cacheKey, cacheResult.Data);
        var cached = new ClientComputed<T>(
            input.MethodDef.ComputedOptions,
            input, cacheResult.Value, VersionGenerator.NextVersion(),
            cacheEntry);

        // SuppressFlow here ensures that "true" computed won't be registered as a dependency -
        // which is correct, coz its cached version already became a dependency, and once
        // the true computed is created, its cached (prev.) version will be invalidated.
        // NOTE(AY): We can't use cancellationToken from here:
        // - We're completing the computation w/ cached value here
        // - But the code below starts the async task running the actual RPC call
        // - And if this task gets cancelled, the subscription to invalidation won't be set up,
        //   and thus the result may end up being stale forever.
        using var suppressFlow = ExecutionContextExt.SuppressFlow();
        _ = Task.Run(() => ApplyRpcUpdate(input, cache, cached), default);
        return cached;
    }

    private async Task ApplyRpcUpdate(
        ComputeMethodInput input,
        IClientComputedCache cache,
        ClientComputed<T> cached)
    {
        // 1. Start the RPC call
        var cacheInfoCapture = new RpcCacheInfoCapture();
        var call = SendRpcCall(input, cacheInfoCapture, default);

        // 2. Bind the call to cached computed
        if (!cached.BindToCall(call)) {
            // Ok, this is a weird case: existing was invalidated manually while we were getting here.
            // This means the call is already aborted (see BindToCall logic), and since we're
            // operating in background to update cached value, we can just exit.
            return;
        }

        // 3. Await for its completion
        var typedResultTask = (Task<T>)call.ResultTask;
        var result = typedResultTask.IsCompletedSuccessfully()
            ? (Result<T>)typedResultTask.Result
            : await typedResultTask.ToResultAsync().ConfigureAwait(false);
        var cacheEntry = await cacheInfoCapture.GetEntry().ConfigureAwait(false);

        // 4. Re-entering the lock & check if cached is still consistent
        using var _ = await InputLocks.Lock(input).ConfigureAwait(false);
        if (!cached.IsConsistent())
            return; // Since the call was bound to cached, it's properly cancelled already

        var synchronizedSource = cached.SynchronizedSource;
        if (!result.HasError && cacheEntry?.IsResultIdenticalTo(cached.CacheEntry!) == true) {
            // Existing is still intact
            synchronizedSource.TrySetResult(default);
            return;
        }

        // 5. Now, let's update cache entry
        UpdateCache(cache, cacheEntry, result);

        // 6. Create the new computed - it invalidates the cached one upon registering
        var computed = new ClientComputed<T>(
            input.MethodDef.ComputedOptions,
            input, result, VersionGenerator.NextVersion(),
            cacheEntry, synchronizedSource);
        computed.BindToCall(call);
        ((IComputedImpl)computed).RenewTimeouts(true);
        synchronizedSource.TrySetResult(default);
    }

    public override async ValueTask<Computed<T>> Invoke(ComputedInput input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken = default)
    {
        context ??= ComputeContext.Current;

        // Read-Lock-RetryRead-Compute-Store pattern

        var existing = GetExisting(input);
        if (existing.TryUseExisting(context, usedBy))
            return existing!;

        using var _ = await InputLocks.Lock(input, cancellationToken).ConfigureAwait(false);

        existing = GetExisting(input);
        if (existing.TryUseExistingFromLock(context, usedBy))
            return existing!;

        var computed = await Compute(input, existing, cancellationToken).ConfigureAwait(false);
        computed.UseNew(context, usedBy, existing);
        return computed;
    }

    public override Task<T> InvokeAndStrip(ComputedInput input,
        IComputed? usedBy,
        ComputeContext? context,
        CancellationToken cancellationToken = default)
    {
        context ??= ComputeContext.Current;

        var computed = GetExisting(input);
        return computed.TryUseExisting(context, usedBy)
            ? computed.StripToTask(context)
            : TryRecompute(input, usedBy, context, cancellationToken);
    }

    // Protected methods

    protected new async Task<T> TryRecompute(ComputedInput input,
        IComputed? usedBy,
        ComputeContext context,
        CancellationToken cancellationToken = default)
    {
        using var _ = await InputLocks.Lock(input, cancellationToken).ConfigureAwait(false);

        var existing = GetExisting(input);
        if (existing.TryUseExistingFromLock(context, usedBy))
            return existing.Strip(context);

        var computed = await Compute(input, existing, cancellationToken).ConfigureAwait(false);
        computed.UseNew(context, usedBy, existing);
        return computed.Value;
    }

    // Private methods

    private RpcOutboundComputeCall<T> SendRpcCall(
        ComputeMethodInput input,
        RpcCacheInfoCapture? cacheInfoCapture,
        CancellationToken cancellationToken)
    {
        using var scope = RpcOutboundContext.Use(RpcComputeCallType.Id);
        scope.Context.CacheInfoCapture = cacheInfoCapture;
        input.InvokeOriginalFunction(cancellationToken);
        var call = (RpcOutboundComputeCall<T>?)scope.Context.Call;
        if (call == null)
            throw Errors.InternalError(
                "No call is sent, which means the service behind this proxy isn't an RPC client proxy (misconfiguration), " +
                "or RpcPeerResolver routes the call to a local service, which shouldn't happen at this point.");
        return call;
    }

    private async ValueTask<RpcCacheEntry?> UpdateCache(
        IClientComputedCache cache,
        RpcCacheInfoCapture cacheInfoCapture,
        Result<T> result)
    {
        var key = cacheInfoCapture.Key;
        if (ReferenceEquals(key, null))
            return null;

        if (result.Error is { } error) {
            if (error is not OperationCanceledException)
                cache.Remove(key); // Error -> wipe cache entry
            return null;
        }

        var cacheEntry = await cacheInfoCapture.GetEntry().ConfigureAwait(false);
        if (cacheEntry == null)
            return null; // Nothing to update

        // Update cache
        if (cacheEntry.Result.IsEmpty)
            cache.Remove(key);  // An error, but not cancellation
        else
            cache.Set(cacheEntry.Key, cacheEntry.Result);
        return cacheEntry;
    }

    private void UpdateCache(
        IClientComputedCache cache,
        RpcCacheEntry? cacheEntry,
        Result<T> result)
    {
        if (cacheEntry is not { Key: { } key })
            return; // Nothing to update

        if (result.Error is { } error) {
            if (error is not OperationCanceledException)
                cache.Remove(key); // Error -> wipe cache entry
            return;
        }

        // Update cache
        if (cacheEntry.Result.IsEmpty)
            cache.Remove(key);  // An error, but not cancellation
        else
            cache.Set(cacheEntry.Key, cacheEntry.Result);
    }

    private IClientComputedCache? GetCache(ComputeMethodInput input)
        => Cache == null
            ? null :
            input.MethodDef.ComputedOptions.ClientCacheMode != ClientCacheMode.Cache
                ? null
                : Cache;
}
