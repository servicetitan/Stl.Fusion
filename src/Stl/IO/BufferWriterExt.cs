using System.Buffers;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Stl.IO.Internal;

namespace Stl.IO;

public static class BufferWriterExt
{
    public static void Write<T>(this IBufferWriter<T> writer, ReadOnlySequence<T> sequence)
    {
        foreach (var segment in sequence)
            writer.Write(segment.Span);
    }

    public static void Reset<T>(this ArrayPoolBufferWriter<T> writer, int writtenCount = 0)
        => ArrayPoolBufferWriterHelper<T>.IndexSetter.Invoke(writer, writtenCount);
}
