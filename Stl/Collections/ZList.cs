using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using Stl.Internal;

namespace Stl.Collections
{
    // The list that typically requires zero allocations;
    // it is supposed to be used as a temp. buffer in various
    // enumeration scenarios. 
    public struct ZList<T> : IList<T>
    {
        public static Lease Rent(int capacity = MinCapacity) 
            => new Lease(new ZList<T>(capacity));

        public struct Lease : IDisposable
        {
            public ZList<T> List;

            internal Lease(ZList<T> list) => List = list;

            public void Dispose()
            {
                List._lease?.Dispose();
                List._lease = null!;
            }
        }

        public struct Enumerator : IEnumerator<T>
        {
            private ZList<T> _list;
            private int _index;

            public Enumerator(ZList<T> list) : this()
            {
                _index = -1;
                _list = list;
            }

            public bool MoveNext() => ++_index < _list.Count;
            public void Reset() => _index = -1;
            object? IEnumerator.Current => Current;
            public T Current => _list[_index];
            public void Dispose() { }
        }

        public const int MinCapacity = 16;
        private static readonly MemoryPool<T> Pool = MemoryPool<T>.Shared;

        private IMemoryOwner<T> _lease;
        public Memory<T> Buffer => _lease.Memory;
        public int Capacity => Buffer.Length;
        public int Count { get; private set; }
        public bool IsReadOnly => false;

        public T this[int index] {
            get => index < Count ? Buffer.Span[index] : throw new IndexOutOfRangeException();
            set {
                if (index >= Count) throw new IndexOutOfRangeException();
                Buffer.Span[index] = value;
            }
        }

        private ZList(int capacity)
        {
            if (capacity < MinCapacity)
                capacity = MinCapacity;
            _lease = Pool.Rent(capacity);
            Count = 0;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        Enumerator GetEnumerator() => new Enumerator(this); 

        public void Add(T item)
        {
            var capacity = Capacity;
            if (Count >= capacity) {
                var newCapacity = capacity << 1;
                if (newCapacity < capacity)
                    throw Errors.ZListIsTooLong();
                Resize(newCapacity);
            }
            this[Count++] = item;

        }

        public void Insert(int index, T item)
        {
            var capacity = Capacity;
            if (Count >= capacity) {
                var newCapacity = capacity << 1;
                if (newCapacity < capacity)
                    throw Errors.ZListIsTooLong();
                Resize(newCapacity);
            }
            var copyLength = Count - index;
            if (copyLength < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            var span = Buffer.Span.Slice(0, ++Count);
            var source = span.Slice(index, copyLength);
            var target = span.Slice(index + 1);
            source.CopyTo(target);
            span[index] = item;
        }

        public void RemoveAt(int index)
        {
            var copyLength = Count - index - 1;
            if (copyLength < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            var span = Buffer.Span.Slice(0, Count--);
            var source = span.Slice(index + 1, copyLength);
            var target = span.Slice(index);
            source.CopyTo(target);
        }

        public void Clear()
        {
            _lease.Dispose();
            _lease = Pool.Rent(MinCapacity);
            Count = 0;
        }

        public void Resize(int capacity)
        {
            if (capacity < MinCapacity)
                capacity = MinCapacity;

            var pool = Pool;
            var span = _lease.Memory.Span;
            var newLease = MemoryPool<T>.Shared.Rent(capacity);
            if (capacity < Count) {
                Count = capacity;
                span = span.Slice(0, capacity);
            }
            span.CopyTo(newLease.Memory.Span);
            _lease.Dispose();
            _lease = newLease;
        }

        public void CopyTo(T[] array, int arrayIndex) 
            => Buffer.Span.CopyTo(array.AsSpan().Slice(arrayIndex));

        // These methods aren't supported b/c otherwise
        // ZList<T> should have "where T : IEquatable<T>" constraint
        public bool Contains(T item) 
            => throw new NotSupportedException();
        public bool Remove(T item)
            => throw new NotSupportedException();
        public int IndexOf(T item)
            => throw new NotSupportedException();
    }
}
