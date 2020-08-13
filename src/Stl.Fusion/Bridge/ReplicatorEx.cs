using System;
using System.Collections.Generic;
using Stl.Fusion.Internal;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public static class ReplicatorEx
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

        public static IReplica Get(this IReplicator replicator, PublicationRef publicationRef)
            => replicator.TryGet(publicationRef) ?? throw new KeyNotFoundException();

        public static IReplica<T>? TryGet<T>(this IReplicator replicator, PublicationRef publicationRef)
            => replicator.TryGet(publicationRef) as IReplica<T>;
        public static IReplica<T> Get<T>(this IReplicator replicator, PublicationRef publicationRef)
            => (IReplica<T>) replicator.Get(publicationRef);
    }
}
