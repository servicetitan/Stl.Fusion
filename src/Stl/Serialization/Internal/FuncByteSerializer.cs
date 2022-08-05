using System.Buffers;

namespace Stl.Serialization.Internal;

public class FuncByteSerializer<T> : IByteSerializer<T>
{
    public Func<byte[], T> Reader { get; }
    public Func<T, byte[]> Writer { get; }

    public FuncByteSerializer(Func<byte[], T> reader, Func<T, byte[]> writer)
    {
        Reader = reader;
        Writer = writer;
    }

    public T Read(ReadOnlyMemory<byte> data) 
        => Reader.Invoke(data.ToArray());

    public void Write(IBufferWriter<byte> bufferWriter, T value)
    {
        var result = Writer.Invoke(value);
        bufferWriter.Write(result);
    }
}
