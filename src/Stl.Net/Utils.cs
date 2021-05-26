using System;
using System.Buffers;
using System.Text;

#if NETSTANDARD2_0

namespace Stl.Net
{
    public interface IArrayOwner<T> : IDisposable
    {
        T[] Array { get; }
    }
    
    public static class ArrayPoolEx
    {
        private sealed class ArrayPoolBuffer<T> : IArrayOwner<T>, IDisposable
        {
            private ArrayPool<T> _pool;
            private T[] _array;

            public ArrayPoolBuffer(ArrayPool<T> pool, int size)
            {
                if (pool == null) throw new ArgumentNullException(nameof(pool));
                this._pool = pool;
                this._array = pool.Rent(size);
            }

            public void Dispose()
            {
                T[] array = this._array;
                if (array == null)
                    return;
                this._array = (T[]) null;
                _pool.Return(array);
            }

            public T[] Array => _array;
        }

        public static IArrayOwner<T> RentAsOwner<T>(this ArrayPool<T> pool, int size)
        {
            return new ArrayPoolBuffer<T>(pool, size);
        }
    }

    public static class ArraySegmentEx
    {
        public static ArraySegment<T> Slice<T>(this ArraySegment<T> arraySegment, int start)
        {
            if (start > arraySegment.Count)
                throw new ArgumentOutOfRangeException(nameof(start));
            return new ArraySegment<T>(arraySegment.Array, arraySegment.Offset + start, arraySegment.Count - start);
        }
        
        public static ArraySegment<T> Slice<T>(this ArraySegment<T> arraySegment, int start, int length)
        {
            if (start + length > arraySegment.Count)
                throw new ArgumentOutOfRangeException(nameof(start));
            return new ArraySegment<T>(arraySegment.Array, arraySegment.Offset + start, length);
        }

        public static void CopyTo<T>(this ArraySegment<T> arraySegment, T[] dest)
        {
            CopyTo<T>(arraySegment, dest, 0);
        }

        public static void CopyTo<T>(this ArraySegment<T> arraySegment, T[] dest, int arrayIndex)
        {
            System.Array.Copy(arraySegment.Array, arraySegment.Offset, dest, arrayIndex, arraySegment.Count);
        }

        public static string ToStringEx(this ArraySegment<char> chars)
        {
            return new string(chars.Array, chars.Offset, chars.Count);
        }
    }

    public static class StringBuilderEx
    {
        public static void Append(this StringBuilder sb, ArraySegment<char> chars)
        {
            if (sb == null) throw new ArgumentNullException(nameof(sb));
            sb.Append(chars.Array, chars.Offset, chars.Count);
        }
    }
}

#endif