using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl;
using Stl.ImmutableModel.Internal;

namespace Stl.ImmutableModel.Updating
{
    public readonly struct ModelChangeSet : IReadOnlyDictionary<Key, NodeChangeType>, IEquatable<ModelChangeSet>
    {
        public static ModelChangeSet Empty { get; } = new ModelChangeSet(ImmutableDictionary<Key, NodeChangeType>.Empty);

        [JsonIgnore]
        public int Count => Items.Count;
        [JsonIgnore]
        public IEnumerable<Key> Keys => Items.Keys;
        [JsonIgnore]
        public IEnumerable<NodeChangeType> Values => Items.Values;

        public NodeChangeType this[Key key] => Items[key];

        public ImmutableDictionary<Key, NodeChangeType> Items { get; }

        [JsonConstructor] 
        public ModelChangeSet(ImmutableDictionary<Key, NodeChangeType> items) 
            => Items = items;

        public override string ToString() => $"{GetType().Name}({Items.Count} item(s))";

        // IReadOnlyDictionaryPlus<Key, NodeChangeType> methods 

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<KeyValuePair<Key, NodeChangeType>> GetEnumerator() 
            => Items.GetEnumerator();

        public bool ContainsKey(Key key) => Items.ContainsKey(key);
        
        public bool TryGetValue(Key key, out NodeChangeType value) 
            => Items.TryGetValue(key, out value);
        public bool TryGetValueUntyped(Key key, out object? value)
        {
            if (Items.TryGetValue(key, out var v)) {
                value = v;
                return true;
            }
            value = null;
            return false;
        }

        // ModelChangeSet-specific operations

        public ModelChangeSet Add(Key key, NodeChangeType changeType)
        {
            changeType = Items.GetValueOrDefault(key) | changeType;
            return changeType == 0 ? this : new ModelChangeSet(Items.SetItem(key, changeType));
        }

        public ModelChangeSet Merge(ModelChangeSet other)
            => other.Items.Aggregate(this, (s, p) => s.Add(p.Key, p.Value));

        // Operators

        public static ModelChangeSet operator +(ModelChangeSet first, ModelChangeSet second) => first.Merge(second);

        // Equality

        public bool Equals(ModelChangeSet other) => Equals(Items, other.Items);
        public override bool Equals(object? obj) => obj is ModelChangeSet other && Equals(other);
        public override int GetHashCode() => Items.GetHashCode();
        public static bool operator ==(ModelChangeSet left, ModelChangeSet right) => left.Equals(right);
        public static bool operator !=(ModelChangeSet left, ModelChangeSet right) => !left.Equals(right);
    }
}
