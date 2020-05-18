using System;
using System.Collections.Generic;
using Stl.Fusion.Internal;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public static class ReplicatorEx
    {
        private static readonly Exception ReplicaHasBeenNeverUpdatedError = Errors.ReplicaHasBeenNeverUpdated();
        
        public static IReplica<T> GetOrAdd<T>(this IReplicator replicator, 
            Symbol publisherId, Symbol publicationId, bool requestUpdate = false)
        {
            var output = new Result<T>(default!, ReplicaHasBeenNeverUpdatedError);
            var initialOutput = new LTagged<Result<T>>(output, LTag.Default);
            return replicator.GetOrAdd(publisherId, publicationId, initialOutput, false, requestUpdate); 
        }

        public static IReplica Get(this IReplicator replicator, Symbol publicationId) 
            => replicator.TryGet(publicationId) ?? throw new KeyNotFoundException();

        public static IReplica<T>? TryGet<T>(this IReplicator replicator, Symbol publicationId)
            => replicator.TryGet(publicationId) as IReplica<T>;
        public static IReplica<T> Get<T>(this IReplicator replicator, Symbol publicationId)
            => (IReplica<T>) replicator.Get(publicationId);
    }
}
