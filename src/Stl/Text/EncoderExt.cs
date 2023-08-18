using System.Buffers;
using System.Text;
using Cysharp.Text;

namespace Stl.Text;

#if NETSTANDARD2_0
public static unsafe class EncoderExt
#else
public static class EncoderExt
#endif
{
    public static byte[] Convert(this Encoder encoder, ReadOnlySpan<char> source)
    {
        var sb = ZString.CreateUtf8StringBuilder();
        try {
            encoder.Convert(source, ref sb);
            return sb.AsSpan().ToArray();
        }
        finally {
            sb.Dispose();
        }
    }

    public static void Convert(this Encoder encoder, ReadOnlySpan<char> source, IBufferWriter<byte> target, bool flush = true)
    {
        while (true) {
            int charsUsed, bytesUsed;
            bool completed;
            var freeSpan = target.GetSpan(source.Length);
#if NETSTANDARD2_0
            fixed (char* sourcePtr = &source.GetPinnableReference())
            fixed (byte* freeSpanPtr = &freeSpan.GetPinnableReference())
                encoder.Convert(sourcePtr, source.Length, freeSpanPtr, freeSpan.Length,
                    flush, out charsUsed, out bytesUsed, out completed);
#else
            encoder.Convert(source, freeSpan, flush, out charsUsed, out bytesUsed, out completed);
#endif
            target.Advance(bytesUsed);
            if (completed)
                return;

            source = source[charsUsed..];
        }
    }

    public static void Convert(this Encoder encoder, ReadOnlySpan<char> source, ref Utf8ValueStringBuilder target, bool flush = true)
    {
        while (true) {
            int charsUsed, bytesUsed;
            bool completed;
            var freeSpan = target.GetSpan(source.Length);
#if NETSTANDARD2_0
            fixed (char* sourcePtr = &source.GetPinnableReference())
            fixed (byte* freeSpanPtr = &freeSpan.GetPinnableReference())
                encoder.Convert(sourcePtr, source.Length, freeSpanPtr, freeSpan.Length,
                    flush, out charsUsed, out bytesUsed, out completed);
#else
            encoder.Convert(source, freeSpan, flush, out charsUsed, out bytesUsed, out completed);
#endif
            target.Advance(bytesUsed);
            if (completed)
                return;

            source = source[charsUsed..];
        }
    }
}
