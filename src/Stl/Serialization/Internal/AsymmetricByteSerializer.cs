using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization.Internal;

public sealed class AsymmetricByteSerializer(IByteSerializer reader, IByteSerializer writer) : IByteSerializer
{
    public IByteSerializer Reader { get; } = reader;
    public IByteSerializer Writer { get; } = writer;

    public IByteSerializer<T> ToTyped<T>(Type? serializedType = null)
        => new AsymmetricByteSerializer<T>(
            Reader.ToTyped<T>(serializedType),
            Writer.ToTyped<T>(serializedType));

    // IByteReader, IByteWriter impl.

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
        => Reader.Read(data, type, out readLength);

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
        => Writer.Write(bufferWriter, value, type);
}

public sealed class AsymmetricByteSerializer<T>(IByteSerializer<T> reader, IByteSerializer<T> writer) : IByteSerializer<T>
{
    public IByteSerializer<T> Reader { get; } = reader;
    public IByteSerializer<T> Writer { get; } = writer;

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public T Read(ReadOnlyMemory<byte> data, out int readLength)
        => Reader.Read(data, out readLength);

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public void Write(IBufferWriter<byte> bufferWriter, T value)
        => Writer.Write(bufferWriter, value);
}
