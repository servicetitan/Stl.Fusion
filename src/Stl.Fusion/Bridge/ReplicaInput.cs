using System;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public class ReplicaInput : ComputedInput, IEquatable<ReplicaInput>
    {
        protected internal readonly IReplicaImpl ReplicaImpl;
        protected internal IReplicatorImpl ReplicatorImpl => (IReplicatorImpl) Replicator;

        public Symbol PublisherId { get; }
        public Symbol PublicationId { get; }
        public IReplicator Replicator => ReplicaImpl.Replicator;
        public IReplica Replica => ReplicaImpl;

        public ReplicaInput(IReplicaImpl replicaImpl, Symbol publisherId, Symbol publicationId)
            : base(replicaImpl)
        {
            ReplicaImpl = replicaImpl;
            PublisherId = publisherId;
            PublicationId = publicationId;
        }

        public override string ToString()
            => $"{GetType().Name}({Replica.PublicationId})";

        // Equality

        public bool Equals(ReplicaInput? other)
            => !ReferenceEquals(null, other) && PublicationId == other.PublicationId;
        public override bool Equals(ComputedInput other)
            => other is ReplicaInput ri && PublicationId == ri.PublicationId;
        public override bool Equals(object? obj)
            => Equals(obj as ReplicaInput);
        public override int GetHashCode()
            => base.GetHashCode();
    }
}
