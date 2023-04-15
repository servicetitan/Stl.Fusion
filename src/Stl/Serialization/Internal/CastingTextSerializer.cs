using System.Buffers;

namespace Stl.Serialization.Internal;

public class CastingTextSerializer<T> : ITextSerializer<T>
{
    public ITextSerializer UntypedSerializer { get; }
    public Type SerializedType { get; }
    public bool PreferStringApi => UntypedSerializer.PreferStringApi;

    public CastingTextSerializer(ITextSerializer untypedSerializer, Type serializedType)
    {
        UntypedSerializer = untypedSerializer;
        SerializedType = serializedType;
    }

    public T Read(string data)
        => (T) UntypedSerializer.Read(data, SerializedType)!;
    public T Read(ReadOnlyMemory<byte> data, out int readLength)
        => (T) UntypedSerializer.Read(data, SerializedType, out readLength)!;

    public string Write(T value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => UntypedSerializer.Write(value, SerializedType);
    public void Write(IBufferWriter<byte> bufferWriter, T value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => UntypedSerializer.Write(bufferWriter, value, SerializedType);
}
