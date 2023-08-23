using System.Buffers;

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

    public object? Read(ReadOnlyMemory<byte> data, Type type, out int readLength)
        => Reader.Read(data, type, out readLength);

    public void Write(IBufferWriter<byte> bufferWriter, object? value, Type type)
        => Writer.Write(bufferWriter, value, type);
}

public sealed class AsymmetricByteSerializer<T> : IByteSerializer<T>
{
    public IByteSerializer<T> Reader { get; }
    public IByteSerializer<T> Writer { get; }

    public AsymmetricByteSerializer(IByteSerializer<T> reader, IByteSerializer<T> writer)
    {
        Reader = reader;
        Writer = writer;
    }

    public T Read(ReadOnlyMemory<byte> data, out int readLength)
        => Reader.Read(data, out readLength);

    public void Write(IBufferWriter<byte> bufferWriter, T value)
        => Writer.Write(bufferWriter, value);
}
