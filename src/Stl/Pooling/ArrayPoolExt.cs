using System;
using System.Buffers;

namespace Stl.Pooling
{
    public static class ArrayPoolExt
    {
        public readonly struct ArrayPoolLease<T> : IDisposable
        {
            public readonly ArrayPool<T> Pool;
            public readonly T[] Array;

            public ArrayPoolLease(ArrayPool<T> pool, int size)
            {
                Pool = pool;
                Array = pool.Rent(size);
            }

            public void Dispose()
                => Pool.Return(Array);
        }

        public static ArrayPoolLease<T> Lease<T>(this ArrayPool<T> pool, int size)
            => new(pool, size);
    }
}
