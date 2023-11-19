#if !NETSTANDARD
using System.Runtime.Intrinsics.X86;
#endif

namespace Stl.Mathematics;

public static class Bits
{
#if !NET7_0_OR_GREATER
    private static readonly byte[] DeBruijnTrailingZeroCount = {
        0, 1, 2, 53, 3, 7, 54, 27, 4, 38, 41, 8, 34, 55, 48, 28,
        62, 5, 39, 46, 44, 42, 22, 9, 24, 35, 59, 56, 49, 18, 29, 11,
        63, 52, 6, 26, 37, 40, 33, 47, 61, 45, 43, 21, 23, 58, 17, 10,
        51, 25, 36, 32, 60, 20, 57, 16, 50, 31, 19, 15, 30, 14, 13, 12
    };
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPowerOf2(ulong n)
        => (n & (n - 1)) == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GreaterOrEqualPowerOf2(ulong n)
    {
        var msb = LeadingBitMask(n);
        return msb == n ? n : msb << 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong TrailingBitMask(ulong n)
        => n & (~n + 1);

#if NET7_0_OR_GREATER
    // .NET 7+ - the methods here use AggressiveInlining option

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(ulong n)
        => (int)ulong.PopCount(n);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong LeadingBitMask(ulong n)
    {
        const ulong highBit = 1UL << 63;
        var leadingZeroCount = (int)ulong.LeadingZeroCount(n);
        // a >> b works as a >> (b & 63)
        return leadingZeroCount > 63 ? 0 : highBit >> leadingZeroCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LeadingBitIndex(ulong n)
    {
        var r = (int)ulong.LeadingZeroCount(n);
        return r == 64 ? r : 63 - r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LeadingZeroCount(ulong n)
        => (int)ulong.LeadingZeroCount(n);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TrailingZeroCount(ulong n)
        => (int)ulong.TrailingZeroCount(n);

#else
    // .NET 6 and below - the methods here don't use AggressiveInlining option

    public static int PopCount(ulong n)
    {
        var count = 0;
        while (n != 0) {
            count++;
            n &= (n - 1);
        }
        return count;
    }

    public static ulong LeadingBitMask(ulong n)
    {
#if !NETSTANDARD
        const ulong highBit = 1UL << 63;
        if (Lzcnt.X64.IsSupported) {
            var leadingZeroCount = (int)Lzcnt.X64.LeadingZeroCount(n);
            // a >> b works as a >> (b & 63)
            return leadingZeroCount > 63 ? 0 : highBit >> leadingZeroCount;
        }
#endif
        n |= n >> 1;
        n |= n >> 2;
        n |= n >> 4;
        n |= n >> 8;
        n |= n >> 16;
        n |= n >> 32;
        return n ^ (n >> 1);
    }

    public static int LeadingBitIndex(ulong n)
    {
#if !NETSTANDARD
        if (Lzcnt.X64.IsSupported) {
            var leadingZeroCount = (int)Lzcnt.X64.LeadingZeroCount(n);
            return leadingZeroCount == 64 ? 64 : 63 - leadingZeroCount;
        }
#endif
        return TrailingZeroCount(LeadingBitMask(n));
    }

    public static int LeadingZeroCount(ulong n)
    {
#if !NETSTANDARD
        if (Lzcnt.X64.IsSupported)
            return (int)Lzcnt.X64.LeadingZeroCount(n);
#endif
        var r = TrailingZeroCount(LeadingBitMask(n));
        return r == 64 ? 64 : 63 - r;
    }

    public static int TrailingZeroCount(ulong n)
    {
#if !NETSTANDARD
        if (Bmi1.X64.IsSupported)
            return (int)Bmi1.X64.TrailingZeroCount(n);
#endif
        if (n == 0)
            return 64;
        unsafe {
            unchecked {
                var x = (long)n;
                fixed (byte* lut = DeBruijnTrailingZeroCount)
                    return lut[(0x022FDD63CC95386Dul * (ulong)(x & -x)) >> 58];
            }
        }
    }

#endif
}
