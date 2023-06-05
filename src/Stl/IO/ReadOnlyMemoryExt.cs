using System.Buffers;
using Stl.IO.Internal;

namespace Stl.IO;

public static class ReadOnlyMemoryExt
{
    public static ReadOnlySequence<T> ToSequence<T>(this ReadOnlyMemory<T> buffer)
    {
        var segment = new ReadOnlyMemorySegment<T>(buffer);
        return new ReadOnlySequence<T>(segment, 0, segment, buffer.Length);
    }
}
