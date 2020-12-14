using System;
using System.Runtime.CompilerServices;

namespace Stl.Collections
{
    public struct Collector<T> : IDisposable
    {
        private ArrayBuffer<T> _buffer;

        public Span<T> Items => _buffer.Span;
        public int Count => _buffer.Count;
        public T[] Buffer => _buffer.Buffer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Collector(bool mustClean, int capacity)
            => _buffer = ArrayBuffer<T>.Lease(mustClean, capacity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Collector<T> New(bool mustClean)
            => new(mustClean, ArrayBuffer<T>.MinCapacity);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Collector<T> New(bool mustClean, int capacity)
            => new(mustClean, capacity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
            => _buffer.Dispose();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
            => _buffer.Add(item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T>.Enumerator GetEnumerator()
            => _buffer.GetEnumerator();
    }
}
