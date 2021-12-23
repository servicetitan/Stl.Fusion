using System.Buffers;

namespace Stl.Serialization;

public interface IByteSerializer
{
    object? Read(ReadOnlyMemory<byte> data, Type type);
    void Write(IBufferWriter<byte> bufferWriter, object? value, Type type);
    IByteSerializer<T> ToTyped<T>(Type? serializedType = null);
}

public interface IByteSerializer<T>
{
    T Read(ReadOnlyMemory<byte> data);
    void Write(IBufferWriter<byte> bufferWriter, T value);
}
