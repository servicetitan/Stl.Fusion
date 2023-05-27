using Stl.Fusion.Interception;
using Stl.Fusion.Rpc.Internal;
using Stl.Rpc.Infrastructure;

namespace Stl.Fusion.Rpc.Interception;

public interface IRpcComputed : IComputed
{
    RpcOutboundCall? Call { get; }
    LTag RemoteVersion { get; }
}

public class RpcComputed<T> : ComputeMethodComputed<T>, IRpcComputed
{
    RpcOutboundCall? IRpcComputed.Call => Call;
    public RpcOutboundComputeCall<T>? Call { get; }
    public LTag RemoteVersion { get; }

    public RpcComputed(
        ComputedOptions options,
        ComputeMethodInput input,
        Result<T> output,
        LTag version,
        bool isConsistent,
        RpcOutboundComputeCall<T>? call = null,
        LTag remoteVersion = default)
        : base(options, input, output, version, isConsistent)
    {
        Call = call;
        RemoteVersion = remoteVersion;
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
