using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Stl.Internal;

namespace Stl.Collections
{
    // List-like struct that typically requires zero allocations
    // (it relies on MemoryPool<T>.Shared & disposes its buffer);
    // it is supposed to be used as a temp. buffer in various
    // enumeration scenarios. 
    public ref struct ListBuffer<T>
    {
        public const int MinCapacity = 1;
        public const int DefaultCapacity = 16;
        private static readonly MemoryPool<T> Pool = MemoryPool<T>.Shared;

        private IMemoryOwner<T> _lease;
        private int _count;

        public Memory<T> BufferMemory => _lease.Memory;
        public Span<T> BufferSpan { get; private set; }
        public Span<T> Span => BufferSpan.Slice(0, Count);
        public int Capacity => BufferSpan.Length;
        public int Count {
            get => _count;
            set {
                if (value < 0 || value > Capacity)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _count = value;
            }
        }

        public T this[int index] {
            get => index < Count ? BufferSpan[index] : throw new IndexOutOfRangeException();
            set {
                if (index >= Count) throw new IndexOutOfRangeException();
                BufferSpan[index] = value;
            }
        }

        private ListBuffer(int capacity)
        {
            if (capacity < MinCapacity)
                capacity = MinCapacity;
            _lease = Pool.Rent(capacity);
            _count = 0;
            BufferSpan = _lease.Memory.Span;
        }

        public static ListBuffer<T> Lease(int capacity = DefaultCapacity) 
            => new ListBuffer<T>(capacity);
        public static ListBuffer<T> LeaseAndSetCount(int count) 
            => new ListBuffer<T>(count) {Count = count};

        public void Release()
        {
            _lease?.Dispose();
            _lease = null!;
        }

        public Span<T>.Enumerator GetEnumerator() => Span.GetEnumerator(); 
        
        public T[] ToArray() => Span.ToArray();
        public List<T> ToList()
        {
            var list = new List<T>(Count);
            foreach (var item in Span)
                list.Add(item);
            return list;
        }

        public void Add(T item)
        {
            var capacity = Capacity;
            if (Count >= capacity) {
                var newCapacity = capacity << 1;
                if (newCapacity < capacity)
                    throw Errors.ZListIsTooLong();
                Resize(newCapacity);
            }
            BufferSpan[Count++] = item;
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
            var span = BufferSpan.Slice(0, ++Count);
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
            var span = BufferSpan.Slice(0, Count--);
            var source = span.Slice(index + 1, copyLength);
            var target = span.Slice(index);
            source.CopyTo(target);
        }

        public void Clear()
        {
            ChangeLease(Pool.Rent(MinCapacity));
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
            ChangeLease(newLease);
        }

        public void CopyTo(T[] array, int arrayIndex) 
            => BufferSpan.CopyTo(array.AsSpan().Slice(arrayIndex));

        // Private methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ChangeLease(IMemoryOwner<T> newLease)
        {
            _lease.Dispose();
            _lease = newLease;
            BufferSpan = _lease.Memory.Span;
        }
    }
}
