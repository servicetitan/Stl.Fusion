using Stl.Fusion.Client.Internal;
using Stl.Fusion.Interception;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Client.Interception;

public interface IClientComputed : IComputed, IDisposable
{
    RpcOutboundCall? Call { get; }
}

public class ClientComputed<T> : ComputeMethodComputed<T>, IClientComputed
{
    RpcOutboundCall? IClientComputed.Call => Call;
    public RpcOutboundComputeCall<T>? Call { get; }

    public ClientComputed(
        ComputedOptions options,
        ComputeMethodInput input,
        Result<T> output,
        LTag version,
        bool isConsistent,
        RpcOutboundComputeCall<T>? call = null)
        : base(options, input, output, version, isConsistent)
    {
        Call = call;
        if (call == null) {
            StartAutoInvalidation();
            return;
        }

        var whenInvalidated = call.WhenInvalidated;
        if (whenInvalidated.IsCompleted)
            Invalidate(true);
        else {
            _ = whenInvalidated.ContinueWith(
                _ => Invalidate(true),
                CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            StartAutoInvalidation();
        }
    }

#pragma warning disable MA0055
    ~ClientComputed() => Dispose();
#pragma warning restore MA0055

    public void Dispose()
    {
        var call = Call;
        if (call == null)
            return;

        var peer = call.Context.Peer;
        if (peer == null)
            return;

        peer.Calls.Outbound.Remove(call.Id, out _);
        if (!this.IsInvalidated()) {
            try {
                var systemCallSender = peer.Hub.InternalServices.SystemCallSender;
                _ = systemCallSender.Cancel(peer, call.Id);
            }
            catch {
                // It's totally fine to ignore any error here:
                // peer.Hub might be already disposed at this point,
                // so SystemCallSender might not be available.
                // In any case, peer on the other side is going to
                // be gone as well after that, so every call there
                // will be cancelled anyway.
            }
        }
    }

    protected override void OnInvalidated()
    {
        // PseudoUnregister is used here just to trigger the
        // Unregistered event in ComputedRegistry.
        // We want to keep this computed while it's possible:
        // ClientComputeMethodFunction.Compute tries to reuse it
        // to avoid the computation (RPC call).
        ComputedRegistry.Instance.PseudoUnregister(this);
        CancelTimeouts();
        if (Function is IClientComputeMethodFunction fn)
            fn.OnInvalidated(this);
    }
}
