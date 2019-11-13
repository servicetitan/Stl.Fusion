using System;
using System.Buffers;
using Stl.Internal;

namespace Stl.Collections
{
    // List-like struct that typically requires zero allocations
    // (it relies on MemoryPool<T>.Shared & disposes its buffer);
    // it is supposed to be used as a temp. buffer in various
    // enumeration scenarios. 
    public ref struct ListBuffer<T>
    {
        public static Lease Rent(int capacity = MinCapacity) 
            => new Lease(new ListBuffer<T>(capacity));

        public ref struct Lease
        {
            public ListBuffer<T> Buffer;

            internal Lease(ListBuffer<T> buffer) => Buffer = buffer;

            public void Dispose()
            {
                Buffer._lease?.Dispose();
                Buffer._lease = null!;
            }
        }

        public ref struct Enumerator
        {
            private ListBuffer<T> _buffer;
            private int _index;

            public Enumerator(ListBuffer<T> buffer) : this()
            {
                _index = -1;
                _buffer = buffer;
            }

            public bool MoveNext() => ++_index < _buffer.Count;
            public void Reset() => _index = -1;
            public T Current => _buffer[_index];
            public void Dispose() { }
        }

        public const int MinCapacity = 16;
        private static readonly MemoryPool<T> Pool = MemoryPool<T>.Shared;

        private IMemoryOwner<T> _lease;
        public Memory<T> Buffer => _lease.Memory;
        public int Capacity => Buffer.Length;
        public int Count { get; private set; }
        public Span<T> Span => _lease.Memory.Span.Slice(0, Count);

        public T this[int index] {
            get => index < Count ? Buffer.Span[index] : throw new IndexOutOfRangeException();
            set {
                if (index >= Count) throw new IndexOutOfRangeException();
                Buffer.Span[index] = value;
            }
        }

        private ListBuffer(int capacity)
        {
            if (capacity < MinCapacity)
                capacity = MinCapacity;
            _lease = Pool.Rent(capacity);
            Count = 0;
        }

        public Enumerator GetEnumerator() => new Enumerator(this); 
        public T[] ToArray() => Span.ToArray();

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
    }
}
