using System.Buffers;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Stl.IO;

namespace Stl.Serialization.Internal;

public class FuncByteSerializer<T> : IByteSerializer<T>
{
    public Func<ReadOnlyMemory<byte>, (T Value, int ReadLength)> Reader { get; }
    public Action<IBufferWriter<byte>, T> Writer { get; }

    public FuncByteSerializer(
        Func<ReadOnlyMemory<byte>, (T Value, int ReadLength)> reader,
        Action<IBufferWriter<byte>, T> writer)
    {
        Reader = reader;
        Writer = writer;
    }

    public T Read(ReadOnlySequence<byte> data, out long readLength)
    {
        using var writer = new ArrayPoolBufferWriter<byte>();
        writer.Write(data);
        var result = Read(writer.WrittenMemory, out var intReadLength);
        readLength = intReadLength;
        return result;
    }

    public T Read(ReadOnlyMemory<byte> data, out int readLength)
    {
        var result = Reader.Invoke(data);
        readLength = result.ReadLength;
        return result.Value;
    }

    public void Write(IBufferWriter<byte> bufferWriter, T value)
        => Writer.Invoke(bufferWriter, value);
}
