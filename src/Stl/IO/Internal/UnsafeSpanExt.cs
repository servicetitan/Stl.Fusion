namespace Stl.IO.Internal;

public static class UnsafeSpanExt
{
    // ReadUnchecked

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadUnchecked<T>(this Span<byte> span)
    {
        ref var byteRef = ref MemoryMarshal.GetReference(span);
        return Unsafe.ReadUnaligned<T>(ref byteRef);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadUnchecked<T>(this Span<byte> span, int byteOffset)
    {
        ref var byteRef = ref Unsafe.Add(ref MemoryMarshal.GetReference(span), byteOffset);
        return Unsafe.ReadUnaligned<T>(ref byteRef);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadUnchecked<T>(this ReadOnlySpan<byte> span)
    {
        ref var byteRef = ref MemoryMarshal.GetReference(span);
        return Unsafe.ReadUnaligned<T>(ref byteRef);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadUnchecked<T>(this ReadOnlySpan<byte> span, int byteOffset)
    {
        ref var byteRef = ref Unsafe.Add(ref MemoryMarshal.GetReference(span), byteOffset);
        return Unsafe.ReadUnaligned<T>(ref byteRef);
    }

    // WriteUnchecked

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUnchecked<T>(this Span<byte> span, T value)
    {
        ref var byteRef = ref MemoryMarshal.GetReference(span);
        Unsafe.WriteUnaligned(ref byteRef, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUnchecked<T>(this Span<byte> span, int byteOffset, T value)
    {
        ref var byteRef = ref Unsafe.Add(ref MemoryMarshal.GetReference(span), byteOffset);
        Unsafe.WriteUnaligned(ref byteRef, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUnchecked<T>(this ReadOnlySpan<byte> span, T value)
    {
        ref var byteRef = ref MemoryMarshal.GetReference(span);
        Unsafe.WriteUnaligned(ref byteRef, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUnchecked<T>(this ReadOnlySpan<byte> span, int byteOffset, T value)
    {
        ref var byteRef = ref Unsafe.Add(ref MemoryMarshal.GetReference(span), byteOffset);
        Unsafe.WriteUnaligned(ref byteRef, value);
    }
}
