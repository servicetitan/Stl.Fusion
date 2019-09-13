using System;
using Newtonsoft.Json;

namespace Stl.ImmutableModel
{
    [Serializable]
    public readonly struct Ref<TNode> : IEquatable<Ref<TNode>>
        where TNode : class, INode
    {
        public DomainKey DomainKey { get; }

        [JsonConstructor]
        public Ref(DomainKey domainKey) => DomainKey = domainKey;
        public Ref(Type domain, Key key) => DomainKey = (domain, key);

        public override string ToString() => $"{GetType()}({DomainKey.Domain}, {DomainKey.Key})";

        public TNode Resolve(IIndex index) => index.GetNode<TNode>(DomainKey);

        // Conversion

        public void Deconstruct(out Type domain, out Key key)
        {
            domain = DomainKey.Domain;
            key = DomainKey.Key;
        }

        public static implicit operator Ref<TNode>((Type Domain, Key Key) source) 
            => new Ref<TNode>(source.Domain, source.Key);

        // Equality

        public bool Equals(Ref<TNode> other) => DomainKey == other.DomainKey;
        public override bool Equals(object? obj) => obj is Ref<TNode> other && Equals(other);
        public override int GetHashCode() => DomainKey.GetHashCode();
        public static bool operator ==(Ref<TNode> left, Ref<TNode> right) => left.Equals(right);
        public static bool operator !=(Ref<TNode> left, Ref<TNode> right) => !left.Equals(right);
    }
}
