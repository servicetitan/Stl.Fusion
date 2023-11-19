using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization.Internal;

public class FuncByteSerializer<T>(Func<ReadOnlyMemory<byte>, (T Value, int ReadLength)> reader,
        Action<IBufferWriter<byte>, T> writer)
    : IByteSerializer<T>
{
    public Func<ReadOnlyMemory<byte>, (T Value, int ReadLength)> Reader { get; } = reader;
    public Action<IBufferWriter<byte>, T> Writer { get; } = writer;

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(ReadOnlyMemory<byte> data, out int readLength)
    {
        var result = Reader.Invoke(data);
        readLength = result.ReadLength;
        return result.Value;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(IBufferWriter<byte> bufferWriter, T value)
        => Writer.Invoke(bufferWriter, value);
}
