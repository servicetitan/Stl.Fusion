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
    public T Read(ReadOnlyMemory<char> data)
        => (T) UntypedSerializer.Read(data, SerializedType)!;

    public string Write(T value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => UntypedSerializer.Write(value, SerializedType);
    public void Write(IBufferWriter<char> bufferWriter, T value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => UntypedSerializer.Write(bufferWriter, value, SerializedType);
}
