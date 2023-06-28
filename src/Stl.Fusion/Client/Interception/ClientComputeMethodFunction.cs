using Cysharp.Text;
using Stl.Fusion.Client.Caching;
using Stl.Fusion.Client.Internal;
using Stl.Fusion.Interception;
using Stl.Rpc.Caching;
using Stl.Rpc.Infrastructure;
using Stl.Versioning;
using Errors = Stl.Internal.Errors;

namespace Stl.Fusion.Client.Interception;

public interface IClientComputeMethodFunction : IComputeMethodFunction
{
    void OnInvalidated(IClientComputed computed);
}

public class ClientComputeMethodFunction<T> : ComputeFunctionBase<T>, IClientComputeMethodFunction
{
    private string? _toString;

    protected readonly VersionGenerator<LTag> VersionGenerator;
    protected readonly ClientComputedCache? Cache;

    public ClientComputeMethodFunction(
        ComputeMethodDef methodDef,
        VersionGenerator<LTag> versionGenerator,
        ClientComputedCache? cache,
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
        if (computed.Call?.Context.CacheInfoCapture?.Key is { } cacheKey)
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
            : RemoteCompute(typedInput, cache, cancellationToken).ToValueTask();
    }

    private async ValueTask<Computed<T>> CachedCompute(
        ComputeMethodInput input,
        ClientComputedCache cache,
        CancellationToken cancellationToken)
    {
        var cacheInfoCapture = new RpcCacheInfoCapture(RpcCacheInfoCaptureMode.KeyOnly);
        SendRpcCall(input, cacheInfoCapture, cancellationToken);
        if (cacheInfoCapture.Key is not { } cacheKey)
            return await RemoteCompute(input, cache, cancellationToken).ConfigureAwait(false);

        var cachedResultOpt = await Cache!.Get<T>(input, cacheKey, cancellationToken).ConfigureAwait(false);
        if (!cachedResultOpt.IsSome(out var cachedResult))
            return await RemoteCompute(input, cache, cancellationToken).ConfigureAwait(false);

        var computed = new ClientComputed<T>(
            input.MethodDef.ComputedOptions,
            input, cachedResult, VersionGenerator.NextVersion(), true);

        using var suppressFlow = ExecutionContextExt.SuppressFlow();
        _ = Task.Run(() => RemoteCompute(input, cache, cancellationToken), cancellationToken);
        return computed;
    }

    private async Task<Computed<T>> RemoteCompute(
        ComputeMethodInput input,
        ClientComputedCache? cache,
        CancellationToken cancellationToken)
    {
        RpcCacheInfoCapture? cacheInfoCapture;
        RpcOutboundComputeCall<T>? call = null;
        Result<T> result;
        bool isConsistent;
        Result<TextOrBytes>? cacheResult;

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

            isConsistent = call == null || !call.WhenInvalidated.IsCompletedSuccessfully();
            if (isConsistent && cacheInfoCapture != null && !result.HasError)
                cacheResult = await cacheInfoCapture.ResultSource!.Task.ResultAwait(false);
            else
                cacheResult = null;

            if (isConsistent || ++retryIndex >= 3)
                break;

            // A small pause before retrying
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        var computed = new ClientComputed<T>(
            input.MethodDef.ComputedOptions,
            input, result, VersionGenerator.NextVersion(), isConsistent,
            call);

        var cacheKey = cacheInfoCapture?.Key;
        if (!ReferenceEquals(cacheKey, null)) {
            if (cacheResult?.IsValue(out var vCacheResult) == true)
                cache!.Set(cacheKey, vCacheResult);
            else
                cache!.Remove(cacheKey);
        }
        return computed;
    }

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

    private ClientComputedCache? GetCache(ComputeMethodInput input)
        => Cache == null
            ? null :
            input.MethodDef.ComputedOptions.ClientCacheMode != ClientCacheMode.Cache
                ? null
                : Cache;
}
