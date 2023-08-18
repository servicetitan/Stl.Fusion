using Stl.Internal;
using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Internal;

public class RpcInboundComputeCall<TResult> : RpcInboundCall<TResult>
{
    private CancellationTokenSource? completionCancellationSource;

    public Computed<TResult>? Computed { get; protected set; }

    public RpcInboundComputeCall(RpcInboundContext context, RpcMethodDef methodDef)
        : base(context, methodDef)
    {
        if (NoWait)
            throw Errors.InternalError($"{GetType().GetName()} is incompatible with NoWait option.");
    }

    public override async ValueTask Complete(bool silentCancel = false)
    {
        silentCancel |= CancellationToken.IsCancellationRequested;
        if (silentCancel) {
            PrepareToComplete(silentCancel); // This call also cancels any other running completion
            return;
        }

        CancellationToken cancellationToken;
        Computed<TResult>? computed;
        lock (Lock) {
            // Let's cancel already running completion first
            completionCancellationSource.CancelAndDisposeSilently();
            completionCancellationSource = CancellationToken.CreateLinkedTokenSource();
            cancellationToken = completionCancellationSource.Token;
            computed = Computed;
            if (computed != null) {
                var versionHeader = FusionRpcHeaders.Version with { Value = computed.Version.ToString() };
                ResultHeaders = ResultHeaders.TryAdd(versionHeader);
            }
        }
        if (cancellationToken.IsCancellationRequested)
            return;

        if (computed == null) {
            await base.Complete(silentCancel: false).ConfigureAwait(false);
            return;
        }

        await SendResult().ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested)
            return;

        await Computed!.WhenInvalidated(cancellationToken).SilentAwait(false);
        if (cancellationToken.IsCancellationRequested)
            return;

        lock (Lock) {
            completionCancellationSource.CancelAndDisposeSilently();
            if (!PrepareToComplete())
                return;
        }
        await SendInvalidation().ConfigureAwait(false);
    }

    public override bool Restart()
    {
        if (!ResultTask.IsCompleted)
            return true; // Result isn't produced yet

        // Result is produced
        lock (Lock) {
            if (completionCancellationSource == null)
                return true; // CompleteEventually haven't started yet -> let it do the job

            if (completionCancellationSource.IsCancellationRequested)
                return false; // CompleteEventually already ended -> we have to restart

            // CompleteEventually is running - let's try to cancel it
            completionCancellationSource.CancelAndDisposeSilently();
        }
        _ = CompleteEventually();
        return true;
    }

    // Protected & private methods

    protected override async Task<TResult> InvokeTarget()
    {
        var ccs = Fusion.Computed.BeginCapture();
        try {
            return await base.InvokeTarget().ConfigureAwait(false);
        }
        finally {
            if (ccs.Context.TryGetCaptured<TResult>(out var computed)) {
                lock (Lock)
                    Computed = computed;
            }
            ccs.Dispose();
        }
    }

    private ValueTask SendInvalidation()
    {
        var computeSystemCallSender = Hub.Services.GetRequiredService<RpcComputeSystemCallSender>();
        return computeSystemCallSender.Invalidate(Context.Peer, Id, ResultHeaders);
    }
}
