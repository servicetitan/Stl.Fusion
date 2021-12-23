using System.Buffers;
using System.Text;
using Cysharp.Text;

namespace Stl.Text;

#if NETSTANDARD2_0
public static unsafe class EncoderDecoderExt
#else
public static class EncoderDecoderExt
#endif
{
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
                    true, out charsUsed, out bytesUsed, out completed);
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
                    true, out charsUsed, out bytesUsed, out completed);
#else
            encoder.Convert(source, freeSpan, flush, out charsUsed, out bytesUsed, out completed);
#endif
            target.Advance(bytesUsed);
            if (completed)
                return;
            source = source[charsUsed..];
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
                    true, out bytesUsed, out charsUsed, out completed);
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
                    true, out bytesUsed, out charsUsed, out completed);
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
