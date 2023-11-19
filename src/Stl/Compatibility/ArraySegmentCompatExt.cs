#if NETSTANDARD2_0

// ReSharper disable once CheckNamespace
namespace System;

public static class ArraySegmentCompatExt
{
    public static ArraySegment<T> Slice<T>(this ArraySegment<T> arraySegment, int start)
    {
        if (start > arraySegment.Count)
            throw new ArgumentOutOfRangeException(nameof(start));

        return new ArraySegment<T>(arraySegment.Array!, arraySegment.Offset + start, arraySegment.Count - start);
    }

    public static ArraySegment<T> Slice<T>(this ArraySegment<T> arraySegment, int start, int length)
    {
        if (start + length > arraySegment.Count)
            throw new ArgumentOutOfRangeException(nameof(start));

        return new ArraySegment<T>(arraySegment.Array!, arraySegment.Offset + start, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo<T>(this ArraySegment<T> arraySegment, T[] dest)
        => CopyTo(arraySegment, dest, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo<T>(this ArraySegment<T> arraySegment, T[] dest, int arrayIndex)
        => Array.Copy(arraySegment.Array!, arraySegment.Offset, dest, arrayIndex, arraySegment.Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToString(this ArraySegment<char> chars)
        => new(chars.Array!, chars.Offset, chars.Count);
}

#endif
