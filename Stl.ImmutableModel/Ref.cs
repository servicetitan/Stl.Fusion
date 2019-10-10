using System;
using Newtonsoft.Json;
using Stl.ImmutableModel.Indexing;

namespace Stl.ImmutableModel
{
    [Serializable]
    public readonly struct Ref<TNode> : IEquatable<Ref<TNode>>
        where TNode : class, INode
    {
        public Key Key { get; }

        [JsonConstructor]
        public Ref(Key key) => Key = key;

        public override string ToString() => $"{GetType().Name}({Key})";

        public TNode Resolve(IModelIndex index) => index.GetNode<TNode>(Key);

        // Conversion

        public static implicit operator Ref<TNode>(Key key) => new Ref<TNode>(key);

        // Equality

        public bool Equals(Ref<TNode> other) => Key == other.Key;
        public override bool Equals(object? obj) => obj is Ref<TNode> other && Equals(other);
        public override int GetHashCode() => Key.GetHashCode();
        public static bool operator ==(Ref<TNode> left, Ref<TNode> right) => left.Equals(right);
        public static bool operator !=(Ref<TNode> left, Ref<TNode> right) => !left.Equals(right);
    }
}
