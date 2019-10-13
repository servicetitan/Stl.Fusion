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
        public ImmutableDictionary<TKey, TValue> Items { get; protected set; } = ImmutableDictionary<TKey, TValue>.Empty;
        IEnumerable<KeyValuePair<TKey, object?>> IReadOnlyDictionaryPlus<TKey>.Items 
            => Items.Select(p => KeyValuePair.Create(p.Key, (object?) p.Value));

        public TValue this[TKey key] => Items[key];

        protected ImmutableDictionaryNodeBase(Key key) : base(key) { }

        protected TSelf BaseWith<TSelf>(TKey key, Option<TValue> value)
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

        protected TSelf BaseWith<TSelf>(IEnumerable<(TKey Key, Option<TValue> Value)> changes)
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

        protected TSelf BaseWithCleared<TSelf>()
            where TSelf : ImmutableDictionaryNodeBase<TKey, TValue> 
        {
            var clone = (TSelf) MemberwiseClone();
            clone.Items = clone.Items.Clear();
            return clone;
        }

        // Enumerators

        IEnumerator IEnumerable.GetEnumerator() 
            => Items.GetEnumerator();
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() 
            => Items.GetEnumerator();

        // IReadOnlyXxx members -- all are implemented explicitly

        int IReadOnlyCollection<KeyValuePair<TKey, TValue>>.Count => Items.Count;
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Items.Keys;
        IEnumerable<TKey> IReadOnlyDictionaryPlus<TKey>.Keys => Items.Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Items.Values;
        bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key) => Items.ContainsKey(key);
        bool IReadOnlyDictionaryPlus<TKey>.ContainsKey(TKey key) => Items.ContainsKey(key);
        bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value) => Items.TryGetValue(key, out value);
        bool IReadOnlyDictionaryPlus<TKey>.TryGetValueUntyped(TKey key, out object? value)
        {
            if (Items.TryGetValue(key, out var v)) {
                // ReSharper disable once HeapView.BoxingAllocation
                value = v;
                return true;
            }
            value = null;
            return false;
        }

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
