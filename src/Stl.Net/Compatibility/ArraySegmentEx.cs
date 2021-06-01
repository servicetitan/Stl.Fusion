#if NETSTANDARD2_0

using System;

namespace Stl.Net
{
    internal static class ArraySegmentEx
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
}

#endif
