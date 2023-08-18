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
{
    void OnInvalidated(IClientComputed computed);
}

public class ClientComputeMethodFunction<T> : ComputeFunctionBase<T>, IClientComputeMethodFunction
{
    private string? _toString;

    protected readonly VersionGenerator<LTag> VersionGenerator;
    protected readonly IClientComputedCache? Cache;

    public ClientComputeMethodFunction(
        ComputeMethodDef methodDef,
        VersionGenerator<LTag> versionGenerator,
        IClientComputedCache? cache,
        IServiceProvider services)
        : base(methodDef, services)
    {
        VersionGenerator = versionGenerator;
        Cache = cache;
    }

    public override string ToString()
        => _toString ??= ZString.Concat('*', base.ToString());

    public void OnInvalidated(IClientComputed computed)
    {
        var cacheKey = computed.CacheEntry?.Key ?? computed.Call?.Context.CacheInfoCapture?.Key;
        if (!ReferenceEquals(cacheKey, null))
            GetCache((ComputeMethodInput)computed.Input)?.Remove(cacheKey);
    }

    protected override ValueTask<Computed<T>> Compute(
        ComputedInput input, Computed<T>? existing,
        CancellationToken cancellationToken)
    {
        var typedInput = (ComputeMethodInput)input;
        var cache = GetCache(typedInput);
        return existing == null && cache != null
            ? CachedCompute(typedInput, cache, cancellationToken)
            : RemoteCompute(typedInput, cache, (ClientComputed<T>?)existing, cancellationToken).ToValueTask();
    }

    private async ValueTask<Computed<T>> CachedCompute(
        ComputeMethodInput input,
        IClientComputedCache cache,
        CancellationToken cancellationToken)
    {
        var cacheInfoCapture = new RpcCacheInfoCapture(RpcCacheInfoCaptureMode.KeyOnly);
        SendRpcCall(input, cacheInfoCapture, cancellationToken);
        if (cacheInfoCapture.Key is not { } cacheKey)
            return await RemoteCompute(input, cache, null, cancellationToken).ConfigureAwait(false);

        var cacheResultOpt = await Cache!.Get<T>(input, cacheKey, cancellationToken).ConfigureAwait(false);
        if (cacheResultOpt is not { } cacheResult)
            return await RemoteCompute(input, cache, null, cancellationToken).ConfigureAwait(false);

        var cacheEntry = new RpcCacheEntry(cacheKey, cacheResult.Data);
        var computed = new ClientComputed<T>(
            input.MethodDef.ComputedOptions,
            input, cacheResult.Value, VersionGenerator.NextVersion(), true,
            null, cacheEntry);

        // SuppressFlow here ensures that "true" computed won't be registered as a dependency -
        // which is correct, coz its cached version already became a dependency, and once
        // the true computed is created, its cached (prev.) version will be invalidated.
        using var suppressFlow = ExecutionContextExt.SuppressFlow();
        _ = Task.Run(() => RemoteCompute(input, cache, computed, cancellationToken), cancellationToken);
        return computed;
    }

    private async Task<Computed<T>> RemoteCompute(
        ComputeMethodInput input,
        IClientComputedCache? cache,
        ClientComputed<T>? existing,
        CancellationToken cancellationToken)
    {
        RpcCacheInfoCapture? cacheInfoCapture;
        RpcCacheEntry? cacheEntry;
        RpcOutboundComputeCall<T>? call = null;
        Result<T> result;
        bool isConsistent;

        var retryIndex = 0;
        while (true) {
            // We repeat this 3 times in case we get a result,
            // which is instantly inconsistent.
            // This is possible, if the call is re-sent on reconnect,
            // and the very first response that passes through is
            // invalidation.
            cacheInfoCapture = cache != null ? new RpcCacheInfoCapture() : null;
            try {
                call = SendRpcCall(input, cacheInfoCapture, cancellationToken);
                var resultTask = (Task<T>)call.ResultTask;
                result = await resultTask.ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException) {
                result = new Result<T>(default!, e);
            }

            isConsistent = call == null || !call.WhenInvalidated.IsCompleted;
            cacheEntry = null;
            if (isConsistent && cacheInfoCapture != null && !result.HasError)
                cacheEntry = await cacheInfoCapture.GetEntry(cancellationToken).ConfigureAwait(false);

            if (isConsistent || ++retryIndex >= 3)
                break;

            // A small pause before retrying
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        var existingCacheEntry = existing?.CacheEntry;
        if (cacheEntry != null && existingCacheEntry != null
            && existing!.IsConsistent()
            && existingCacheEntry.Result.DataEquals(cacheEntry.Result)) {
            // We know here that existing is consistent with the new result
            existing!.BindToCall(call!); // cacheEntry != null -> call != null
            return existing;
        }

        var computed = new ClientComputed<T>(
            input.MethodDef.ComputedOptions,
            input, result, VersionGenerator.NextVersion(), isConsistent,
            call);

        var cacheKey = cacheInfoCapture?.Key;
        if (ReferenceEquals(cacheKey, null))
            return computed; // No cache key -> no need to worry about the cache

        // Update cache
        if (cacheEntry == null)
            cache!.Remove(cacheKey);
        else if (existingCacheEntry == null || !existingCacheEntry.Result.DataEquals(cacheEntry.Result))
            cache!.Set(cacheKey, cacheEntry.Result); // Update cache if data has changed

        return computed;
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

    private IClientComputedCache? GetCache(ComputeMethodInput input)
        => Cache == null
            ? null :
            input.MethodDef.ComputedOptions.ClientCacheMode != ClientCacheMode.Cache
                ? null
                : Cache;
}
