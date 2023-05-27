using Stl.Internal;
using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Rpc.Internal;

public class RpcInboundComputeCall<TResult> : RpcInboundCall<TResult>
{
    public Computed<TResult>? Computed { get; protected set; }

    public RpcInboundComputeCall(RpcInboundContext context, RpcMethodDef methodDef)
        : base(context, methodDef)
    {
        if (NoWait)
            throw Errors.InternalError($"{GetType().GetName()} is incompatible with NoWait option.");
    }

    public override async Task Invoke()
    {
        if (!TryRegister())
            return;

        try {
            Arguments = DeserializeArguments();
            using var ccs = Fusion.Computed.BeginCapture();
            Result = await InvokeService().ConfigureAwait(false);
            Computed = ccs.Context.GetCaptured<TResult>();
        }
        catch (Exception error) {
            Result = new Result<TResult>(default!, error);
        }
        await Complete().ConfigureAwait(false);
        _ = HandleInvalidation();
    }

    private async Task HandleInvalidation()
    {
        var computed = Computed;
        if (computed == null) {
            // No computed is captured
            var cts = CancellationTokenSource;
            if (cts == null) // Already completed or cancelled
                return;

            CancellationTokenSource = null;
            cts.Dispose();
            Unregister();
            return;
        }

        try {
            await Computed!.WhenInvalidated(CancellationToken).ConfigureAwait(false);
        }
        catch {
            // Intended
        }
        if (!Context.CancellationToken.IsCancellationRequested)
            await Invalidate().ConfigureAwait(false);
    }

    public override ValueTask Complete()
    {
        var cts = CancellationTokenSource;
        if (cts == null) // Already completed or cancelled
            return ValueTaskExt.CompletedTask;

        if (CancellationToken.IsCancellationRequested) {
            // Call is cancelled @ the outbound end or Peer is disposed
            return ValueTaskExt.CompletedTask;
        }

        var computed = Computed;
        if (computed != null)
            ResultHeaders.Add(FusionRpcHeaders.Version with { Value = computed.Version.ToString() });

        var systemCallSender = Hub.Services.GetRequiredService<RpcSystemCallSender>();
        return systemCallSender.Complete(Context.Peer, Id, Result, ResultHeaders);
    }

    public virtual ValueTask Invalidate()
    {
        if (!TryComplete(false))
            return ValueTaskExt.CompletedTask;

        if (CancellationToken.IsCancellationRequested) {
            // Call is cancelled @ the outbound end or Peer is disposed, so there is nothing else to do
            return ValueTaskExt.CompletedTask;
        }

        ResultHeaders.Clear();
        var computeSystemCallSender = Hub.Services.GetRequiredService<RpcComputeSystemCallSender>();
        return computeSystemCallSender.Invalidate(Context.Peer, Id, ResultHeaders);
    }
}
