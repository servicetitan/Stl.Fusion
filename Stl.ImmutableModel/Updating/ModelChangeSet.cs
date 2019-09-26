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
    [Serializable]
    public readonly struct ModelChangeSet : IReadOnlyDictionaryPlus<Key, NodeChangeType>,
        IEquatable<ModelChangeSet>, ISerializable 
    {
        public static ModelChangeSet Empty { get; } = new ModelChangeSet(ImmutableDictionary<Key, NodeChangeType>.Empty);

        private readonly ModelChangeSetDeserializationHelper? _deserializationHelper;
        private readonly ImmutableDictionary<Key, NodeChangeType>? _items;

        public int Count => Items.Count;
        public NodeChangeType this[Key key] => Items[key];
        public IEnumerable<Key> Keys => Items.Keys;
        public IEnumerable<NodeChangeType> Values => Items.Values;
        IEnumerable<KeyValuePair<Key, object?>> IReadOnlyDictionaryPlus<Key>.Items
            => Items.Select(p => KeyValuePair.New(p.Key, (object?) p.Value));

        public ImmutableDictionary<Key, NodeChangeType> Items {
            get {
                if (_items != null)
                    return _items;
                var items = _deserializationHelper?.GetImmutableDictionary()
                    ?? ImmutableDictionary<Key, NodeChangeType>.Empty;
                // Tricky: the struct is readonly (and ideally, must be);
                // the code below tries to overwrite it to fix the deserialization
                // + make sure the conversion from Dictionary to ImmutableDictionary
                // happens just once.
                ref var r = ref Unsafe.AsRef(this);
                r = new ModelChangeSet(items);
                return items;
            }
        }

        // The attribute isn't actually needed, since the type already impl. IReadOnlyDictionary.
        // But just in case...
        [JsonConstructor] 
        public ModelChangeSet(IDictionary<Key, NodeChangeType> items)
        {
            _deserializationHelper = null;
            _items = items.ToImmutableDictionary();
        }

        public ModelChangeSet(ImmutableDictionary<Key, NodeChangeType> items)
        {
            _deserializationHelper = null;
            _items = items;
        }

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

        // Serialization

        private ModelChangeSet(SerializationInfo info, StreamingContext context)
        {
            var d = (Dictionary<Key, NodeChangeType>) info.GetValue(nameof(Items), typeof(object))!;
            _deserializationHelper = new ModelChangeSetDeserializationHelper(d);
            _items = null;
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Items), Items.ToDictionary());
        }
    }
}
