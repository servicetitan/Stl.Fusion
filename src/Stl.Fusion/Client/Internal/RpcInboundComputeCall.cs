using Stl.Internal;
using Stl.Rpc;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Internal;

public interface IRpcInboundComputeCall
{ }

public class RpcInboundComputeCall<TResult> : RpcInboundCall<TResult>, IRpcInboundComputeCall
{
    private CancellationTokenSource? _stopCompletionSource;

    public Computed<TResult>? Computed { get; protected set; }

    public RpcInboundComputeCall(RpcInboundContext context, RpcMethodDef methodDef)
        : base(context, methodDef)
    {
        if (NoWait)
            throw Errors.InternalError($"{GetType().GetName()} is incompatible with NoWait option.");
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
                    Computed ??= computed;
            }
            ccs.Dispose();
        }
    }

    protected override Task CompleteSendResult()
    {
        Computed<TResult>? computed;
        CancellationToken stopCompletionToken;
        lock (Lock) {
            // 1. Check if we even need to do any work here
            if (CancellationToken.IsCancellationRequested) {
                Unregister();
                return Task.CompletedTask;
            }

            // 2. Cancel already running completion first
            _stopCompletionSource.CancelAndDisposeSilently();
            var stopCompletionSource = CancellationToken.CreateLinkedTokenSource();
            stopCompletionToken = stopCompletionSource.Token;
            _stopCompletionSource = stopCompletionSource;

            // 3. Retrieve Computed + update ResultHeaders
            computed = Computed;
            if (computed != null) {
                var versionHeader = FusionRpcHeaders.Version with { Value = computed.Version.ToString() };
                ResultHeaders = ResultHeaders.TryAdd(versionHeader);
            }
        }

        // 4. Actually run completion
        return CompleteAsync();

        async Task CompleteAsync() {
            var mustUnregister = false;
            try {
                await SendResult().WaitAsync(stopCompletionToken).ConfigureAwait(false);
                if (computed != null) {
                    await computed.WhenInvalidated(stopCompletionToken).ConfigureAwait(false);
                    await SendInvalidation().ConfigureAwait(false);
                }
                mustUnregister = true;
            }
            finally {
                if (mustUnregister || CancellationToken.IsCancellationRequested)
                    Unregister();
            }
        }
    }

    protected override bool Unregister()
    {
        lock (Lock) {
            if (!Context.Peer.InboundCalls.Unregister(this))
                return false; // Already completed or NoWait

            CancellationTokenSource.DisposeSilently();
            _stopCompletionSource.DisposeSilently();
        }
        return true;
    }

    private Task SendInvalidation()
    {
        var computeSystemCallSender = Hub.Services.GetRequiredService<RpcComputeSystemCallSender>();
        return computeSystemCallSender.Invalidate(Context.Peer, Id, ResultHeaders);
    }
}
