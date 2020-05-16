using System.Collections.Generic;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public static class ReplicatorEx
    {
        public static IReplica Get(this IReplicator replicator, Symbol publicationId) 
            => replicator.TryGet(publicationId) ?? throw new KeyNotFoundException();

        public static IReplica<T>? TryGet<T>(this IReplicator replicator, Symbol publicationId)
            => replicator.TryGet(publicationId) as IReplica<T>;
        public static IReplica<T> Get<T>(this IReplicator replicator, Symbol publicationId)
            => (IReplica<T>) replicator.Get(publicationId);
    }
}
