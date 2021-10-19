using System.Buffers;

namespace Stl.Serialization.Internal;

public abstract class ByteSerializerBase : IByteSerializer, IByteReader, IByteWriter
{
    public IByteReader Reader => this;
    public IByteWriter Writer => this;

    IByteReader<T> IByteReader.ToTyped<T>(Type? serializedType)
        => new CastingByteSerializer<T>(this, serializedType ?? typeof(T));
    IByteWriter<T> IByteWriter.ToTyped<T>(Type? serializedType)
        => new CastingByteSerializer<T>(this, serializedType ?? typeof(T));
    public virtual IByteSerializer<T> ToTyped<T>(Type? serializedType = null)
        => new CastingByteSerializer<T>(this, serializedType ?? typeof(T));

    public abstract object? Read(ReadOnlyMemory<byte> data, Type type);
    public abstract void Write(IBufferWriter<byte> bufferWriter, object? value, Type type);
}
