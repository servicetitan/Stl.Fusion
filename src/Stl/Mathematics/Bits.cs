#if !NETSTANDARD
using System.Runtime.Intrinsics.X86;
#endif

namespace Stl.Mathematics;

public static class Bits
{
    private const ulong DeBruijnMultiplier = 0x07EDD5E59A4E28C2;
    private static readonly byte[] MultiplyDeBruijnBitPosition2 = new byte[64] {
        63,  0, 58,  1, 59, 47, 53,  2,
        60, 39, 48, 27, 54, 33, 42,  3,
        61, 51, 37, 40, 49, 18, 28, 20,
        55, 30, 34, 11, 43, 14, 22,  4,
        62, 57, 46, 52, 38, 26, 32, 41,
        50, 36, 17, 19, 29, 10, 13, 21,
        56, 45, 25, 31, 35, 16,  9, 12,
        44, 24, 15,  8, 23,  7,  6,  5
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPowerOf2(ulong n) => 0 == (n & (n - 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Lsb(ulong n) => n & (~n + 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Msb(ulong n)
    {
#if !NETSTANDARD
        const ulong highBit = 1UL << 63;
        if (Lzcnt.X64.IsSupported)
            return highBit >> (int) Lzcnt.X64.LeadingZeroCount(n);
#endif
        n |= n >> 1;
        n |= n >> 2;
        n |= n >> 4;
        n |= n >> 8;
        n |= n >> 16;
        n |= n >> 32;
        return n ^ (n >> 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GreaterOrEqualPowerOf2(ulong n)
    {
        var msb = Msb(n);
        return msb == n ? n : msb << 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MsbIndex(ulong n)
    {
#if !NETSTANDARD
        if (Lzcnt.X64.IsSupported) {
            var r = (int) Lzcnt.X64.LeadingZeroCount(n);
            return r == 64 ? r : 63 - r;
        }
#endif
        return Index(Msb(n));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LsbIndex(ulong n)
    {
#if !NETSTANDARD
        if (Bmi1.X64.IsSupported)
            return (int) Bmi1.X64.TrailingZeroCount(n);
#endif
        return Index(Lsb(n));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Count(ulong n)
    {
        var count = 0;
        while (n != 0) {
            count++;
            n &= (n - 1);
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int Index(ulong n)
    {
#if !NETSTANDARD
        if (Bmi1.X64.IsSupported)
            return (int) Bmi1.X64.TrailingZeroCount(n);
#endif
        if (n == 0)
            return 64;
        unchecked {
            fixed (byte* lut = MultiplyDeBruijnBitPosition2) {
                return lut[(DeBruijnMultiplier * n) >> 58];
            }
        }
    }
}

