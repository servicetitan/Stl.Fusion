using Cysharp.Text;
using Stl.Fusion.Interception;
using Stl.Fusion.Rpc.Internal;
using Stl.Rpc.Infrastructure;
using Stl.Versioning;
using Errors = Stl.Internal.Errors;

namespace Stl.Fusion.Rpc.Interception;

public interface IRpcComputeMethodFunction : IComputeMethodFunction
{
    void OnInvalidated(IRpcComputed computed);
}

public class RpcComputeMethodFunction<T> : ComputeFunctionBase<T>, IRpcComputeMethodFunction
{
    private string? _toString;

    public VersionGenerator<LTag> VersionGenerator { get; }
    public RpcComputedCache RpcComputedCache { get; }

    public RpcComputeMethodFunction(
        ComputeMethodDef methodDef,
        VersionGenerator<LTag> versionGenerator,
        RpcComputedCache cache,
        IServiceProvider services)
        : base(methodDef, services)
    {
        VersionGenerator = versionGenerator;
        RpcComputedCache = cache;
    }

    public override string ToString()
        => _toString ??= ZString.Concat('*', base.ToString());

    public void OnInvalidated(IRpcComputed computed)
        => _ = RpcComputedCache.Set<T>((ComputeMethodInput)computed.Input, null, CancellationToken.None);

    protected override ValueTask<Computed<T>> Compute(
        ComputedInput input, Computed<T>? existing,
        CancellationToken cancellationToken)
    {
        var typedInput = (ComputeMethodInput)input;
        return RemoteCompute(typedInput, cancellationToken).ToValueTask();
        // return existing == null
        //     ? CachedCompute(typedInput, cancellationToken)
        //     : RpcCompute(typedInput, cancellationToken).ToValueTask();
    }

#if false
    private async ValueTask<Computed<T>> CachedCompute(
        ComputeMethodInput input,
        CancellationToken cancellationToken)
    {
        var outputOpt = await RpcComputedCache.Get<T>(input, cancellationToken).ConfigureAwait(false);
        if (outputOpt is not { } output)
            return await RpcCompute(input, cancellationToken).ConfigureAwait(false);

        var publicationState = CreateFakePublicationState(output);
        var computed = new ReplicaMethodComputed<T>(input.MethodDef.ComputedOptions, input, null, publicationState);

        // Start the task to retrieve the actual value
        using var _1 = ExecutionContextExt.SuppressFlow();
        _ = Task.Run(() => RpcCompute(input, cancellationToken), CancellationToken.None);
        return computed;
    }
#endif

    private async Task<Computed<T>> RemoteCompute(ComputeMethodInput input, CancellationToken cancellationToken)
    {
        RpcOutboundComputeCall<T>? call = null;
        Result<T> result;
        try {
            call = SendRpcCall(input, cancellationToken);
            var resultTask = (Task<T>)call.ResultTask;
            result = await resultTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception error) {
            result = new Result<T>(default!, error);
        }
        return new RpcComputed<T>(
            input.MethodDef.ComputedOptions,
            input, result, VersionGenerator.NextVersion(), true,
            call);
    }

    private RpcOutboundComputeCall<T> SendRpcCall(
        ComputeMethodInput input,
        CancellationToken cancellationToken)
    {
        using var scope = RpcOutboundContext.Use();
        var context = scope.Context;
        if (context.CallType != typeof(RpcOutboundComputeCall<>))
            context.CallType = typeof(RpcOutboundComputeCall<>);

        input.InvokeOriginalFunction(cancellationToken);
        var call = (RpcOutboundComputeCall<T>?)context.Call;
        if (call == null)
            throw Errors.InternalError(
                "No call is sent, which means the service behind this proxy isn't an RPC client proxy (misconfiguration), " +
                "or RpcPeerResolver routes the call to a local service, which shouldn't happen at this point.");
        return call;
    }
}
