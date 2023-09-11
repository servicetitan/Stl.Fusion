using Stl.Internal;
using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Internal;

public class RpcInboundComputeCall<TResult> : RpcInboundCall<TResult>
{
    private CancellationTokenSource? _completionCancellationSource;

    public Computed<TResult>? Computed { get; protected set; }

    public RpcInboundComputeCall(RpcInboundContext context, RpcMethodDef methodDef)
        : base(context, methodDef)
    {
        if (NoWait)
            throw Errors.InternalError($"{GetType().GetName()} is incompatible with NoWait option.");
    }

    public override Task Complete(bool silentCancel = false)
    {
        silentCancel |= CancellationToken.IsCancellationRequested;
        if (silentCancel) {
            PrepareToComplete(silentCancel); // This call also cancels any other running completion
            return Task.CompletedTask;
        }

        CancellationToken cancellationToken;
        Computed<TResult>? computed;
        lock (Lock) {
            // Let's cancel already running completion first
            _completionCancellationSource.CancelAndDisposeSilently();
            _completionCancellationSource = CancellationToken.CreateLinkedTokenSource();
            cancellationToken = _completionCancellationSource.Token;
            computed = Computed;
            if (computed != null) {
                var versionHeader = FusionRpcHeaders.Version with { Value = computed.Version.ToString() };
                ResultHeaders = ResultHeaders.TryAdd(versionHeader);
            }
        }
        return cancellationToken.IsCancellationRequested
            ? Task.CompletedTask
            : computed == null
                ? base.Complete(silentCancel: false)
                : CompleteWithInvalidation(cancellationToken);
    }

    public override bool Restart()
    {
        if (!ResultTask.IsCompleted)
            return true; // Result isn't produced yet

        // Result is produced
        lock (Lock) {
            if (_completionCancellationSource == null)
                return true; // CompleteEventually haven't started yet -> let it do the job

            if (_completionCancellationSource.IsCancellationRequested)
                return false; // CompleteEventually already ended -> we have to restart

            // CompleteEventually is running - let's try to cancel it
            _completionCancellationSource.CancelAndDisposeSilently();
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

    private async Task CompleteWithInvalidation(CancellationToken cancellationToken)
    {
        await SendResult().ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested)
            return;

        await Computed!.WhenInvalidated(cancellationToken).SilentAwait(false);
        if (cancellationToken.IsCancellationRequested)
            return;

        lock (Lock) {
            _completionCancellationSource.CancelAndDisposeSilently();
            if (!PrepareToComplete())
                return;
        }
        await SendInvalidation().ConfigureAwait(false);
    }

    private Task SendInvalidation()
    {
        var computeSystemCallSender = Hub.Services.GetRequiredService<RpcComputeSystemCallSender>();
        return computeSystemCallSender.Invalidate(Context.Peer, Id, ResultHeaders);
    }
}
