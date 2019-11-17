using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;
using Stl.Collections;
using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Reflection;

namespace Stl.ImmutableModel 
{
    [Serializable]
    public abstract class CollectionNodeBase : NodeBase, ICollectionNode
    {
        internal static NodeTypeDef CreateNodeTypeDef(Type type) => new CollectionNodeTypeDef(type);

        // That's just a tagging base type for all collection nodes; 
        // it doesn't expose any public members - you have to cast
        // the instance to ICollectionNode to access these.

        IEnumerable<Symbol> ICollectionNode.Keys => throw new NotImplementedException();
        IEnumerable<object?> ICollectionNode.Values => throw new NotImplementedException();
        IEnumerable<KeyValuePair<Symbol, object?>> ICollectionNode.Items => throw new NotImplementedException();

        object? ICollectionNode.this[Symbol key] {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        bool ICollectionNode.ContainsKey(Symbol key) => throw new NotImplementedException();
        bool ICollectionNode.TryGetValue(Symbol key, out object? value) => throw new NotImplementedException();
        void ICollectionNode.Add(Symbol key, object? value) => throw new NotImplementedException();
        bool ICollectionNode.Remove(Symbol key) => throw new NotImplementedException();
        void ICollectionNode.Clear() => throw new NotImplementedException();
    }

    [Serializable]
    [JsonObject]
    public class CollectionNode<T> : CollectionNodeBase, ICollectionNode<T>
    {
        [JsonProperty(PropertyName = "@Items")]
        protected ChangeTrackingDictionary<Symbol, T> Items = ChangeTrackingDictionary<Symbol, T>.Empty;

        [JsonIgnore]
        public int Count => Items.Count;
        [JsonIgnore]
        public bool IsReadOnly => IsFrozen;

        IEnumerable<Symbol> ICollectionNode.Keys 
            => Items.Keys;
        IEnumerable<object?> ICollectionNode.Values 
            => Items.Values.Select(v => (object?) v);
        IEnumerable<KeyValuePair<Symbol, object?>> ICollectionNode.Items 
            => Items.Select(p => KeyValuePair.Create(p.Key, (object?) p.Value));
        // Methods returning ICollection<...> can't be implemented over ImmutableDictionary<...>
        // w/o a significant perf degrade, so... 
        ICollection<Symbol> IDictionary<Symbol, T>.Keys => throw new NotSupportedException();
        ICollection<T> IDictionary<Symbol, T>.Values => throw new NotSupportedException();

        object? ICollectionNode.this[Symbol key] {
            get => this[key];
            set => this[key] = (T) value!;
        }
        public T this[Symbol key] {
            get => Items[key];
            set => Items = Items.SetItem(key, PrepareValue(key, value));
        }

        IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
        public IEnumerator<KeyValuePair<Symbol, T>> GetEnumerator() => Items.GetEnumerator();

        public bool ContainsKey(Symbol key) => Items.ContainsKey(key);
        public bool Contains(KeyValuePair<Symbol, T> item) => Items.Contains(item);

        bool ICollectionNode.TryGetValue(Symbol key, out object? value)
        {
            if (TryGetValue(key, out var v)) {
                value = v;
                return true;
            }
            value = null;
            return false;
        }
        public bool TryGetValue(Symbol key, out T value) 
            => Items.TryGetValue(key, out value);

        public void Clear()
        {
            this.ThrowIfFrozen();
            Items = ChangeTrackingDictionary<Symbol, T>.Empty; 
        }

        void ICollectionNode.Add(Symbol key, object? value) => Add(key, (T) value!);
        public void Add(KeyValuePair<Symbol, T> item) 
            => Add(item.Key, item.Value);
        public void Add(Symbol key, T value) 
            => Items = Items.Add(key, PrepareValue(key, value));

        public bool Remove(Symbol key)
        {
            this.ThrowIfFrozen();
            var newItems = Items.Remove(key);
            if (ReferenceEquals(newItems.Dictionary, Items.Dictionary))
                return false;
            Items = newItems;
            return true;
        }

        public bool Remove(KeyValuePair<Symbol, T> item)
        {
            this.ThrowIfFrozen();
            var (key, value) = item;
            if (!TryGetValue(key, out var v))
                return false;
            if (!EqualityComparer<T>.Default.Equals(value, v))
                return false;
            Items = Items.Remove(key);
            return true;
        }

        public void CopyTo(KeyValuePair<Symbol, T>[] array, int arrayIndex)
            => Items.ToArray().CopyTo(array, arrayIndex);

        // IFreezable

        public override IFreezable BaseDefrost(bool deep = false)
        {
            var clone = (CollectionNode<T>) base.BaseDefrost(deep);
            clone.DiscardChangeHistory();
            return clone;
        }

        // IHasChangeHistory<T>

        (object? BaseState, object? CurrentState, ImmutableDictionary<Symbol, (DictionaryEntryChangeType ChangeType, T Value)> Changes) IHasChangeHistory<T>.GetChangeHistory()
            => GetChangeHistory();
        protected virtual (object? BaseState, object? CurrentState, ImmutableDictionary<Symbol, (DictionaryEntryChangeType ChangeType, T Value)> Changes) GetChangeHistory() 
            => (Items.Base, Items.Dictionary, Items.Changes);

        protected override (object? BaseState, object? CurrentState, IEnumerable<(Symbol LocalKey, DictionaryEntryChangeType ChangeType, object? Value)> Changes) GetChangeHistoryUntyped()
            => (Items.Base, Items.Dictionary, 
                Items.Changes.Select(p => (p.Key, p.Value.ChangeType, (object?) p.Value.Value)));

        protected override void DiscardChangeHistory()
            => Items = new ChangeTrackingDictionary<Symbol,T>(Items.Dictionary);
    }
}
