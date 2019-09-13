using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Stl.Serialization;

namespace Stl.ImmutableModel 
{
    [Serializable]
    public abstract class ImmutableDictionaryNodeBase<TKey, TValue> : NodeBase, 
        IReadOnlyDictionaryPlus<TKey, TValue>, INotifyDeserialized
        where TKey : notnull
    {
        private (TKey Key, object? Value)[]? _deserializingItems; 
        
        [field: NonSerialized]
        protected ImmutableDictionary<TKey, TValue> Items { get; set; } = 
            ImmutableDictionary<TKey, TValue>.Empty;

        public int Count => Items.Count;
        public IEnumerable<TKey> Keys => Items.Keys;
        public IEnumerable<TValue> Values => Items.Values;
        public TValue this[TKey key] => Items[key];

        protected ImmutableDictionaryNodeBase(Key key) : base(key) { }

        protected TSelf Update<TSelf>(TKey key, Option<TValue> value)
            where TSelf : ImmutableDictionaryNodeBase<TKey, TValue> 
        {
            var clone = (TSelf) MemberwiseClone();
            var items = clone.Items;
            items = value.HasValue 
                ? items.SetItem(key, value.UnsafeValue) 
                : items.Remove(key);
            clone.Items = items;
            return clone;
        }

        protected TSelf Update<TSelf>(IEnumerable<(TKey Key, Option<TValue> Value)> changes)
            where TSelf : ImmutableDictionaryNodeBase<TKey, TValue> 
        {
            var clone = (TSelf) MemberwiseClone();
            var items = clone.Items;
            foreach (var (key, value) in changes) {
                items = value.HasValue 
                    ? items.SetItem(key, value.UnsafeValue) 
                    : items.Remove(key);
            }
            clone.Items = items;
            return clone;
        }

        public bool ContainsKey(TKey key) => Items.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value) => Items.TryGetValue(key, out value);
        public bool TryGetValueUntyped(TKey key, out object? value)
        {
            if (TryGetValue(key, out var v)) {
                // ReSharper disable once HeapView.BoxingAllocation
                value = v;
                return true;
            }
            value = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Items.GetEnumerator();

        // Serialization

        protected ImmutableDictionaryNodeBase(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _deserializingItems = ((TKey Key, object? Value)[]) info.GetValue(nameof(Items), typeof(object))!;
        }

        // Complex, b/c JSON.NET doesn't allow [OnDeserialized] methods to be virtual
        [OnDeserialized] protected void OnDeserializedHandler(StreamingContext context) => OnDeserialized(context);
        void INotifyDeserialized.OnDeserialized(StreamingContext context) => OnDeserialized(context);
        protected virtual void OnDeserialized(StreamingContext context)
        {
            if (_deserializingItems == null)
                return; // JSON deserialization / something else
            var typedItems = _deserializingItems
                .Select(p => new KeyValuePair<TKey, TValue>(p.Key, (TValue) p.Value!))
                .ToArray();
            Items = ImmutableDictionary<TKey, TValue>.Empty.AddRange(typedItems);
            _deserializingItems = null;
            foreach (var value in Items.Values)
                if (value is INotifyDeserialized d)
                    d.OnDeserialized(context);
        }


        protected override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            var children = Items
                // ReSharper disable once HeapView.BoxingAllocation
                .Select(p => (p.Key, Value: (object?) p.Value))
                .ToArray();
            info.AddValue(nameof(Items), children);
        }
    }
}
