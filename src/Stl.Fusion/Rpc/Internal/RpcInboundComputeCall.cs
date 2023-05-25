using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Rpc.Internal;

public class RpcInboundComputeCall<TResult> : RpcInboundCall<TResult>
{
    public Computed<TResult>? Computed { get; protected set; }

    public RpcInboundComputeCall(RpcInboundContext context, RpcMethodDef methodDef)
        : base(context, methodDef)
    { }

    public override async Task Process()
    {
        if (!TryRegister())
            return;

        using var ccs = Fusion.Computed.BeginCapture();
        try {
            Arguments = DeserializeArguments();
            Result = await InvokeService().ConfigureAwait(false);
            Computed = ccs.Context.GetCaptured<TResult>();
        }
        catch (Exception error) {
            Result = new Result<TResult>(default!, error);
        }
        await Complete().ConfigureAwait(false);
    }

    public override ValueTask Complete()
    {
        var cts = CancellationTokenSource;
        if (cts == null) // NoWait or already completed
            return ValueTaskExt.CompletedTask;

        CancellationTokenSource = null;
        cts.Dispose();

        if (CancellationToken.IsCancellationRequested) {
            Unregister();
            // Call is cancelled @ the outbound end or Peer is disposed, so there is nothing else to do
            return ValueTaskExt.CompletedTask;
        }

        var computed = Computed;
        if (computed == null) {
            // No computed is captured
            Unregister();
        }
        else {
            ResultHeaders.Add(RpcFusionHeaders.Version with { Value = computed.Version.ToString() });
            // computed.Invalidated += 
        }

        var systemCallSender = Hub.Services.GetRequiredService<RpcSystemCallSender>();
        return systemCallSender.Complete(Context.Peer, Id, Result, ResultHeaders);
    }
}
