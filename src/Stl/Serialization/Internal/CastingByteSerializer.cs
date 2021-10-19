using System.Buffers;

namespace Stl.Serialization.Internal;

public class CastingByteSerializer<T> : IByteSerializer<T>, IByteReader<T>, IByteWriter<T>
{
    public IByteSerializer Serializer { get; }
    public Type SerializedType { get; }
    public IByteReader<T> Reader => this;
    public IByteWriter<T> Writer => this;

    public CastingByteSerializer(IByteSerializer serializer, Type serializedType)
    {
        Serializer = serializer;
        SerializedType = serializedType;
    }

    public T Read(ReadOnlyMemory<byte> data)
        => (T) Serializer.Reader.Read(data, SerializedType)!;

    public void Write(IBufferWriter<byte> bufferWriter, T? value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => Serializer.Writer.Write(bufferWriter, value, SerializedType);
}
