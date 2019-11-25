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
    public abstract class CollectionNodeBase : Node
    {
        // Ideally it should implement ICollectionNode, but this means
        // this type has to either really implement its methods and pass
        // the calls to its protected abstract implementations (which
        // will just make these calls slower), or has to provide
        // fake implementation throwing ~ NotImplementedException
        // in assumption that it's anyway re-implemented in the
        // descendant -- which gives zero value + pollutes debugger
        // view b/c all of the properties implemented here will be shown
        // there (as a property that thrown an exception on attempt to
        // get its value).
        //
        // Long story short, since this is an abstract type, it's totally
        // fine to leave the implementation of ICollectionNode to
        // the descendant - the cast will always work anyway, the only
        // con is that static cast from this type won't work:
        // ICollectionNode icn = (CollectionNodeBase) someCollection;
        // - but who cares :) It's a rare case + dynamic cast will work
        // anyway.

        internal new static NodeTypeDef CreateNodeTypeDef(Type type) => new CollectionNodeTypeDef(type);

        protected CollectionNodeBase() { }
        protected CollectionNodeBase(Key key) : base(key) { }
    }

    [JsonObject]
    public partial class CollectionNode<T> : CollectionNodeBase, ICollectionNode<T>
    {
        [JsonIgnore]
        protected ChangeTrackingDictionary<Key, T> Items = ChangeTrackingDictionary<Key, T>.Empty;

        [JsonProperty("@Items")]
        private ImmutableDictionary<Key, T> JsonObjectItems {
            get => Items.Dictionary;
            set => Items = new ChangeTrackingDictionary<Key, T>(value);
        }

        [JsonIgnore]
        public int Count => Items.Count;
        [JsonIgnore]
        public bool IsReadOnly => IsFrozen;
        [JsonIgnore]
        public IEnumerable<Key> Keys => Items.Keys;
        [JsonIgnore]
        public IEnumerable<T> Values => Items.Values;

        IEnumerable<object?> ICollectionNode.Values 
            => Items.Values.Select(v => (object?) v);
        IEnumerable<KeyValuePair<Key, object?>> ICollectionNode.Items 
            => Items.Select(p => KeyValuePair.Create(p.Key, (object?) p.Value));
        ICollection<Key> IDictionary<Key, T>.Keys => new KeyCollection(this);
        ICollection<T> IDictionary<Key, T>.Values => new ValueCollection(this);

        object? ICollectionNode.this[Key key] {
            get => this[key];
            set => this[key] = (T) value!;
        }
        public T this[Key key] {
            get => Items[key];
            set => Items = Items.SetItem(key, PrepareItemValue(key, value));
        }

        public CollectionNode() { }
        public CollectionNode(Key key) : base(key) { }

        IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
        public IEnumerator<KeyValuePair<Key, T>> GetEnumerator() => Items.GetEnumerator();

        public bool ContainsKey(Key key) => Items.ContainsKey(key);
        public bool Contains(KeyValuePair<Key, T> item) => Items.Contains(item);

        bool ICollectionNode.TryGetValue(Key key, out object? value)
        {
            if (TryGetValue(key, out var v)) {
                value = v;
                return true;
            }
            value = null;
            return false;
        }
        public bool TryGetValue(Key key, out T value) 
            => Items.TryGetValue(key, out value);

        public void Clear()
        {
            this.ThrowIfFrozen();
            Items = ChangeTrackingDictionary<Key, T>.Empty; 
        }

        void ICollectionNode.Add(Key key, object? value) => Add(key, (T) value!);
        public void Add(KeyValuePair<Key, T> item) 
            => Add(item.Key, item.Value);
        public void Add(Key key, T value) 
            => Items = Items.Add(key, PrepareItemValue(key, value));

        public bool Remove(Key key)
        {
            this.ThrowIfFrozen();
            var newItems = Items.Remove(key);
            if (ReferenceEquals(newItems.Dictionary, Items.Dictionary))
                return false;
            Items = newItems;
            return true;
        }

        public bool Remove(KeyValuePair<Key, T> item)
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

        public void CopyTo(KeyValuePair<Key, T>[] array, int arrayIndex)
            => Items.ToArray().CopyTo(array, arrayIndex);

        // IFreezable

        public override IFreezable BaseToUnfrozen(bool deep = false)
        {
            var clone = (CollectionNode<T>) base.BaseToUnfrozen(deep);
            clone.DiscardChangeHistory();
            return clone;
        }

        // IHasChangeHistory<T>

        (object? BaseState, object? CurrentState, ImmutableDictionary<Key, (DictionaryEntryChangeType ChangeType, T Value)> Changes) IHasChangeHistory<T>.GetChangeHistory()
            => GetChangeHistory();
        protected virtual (object? BaseState, object? CurrentState, ImmutableDictionary<Key, (DictionaryEntryChangeType ChangeType, T Value)> Changes) GetChangeHistory() 
            => (Items.Base, Items.Dictionary, Items.Changes);

        protected override (object? BaseState, object? CurrentState, IEnumerable<(Key Key, DictionaryEntryChangeType ChangeType, object? Value)> Changes) GetChangeHistoryUntyped()
            => (Items.Base, Items.Dictionary, 
                Items.Changes.Select(p => (p.Key, p.Value.ChangeType, (object?) p.Value.Value)));

        protected override void DiscardChangeHistory()
            => Items = new ChangeTrackingDictionary<Key,T>(Items.Dictionary);

        // Other protected & private methods

        protected T PrepareItemValue(Key key, T value)
        {
            this.ThrowIfFrozen();
            if (value is INode node && node.Key.IsNullOrUndefined()) {
                // We automatically provide keys for INode properties (or collection items)
                // by extending the owner's key with property name suffix 
                node.Key = key;
            }
            return value;
        }
    }
}
