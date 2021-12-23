namespace Stl.Serialization.Internal;

public class CastingTextSerializer<T> : ITextSerializer<T>, ITextReader<T>, ITextWriter<T>
{
    public ITextSerializer Serializer { get; }
    public Type SerializedType { get; }
    public ITextReader<T> Reader => this;
    public ITextWriter<T> Writer => this;

    public CastingTextSerializer(ITextSerializer serializer, Type serializedType)
    {
        Serializer = serializer;
        SerializedType = serializedType;
    }

    public T Read(string data)
        => (T) Serializer.Reader.Read(data, SerializedType)!;

    public string Write(T value)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => Serializer.Writer.Write(value, SerializedType);
}
