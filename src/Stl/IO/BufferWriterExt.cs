using System.Buffers;

namespace Stl.IO;

public static class BufferWriterExt
{
    public static void Write<T>(this IBufferWriter<T> writer, ReadOnlySequence<T> sequence)
    {
        foreach (var segment in sequence)
            writer.Write(segment.Span);
    }
}
