using System;
using Stl.Text;

namespace Stl.Fusion.Bridge.Internal
{
    public class ReplicaInput : ComputedInput, IEquatable<ReplicaInput>
    {
        protected internal IReplicaImpl ReplicaImpl { get; }
        // Shortcuts
        public IReplicator Replicator => ReplicaImpl.Replicator;
        public Symbol PublisherId => ReplicaImpl.PublisherId;
        public Symbol PublicationId => ReplicaImpl.PublicationId;
        public IReplica Replica => ReplicaImpl;

        public ReplicaInput(IReplicaImpl replicaImpl) 
            : base(replicaImpl) 
            => ReplicaImpl = replicaImpl;

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
            => PublicationId.GetHashCode();
        public static bool operator ==(ReplicaInput? left, ReplicaInput? right) 
            => Equals(left, right);
        public static bool operator !=(ReplicaInput? left, ReplicaInput? right) 
            => !Equals(left, right);
    }
}
