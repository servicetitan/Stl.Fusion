using System.Buffers;

namespace Stl.Pooling;

public static class ArrayPoolExt
{
    public readonly struct ArrayPoolLease<T>(ArrayPool<T> pool, int size) : IDisposable
    {
        public readonly ArrayPool<T> Pool = pool;
        public readonly T[] Array = pool.Rent(size);

        public void Dispose()
            => Pool.Return(Array);
    }

    public static ArrayPoolLease<T> Lease<T>(this ArrayPool<T> pool, int size)
        => new(pool, size);
}
