using Cysharp.Text;
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
        => Cache?.Remove((ComputeMethodInput)computed.Input);

    protected override ValueTask<Computed<T>> Compute(
        ComputedInput input, Computed<T>? existing,
        CancellationToken cancellationToken)
    {
        var typedInput = (ComputeMethodInput)input;
        return existing == null && Cache != null
            ? CachedCompute(typedInput, cancellationToken)
            : RemoteCompute(typedInput, cancellationToken).ToValueTask();
    }

    private async ValueTask<Computed<T>> CachedCompute(
        ComputeMethodInput input,
        CancellationToken cancellationToken)
    {
        var outputOpt = await Cache!.Get<T>(input, cancellationToken).ConfigureAwait(false);
        if (outputOpt is not { } output)
            return await RemoteCompute(input, cancellationToken).ConfigureAwait(false);

        var computed = new ClientComputed<T>(
            input.MethodDef.ComputedOptions,
            input, output, VersionGenerator.NextVersion(), true);

        // Start the task to retrieve the actual value
        using var suppressFlow = ExecutionContextExt.SuppressFlow();
        _ = Task.Run(() => RemoteCompute(input, cancellationToken), cancellationToken);
        return computed;
    }

    private async Task<Computed<T>> RemoteCompute(ComputeMethodInput input, CancellationToken cancellationToken)
    {
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
            isConsistent = call?.WhenInvalidated.IsCompletedSuccessfully() != true;
            if (isConsistent || ++retryIndex >= 3)
                break;

            // A small pause before retrying
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        var computed = new ClientComputed<T>(
            input.MethodDef.ComputedOptions,
            input, result, VersionGenerator.NextVersion(), isConsistent,
            call);
        Cache?.Set(computed);
        return computed;
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
