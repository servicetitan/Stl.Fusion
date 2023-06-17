using Cysharp.Text;
using Stl.Fusion.Client.Cache;
using Stl.Fusion.Client.Internal;
using Stl.Fusion.Interception;
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

    public VersionGenerator<LTag> VersionGenerator { get; }
    public ClientComputedCache Cache { get; }

    public ClientComputeMethodFunction(
        ComputeMethodDef methodDef,
        VersionGenerator<LTag> versionGenerator,
        ClientComputedCache cache,
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
        // _ = Cache.Set<T>((ComputeMethodInput)computed.Input, null, CancellationToken.None);
    }

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
        using var suppressFlow = ExecutionContextExt.SuppressFlow();
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
        var isConsistent = call?.WhenInvalidated.IsCompletedSuccessfully() != true;
        return new ClientComputed<T>(
            input.MethodDef.ComputedOptions,
            input, result, VersionGenerator.NextVersion(), isConsistent,
            call);
    }

    private RpcOutboundComputeCall<T> SendRpcCall(
        ComputeMethodInput input,
        CancellationToken cancellationToken)
    {
        using var scope = RpcOutboundContext.Use(RpcComputeCallType.Id);
        input.InvokeOriginalFunction(cancellationToken);
        var call = (RpcOutboundComputeCall<T>?)scope.Context.Call;
        if (call == null)
            throw Errors.InternalError(
                "No call is sent, which means the service behind this proxy isn't an RPC client proxy (misconfiguration), " +
                "or RpcPeerResolver routes the call to a local service, which shouldn't happen at this point.");
        return call;
    }
}
