#if NETSTANDARD2_0

using System;
using System.Buffers;

namespace Stl.Net
{
    internal static class ArrayPoolEx
    {
        private sealed class ArrayPoolBuffer<T> : IArrayOwner<T>, IDisposable
        {
            private ArrayPool<T> _pool;
            private T[]? _array;

            public ArrayPoolBuffer(ArrayPool<T> pool, int size)
            {
                if (pool == null) throw new ArgumentNullException(nameof(pool));
                this._pool = pool;
                this._array = pool.Rent(size);
            }

            public void Dispose()
            {
                T[]? array = this._array;
                if (array == null)
                    return;
                this._array = (T[]?) null;
                _pool.Return(array);
            }

            public T[] Array => _array ?? throw new ObjectDisposedException("Array returned back to pool.", (Exception?)null);
        }

        public static IArrayOwner<T> RentAsOwner<T>(this ArrayPool<T> pool, int size)
        {
            return new ArrayPoolBuffer<T>(pool, size);
        }
    }
}

#endif
