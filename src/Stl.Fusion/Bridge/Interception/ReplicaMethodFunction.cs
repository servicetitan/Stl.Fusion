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

    protected override async ValueTask<Computed<T>> Compute(
        ComputedInput input, Computed<T>? existing,
        CancellationToken cancellationToken)
    {
        var typedInput = (ComputeMethodInput) input;
        var methodDef = typedInput.MethodDef;

        // 1. Trying to update the Replica first
        if (existing is ReplicaMethodComputed<T> { Replica: { State: { } state } replica }) {
            if (!state.IsConsistent) {
                // Every time we call Update on replica, it calls RequestUpdate automatically
                // if the state is consistent, which delivers invalidations.
                // But here we request an update when the state is inconsistent,
                // i.e. here we request the new data _after_ the invalidation.
                await replica.RequestUpdate().WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            var computed = replica.RenewComputed(
                typedInput,
                static (replica, state, typedInput1) => new ReplicaMethodComputed<T>(
                    typedInput1.MethodDef.ComputedOptions, typedInput1, replica, state));
            if (computed == null)
                goto renewReplica;

            ComputeContext.Current.TryCapture(computed);
            return computed;
            // If we're here, computed == null, which means replica.IsDisposed == true
        }

        renewReplica:

        // 2. Replica update failed, let's refresh it
        Result<T> output;
        PublicationStateInfo? psi;
        using (var psiCapture = new PublicationStateInfoCapture()) {
            try {
                var rpcResult = typedInput.InvokeOriginalFunction(cancellationToken);
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
                DebugLog?.LogError(e, "Compute: failed to fetch the initial value");
                if (e is AggregateException ae)
                    e = ae.GetFirstInnerException();
                output = Result.Error<T>(e);
            }
            psi = psiCapture.Captured;
        }

        // 3. No PublicationStateInfo - all we can do is to expose this as an error 
        if (psi == null) {
            output = new Result<T>(default!, Errors.NoPublicationStateInfo());
            // We need a unique LTag here, so we use a range that's supposed to be unused by LTagGenerators.
            var version = new LTag(VersionGenerator.NextVersion().Value ^ (1L << 62));
            var computed = new ReplicaMethodComputed<T>(methodDef.ComputedOptions, typedInput, output.Error!, version);
            ComputeContext.Current.TryCapture(computed);
            return computed;
        }

        // 4. Create new Replica<T> & Computed<T>
        {
            var typedPsi = new PublicationStateInfo<T>(psi, output);
            replica = Replicator.AddOrUpdate(typedPsi);
            var computed = replica.RenewComputed(
                typedInput,
                static (replica, state, typedInput1) => new ReplicaMethodComputed<T>(
                    typedInput1.MethodDef.ComputedOptions, typedInput1, replica, state));
            if (computed == null) {
                DebugLog?.LogError("Compute: the replica was disposed right after it was created");
                goto renewReplica;
            }

            ComputeContext.Current.TryCapture(computed);
            return computed;
        }
    }
}
