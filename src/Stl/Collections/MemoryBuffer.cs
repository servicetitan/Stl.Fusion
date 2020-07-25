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
    // ArrayBuffer<T> vs MemoryBuffer<T>: they are almost identical, but
    // ArrayBuffer isn't a ref struct, so you can store it in fields.
    public ref struct MemoryBuffer<T>
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                if (value < 0 || value > Capacity)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _count = value;
            }
        }

        public T this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => index < Count ? BufferSpan[index] : throw new IndexOutOfRangeException();
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                if (index >= Count) throw new IndexOutOfRangeException();
                BufferSpan[index] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MemoryBuffer(int capacity)
        {
            if (capacity < MinCapacity)
                capacity = MinCapacity;
            _lease = Pool.Rent(capacity);
            _count = 0;
            BufferSpan = _lease.Memory.Span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MemoryBuffer<T> Lease(int capacity = DefaultCapacity)
            => new MemoryBuffer<T>(capacity);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MemoryBuffer<T> LeaseAndSetCount(int count)
            => new MemoryBuffer<T>(count) {Count = count};

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            _lease?.Dispose();
            _lease = null!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T>.Enumerator GetEnumerator() => Span.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray() => Span.ToArray();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<T> ToList()
        {
            var list = new List<T>(Count);
            foreach (var item in Span)
                list.Add(item);
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            var newLease = Pool.Rent(capacity);
            if (capacity < Count) {
                Count = capacity;
                span = span.Slice(0, capacity);
            }
            span.CopyTo(newLease.Memory.Span);
            ChangeLease(newLease);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
