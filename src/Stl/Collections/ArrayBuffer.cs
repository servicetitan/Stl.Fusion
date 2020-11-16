using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Stl.Internal;

namespace Stl.Collections
{
    // List-like struct that typically requires zero allocations
    // (it relies on ArrayPool<T>.Shared & disposes its buffer);
    // it is supposed to be used as a temp. buffer in various
    // enumeration scenarios.
    // ArrayBuffer<T> vs MemoryBuffer<T>: they are almost identical, but
    // ArrayBuffer isn't a ref struct, so you can store it in fields.
    public struct ArrayBuffer<T> : IDisposable
    {
        public const int MinCapacity = 1;
        public const int DefaultCapacity = 16;
        private static readonly ArrayPool<T> Pool = ArrayPool<T>.Shared;

        private int _count;

        public T[] Buffer { get; private set; }
        public Span<T> Span => Buffer.AsSpan(0, Count);
        public int Capacity => Buffer.Length;
        public bool MustClean { get; }
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
            get => index < Count ? Buffer[index] : throw new IndexOutOfRangeException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ArrayBuffer(bool mustClean, int capacity)
        {
            MustClean = mustClean;
            if (capacity < MinCapacity)
                capacity = MinCapacity;
            Buffer = Pool.Rent(capacity);
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArrayBuffer<T> Lease(bool mustClean, int capacity = DefaultCapacity)
            => new ArrayBuffer<T>(mustClean, capacity);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArrayBuffer<T> LeaseAndSetCount(bool mustClean, int count)
            => new ArrayBuffer<T>(mustClean, count) {Count = count};

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (Buffer != null)
                Pool.Return(Buffer, MustClean);
            Buffer = null!;
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
        public void SetItem(int index, T item)
        {
            if (index >= Count) throw new IndexOutOfRangeException();
            Buffer[index] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
                Add(item);
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
            Buffer[Count++] = item;
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
            var span = Buffer.AsSpan(0, ++Count);
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
            var span = Buffer.AsSpan(0, Count--);
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

            var span = Buffer.AsSpan();
            var newLease = Pool.Rent(capacity);
            if (capacity < Count) {
                Count = capacity;
                span = span.Slice(0, capacity);
            }
            span.CopyTo(newLease.AsSpan());
            ChangeLease(newLease);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] array, int arrayIndex)
            => Buffer.CopyTo(array.AsSpan().Slice(arrayIndex));

        // Private methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ChangeLease(T[] newLease)
        {
            Pool.Return(Buffer, MustClean);
            Buffer = newLease;
        }
    }
}
