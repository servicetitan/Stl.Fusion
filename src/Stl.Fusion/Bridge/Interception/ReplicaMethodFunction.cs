using Stl.Fusion.Bridge.Internal;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;
using Stl.Versioning;

namespace Stl.Fusion.Bridge.Interception;

public class ReplicaMethodFunction<T> : ComputeFunctionBase<T>
{
    protected readonly VersionGenerator<LTag> VersionGenerator;
    protected readonly IReplicator Replicator;

    public ReplicaMethodFunction(
        ComputeMethodDef methodDef,
        IReplicator replicator,
        VersionGenerator<LTag> versionGenerator)
        : base(methodDef, ((IReplicatorImpl) replicator).Services)
    {
        Replicator = replicator;
        VersionGenerator = versionGenerator;
    }

    protected override async ValueTask<IComputed<T>> Compute(
        ComputeMethodInput input, IComputed<T>? existing,
        CancellationToken cancellationToken)
    {
        var methodDef = input.MethodDef;
        IReplica<T> replica;
        IReplicaComputed<T> replicaComputed;
        ReplicaMethodComputed<T> result;

        // 1. Trying to update the Replica first
        if (existing is IReplicaMethodComputed<T> rsc && rsc.Replica != null) {
            try {
                replica = rsc.Replica;
                replicaComputed = (IReplicaComputed<T>)
                    await replica.Computed.Update(cancellationToken).ConfigureAwait(false);
                result = new (methodDef.ComputedOptions, input, replicaComputed);
                ComputeContext.Current.TryCapture(result);
                return result;
            }
            catch (Exception e) when (e is not OperationCanceledException) {
                DebugLog?.LogError(e, "ComputeAsync: error on Replica update");
            }
        }

        // 2. Replica update failed, let's refresh it
        Result<T> output;
        PublicationStateInfo? psi;
        using (var psiCapture = new PublicationStateInfoCapture()) {
            try {
                var rpcResult = input.InvokeOriginalFunction(cancellationToken);
                if (methodDef.ReturnsValueTask) {
                    var rpcResultTask = (ValueTask<T>) rpcResult;
                    output = Result.Value(await rpcResultTask.ConfigureAwait(false));
                }
                else {
                    var rpcResultTask = (Task<T>) rpcResult;
                    output = Result.Value(await rpcResultTask.ConfigureAwait(false));
                }
            }
            catch (Exception e) when (e is not OperationCanceledException) {
                DebugLog?.LogError(e, "ComputeAsync: error on update");
                if (e is AggregateException ae)
                    e = ae.GetFirstInnerException();
                output = Result.Error<T>(e);
            }
            psi = psiCapture.Captured;
        }

        if (psi == null) {
            output = new Result<T>(default!, Errors.NoPublicationStateInfo());
            // We need a unique LTag here, so we use a range that's supposed to be unused by LTagGenerators.
            var version = new LTag(VersionGenerator.NextVersion().Value ^ (1L << 62));
            result = new (methodDef.ComputedOptions, input, output.Error!, version);
            ComputeContext.Current.TryCapture(result);
            return result;
        }
        if (output.Error != null) {
            // Try to pull the server-side error first
            if (psi is PublicationStateInfo<object> { Output.HasError: true } errorPsi)
                output = Result.Error<T>(errorPsi.Output.Error!);
            else
                // No server-side error -> it's a client-side error
                output = Result.Error<T>(output.Error);
            // We need a unique LTag here, so we use a range that's supposed
            // to be unused by LTagGenerators.
            if (psi.Version == default)
                psi.Version = new LTag(VersionGenerator.NextVersion().Value ^ (1L << 62));
        }
        replica = Replicator.GetOrAdd(ComputedOptions, new PublicationStateInfo<T>(psi, output));
        replicaComputed = (IReplicaComputed<T>)
            await replica.Computed.Update(cancellationToken).ConfigureAwait(false);
        result = new ReplicaMethodComputed<T>(methodDef.ComputedOptions, input, replicaComputed);
        ComputeContext.Current.TryCapture(result);
        return result;
    }
}
