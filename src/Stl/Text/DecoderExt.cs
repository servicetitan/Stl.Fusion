using System.Buffers;
using System.Text;
using Cysharp.Text;

namespace Stl.Text;

#if NETSTANDARD2_0
public static unsafe class DecoderExt
#else
public static class DecoderExt
#endif
{
    public static string Convert(this Decoder decoder, ReadOnlySpan<byte> source)
    {
        var sb = ZString.CreateStringBuilder();
        try {
            decoder.Convert(source, ref sb);
            return sb.ToString();
        }
        finally {
            sb.Dispose();
        }
    }

    public static void Convert(this Decoder decoder, ReadOnlySpan<byte> source, IBufferWriter<char> target, bool flush = true)
    {
        while (true) {
            int charsUsed, bytesUsed;
            bool completed;
            var freeSpan = target.GetSpan(source.Length);
#if NETSTANDARD2_0
            fixed (byte* sourcePtr = &source.GetPinnableReference())
            fixed (char* freeSpanPtr = &freeSpan.GetPinnableReference())
                decoder.Convert(sourcePtr, source.Length, freeSpanPtr, freeSpan.Length,
                    flush, out bytesUsed, out charsUsed, out completed);
#else
            decoder.Convert(source, freeSpan, flush, out bytesUsed, out charsUsed, out completed);
#endif
            target.Advance(charsUsed);
            if (completed)
                return;

            source = source[bytesUsed..];
        }
    }

    public static void Convert(this Decoder decoder, ReadOnlySpan<byte> source, ref Utf16ValueStringBuilder target, bool flush = true)
    {
        while (true) {
            int charsUsed, bytesUsed;
            bool completed;
            var freeSpan = target.GetSpan(source.Length);
#if NETSTANDARD2_0
            fixed (byte* sourcePtr = &source.GetPinnableReference())
            fixed (char* freeSpanPtr = &freeSpan.GetPinnableReference())
                decoder.Convert(sourcePtr, source.Length, freeSpanPtr, freeSpan.Length,
                    flush, out bytesUsed, out charsUsed, out completed);
#else
            decoder.Convert(source, freeSpan, flush, out bytesUsed, out charsUsed, out completed);
#endif
            target.Advance(charsUsed);
            if (completed)
                return;

            source = source[bytesUsed..];
        }
    }
}
