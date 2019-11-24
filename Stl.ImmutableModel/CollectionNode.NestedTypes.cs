using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stl.Text;

namespace Stl.ImmutableModel
{
    public partial class CollectionNode<T>
    {
        public class KeyCollection : ICollection<Key>
        {
            private readonly CollectionNode<T> _source;

            public int Count => _source.Count;
            public bool IsReadOnly => true;

            public KeyCollection(CollectionNode<T> source) => _source = source;

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public IEnumerator<Key> GetEnumerator() => _source.Items.Keys.GetEnumerator();

            public void CopyTo(Key[] array, int arrayIndex)
                => _source.Items.Keys.ToArray().CopyTo(array, arrayIndex);

            public bool Contains(Key item) => throw new NotSupportedException();
            public void Add(Key item) => throw new NotSupportedException();
            public bool Remove(Key item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }
        
        public class ValueCollection : ICollection<T>
        {
            private readonly CollectionNode<T> _source;

            public int Count => _source.Count;
            public bool IsReadOnly => true;

            public ValueCollection(CollectionNode<T> source) => _source = source;

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public IEnumerator<T> GetEnumerator() => _source.Items.Values.GetEnumerator();

            public void CopyTo(T[] array, int arrayIndex)
                => _source.Items.Values.ToArray().CopyTo(array, arrayIndex);

            public bool Contains(T item) => throw new NotSupportedException();
            public void Add(T item) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }
    }
}
