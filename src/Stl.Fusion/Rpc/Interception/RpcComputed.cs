using Stl.Fusion.Interception;
using Stl.Fusion.Rpc.Internal;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Rpc.Interception;

public interface IRpcComputed : IComputed, IDisposable
{
    RpcOutboundCall? Call { get; }
}

public class RpcComputed<T> : ComputeMethodComputed<T>, IRpcComputed
{
    RpcOutboundCall? IRpcComputed.Call => Call;
    public RpcOutboundComputeCall<T>? Call { get; }

    public RpcComputed(
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

        var whenInvalidatedAwaiter = call.WhenInvalidated.GetAwaiter();
        if (whenInvalidatedAwaiter.IsCompleted)
            Invalidate(true);
        else {
            whenInvalidatedAwaiter.OnCompleted(() => Invalidate(true));
            StartAutoInvalidation();
        }
    }

#pragma warning disable MA0055
    ~RpcComputed() => Dispose();
#pragma warning restore MA0055

    public void Dispose()
    {
        var call = Call;
        if (call == null)
            return;

        var peer = call.Context.Peer;
        if (peer == null)
            return;

        peer.Calls.Outbound.TryRemove(call.Id, call);
        if (!this.IsInvalidated()) {
            var systemCallSender = peer.Hub.Internals.SystemCallSender;
            _ = systemCallSender.Cancel(peer, call.Id);
        }
    }

    protected override void OnInvalidated()
    {
        // PseudoUnregister is used here just to trigger the
        // Unregistered event in ComputedRegistry.
        // We want to keep this computed while it's possible:
        // ReplicaMethodFunction.Compute tries to use it
        // to find a Replica to update through.
        // If this computed instance is gone from registry,
        // a new Replica is going to be created for each call
        // to replica method.
        ComputedRegistry.Instance.PseudoUnregister(this);
        CancelTimeouts();
        if (Function is IRpcComputeMethodFunction fn)
            fn.OnInvalidated(this);
    }
}
