namespace Stl.Mathematics;

public static class GuidExt
{
    public static (ulong, ulong) ToUInt64Pair(this Guid guid)
    {
        var bytes = guid.ToByteArray();
        var uint64Span = MemoryMarshal.Cast<byte, ulong>(bytes.AsSpan());
        return (uint64Span[0], uint64Span[1]);
    }

    public static string Format(this Guid guid, int radix)
    {
        var (p1, p2) = guid.ToUInt64Pair();
        var size = radix < 10 ? 130 : 42; // Just to simplify the calculation
        Span<char> buffer = stackalloc char[size];
        var span1 = MathExt.FormatTo(p1, radix, buffer);
        var span2 = MathExt.FormatTo(p2, radix, buffer[span1.Length..]);
        var fullSpan = buffer[..(span1.Length + span2.Length)];
#if !NETSTANDARD2_0
        return new string(fullSpan);
#else
        return fullSpan.ToString();
#endif
    }
}
