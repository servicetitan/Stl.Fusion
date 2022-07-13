using Stl.Fusion.Internal;

namespace Stl.Fusion.Bridge;

public static class ReplicatorExt
{
    private static readonly Exception ReplicaHasBeenNeverUpdatedError =
        Errors.ReplicaHasNeverBeenUpdated();

    public static IReplica<T> GetOrAdd<T>(this IReplicator replicator,
        PublicationRef publicationRef, bool requestUpdate = false)
        => replicator.GetOrAdd<T>(ComputedOptions.ReplicaDefault, publicationRef, requestUpdate);

    public static IReplica<T> GetOrAdd<T>(this IReplicator replicator,
        PublicationStateInfo<T> publicationStateInfo, bool requestUpdate = false)
        => replicator.GetOrAdd(ComputedOptions.ReplicaDefault, publicationStateInfo, requestUpdate);

    public static IReplica<T> GetOrAdd<T>(this IReplicator replicator,
        ComputedOptions computedOptions, PublicationRef publicationRef, bool requestUpdate = false)
    {
        var output = new Result<T>(default!, ReplicaHasBeenNeverUpdatedError);
        var info = new PublicationStateInfo<T>(publicationRef, LTag.Default, false, output);
        return replicator.GetOrAdd(computedOptions, info, requestUpdate);
    }
}
