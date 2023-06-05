using System.Buffers;

namespace Stl.Serialization.Internal;

public class CastingByteSerializer<T> : IByteSerializer<T>
{
    public IByteSerializer UntypedSerializer { get; }
    public Type SerializedType { get; }

    public CastingByteSerializer(IByteSerializer untypedSerializer, Type serializedType)
    {
        UntypedSerializer = untypedSerializer;
        SerializedType = serializedType;
    }

    public T Read(ReadOnlyMemory<byte> data, out int readLength)
        => (T) UntypedSerializer.Read(data, SerializedType, out readLength)!;

    public T Read(ReadOnlySequence<byte> data, out long readLength)
        => (T) UntypedSerializer.Read(data, SerializedType, out readLength)!;

    public void Write(IBufferWriter<byte> bufferWriter, T value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => UntypedSerializer.Write(bufferWriter, value, SerializedType);
}
