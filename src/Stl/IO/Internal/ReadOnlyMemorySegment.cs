using System.Buffers;

namespace Stl.IO.Internal;

public class ReadOnlyMemorySegment<T> : ReadOnlySequenceSegment<T>
{
    public ReadOnlyMemorySegment(ReadOnlyMemory<T> memory)
        => Memory = memory;

    public ReadOnlyMemorySegment<T> Append(ReadOnlyMemory<T> memory)
    {
        var segment = new ReadOnlyMemorySegment<T>(memory) {
            RunningIndex = RunningIndex + Memory.Length
        };
        Next = segment;
        return segment;
    }
}
