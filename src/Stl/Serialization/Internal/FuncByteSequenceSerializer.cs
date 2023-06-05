using System.Buffers;
using Stl.IO;

namespace Stl.Serialization.Internal;

public class FuncByteSequenceSerializer<T> : IByteSerializer<T>
{
    public Func<ReadOnlySequence<byte>, (T Value, long ReadLength)> Reader { get; }
    public Action<IBufferWriter<byte>, T> Writer { get; }

    public FuncByteSequenceSerializer(
        Func<ReadOnlySequence<byte>, (T Value, long ReadLength)> reader,
        Action<IBufferWriter<byte>, T> writer)
    {
        Reader = reader;
        Writer = writer;
    }

    public T Read(ReadOnlySequence<byte> data, out long readLength)
    {
        var result = Reader.Invoke(data);
        readLength = result.ReadLength;
        return result.Value;
    }

    public T Read(ReadOnlyMemory<byte> data, out int readLength)
    {
        var result = Read(data.ToSequence(), out var longReadLength);
        readLength = (int)longReadLength;
        return result;
    }

    public void Write(IBufferWriter<byte> bufferWriter, T value)
        => Writer.Invoke(bufferWriter, value);
}
