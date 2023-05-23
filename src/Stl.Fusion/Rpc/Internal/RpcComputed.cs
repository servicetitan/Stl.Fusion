using Stl.Fusion.Interception;

namespace Stl.Fusion.Rpc.Internal;

public interface IRpcComputed : IComputed
{
    LTag RemoteVersion { get; }
}

public class RpcComputed<T> : ComputeMethodComputed<T>, IRpcComputed
{
    public LTag RemoteVersion { get; }

    public RpcComputed(
        ComputedOptions options,
        ComputeMethodInput input,
        Result<T> output,
        LTag remoteVersion,
        LTag version,
        bool isConsistent)
        : base(options, input, output, version, isConsistent)
    {
        RemoteVersion = remoteVersion;
        StartAutoInvalidation();
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
