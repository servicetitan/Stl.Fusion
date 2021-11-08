using Stl.Fusion.Internal;

namespace Stl.Fusion.Bridge;

public static class ReplicatorExt
{
    private static readonly Exception ReplicaHasBeenNeverUpdatedError =
        Errors.ReplicaHasNeverBeenUpdated();

    public static IReplica<T> GetOrAdd<T>(this IReplicator replicator,
        PublicationRef publicationRef, bool requestUpdate = false)
    {
        var output = new Result<T>(default!, ReplicaHasBeenNeverUpdatedError);
        var info = new PublicationStateInfo<T>(publicationRef, LTag.Default, false, output);
        return replicator.GetOrAdd(info, requestUpdate);
    }
}
