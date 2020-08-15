using System;

namespace Stl.Fusion.Bridge
{
    public class ReplicaInput : ComputedInput, IEquatable<ReplicaInput>
    {
        protected internal readonly IReplicaImpl ReplicaImpl;
        protected internal IReplicatorImpl ReplicatorImpl => (IReplicatorImpl) Replicator;

        public PublicationRef PublicationRef { get; }
        public IReplicator Replicator => ReplicaImpl.Replicator;
        public IReplica Replica => ReplicaImpl;

        public ReplicaInput(IReplicaImpl replicaImpl, PublicationRef publicationRef)
            : base(replicaImpl)
        {
            ReplicaImpl = replicaImpl;
            PublicationRef = publicationRef;
        }

        public override string ToString()
            => $"{GetType().Name}({Replica.PublicationRef})";

        // Equality

        public bool Equals(ReplicaInput? other)
            => !ReferenceEquals(null, other) && PublicationRef == other.PublicationRef;
        public override bool Equals(ComputedInput other)
            => other is ReplicaInput ri && PublicationRef == ri.PublicationRef;
        public override bool Equals(object? obj)
            => Equals(obj as ReplicaInput);
        public override int GetHashCode()
            => base.GetHashCode();
    }
}
