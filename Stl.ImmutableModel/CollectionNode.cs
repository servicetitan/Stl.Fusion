using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl.Collections;
using Stl.ImmutableModel.Indexing;
using Stl.ImmutableModel.Reflection;

namespace Stl.ImmutableModel 
{
    public abstract class CollectionNodeBase : NodeBase
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

        internal static NodeTypeDef CreateNodeTypeDef(Type type) => new CollectionNodeTypeDef(type);
    }

    [JsonObject]
    public partial class CollectionNode<T> : CollectionNodeBase, ICollectionNode<T>
    {
        [JsonIgnore]
        protected ChangeTrackingDictionary<Symbol, T> Items = ChangeTrackingDictionary<Symbol, T>.Empty;

        [JsonProperty("@Items")]
        private ImmutableDictionary<Symbol, T> JsonObjectItems {
            get => Items.Dictionary;
            set => Items = new ChangeTrackingDictionary<Symbol, T>(value);
        }

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
        ICollection<Symbol> IDictionary<Symbol, T>.Keys => new KeyCollection(this);
        ICollection<T> IDictionary<Symbol, T>.Values => new ValueCollection(this);

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

        public override IFreezable BaseToUnfrozen(bool deep = false)
        {
            var clone = (CollectionNode<T>) base.BaseToUnfrozen(deep);
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
