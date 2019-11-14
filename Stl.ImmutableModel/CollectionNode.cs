using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stl.Collections;
using Stl.ImmutableModel.Reflection;

namespace Stl.ImmutableModel 
{
    [Serializable]
    public class CollectionNode<T> : NodeBase, ICollectionNode<T>
    {
        internal static NodeTypeDef CreateNodeTypeInfo(Type type) => new CollectionNodeTypeDef(type);

        protected ChangeTrackingDictionary<Symbol, T> Items = ChangeTrackingDictionary<Symbol, T>.Empty;

        public int Count => Items.Count;
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
            set => this[key] = (T) value;
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

        void ICollectionNode.Add(Symbol key, object? value) => Add(key, (T) value);
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
    }
}
