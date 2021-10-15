namespace Stl.Serialization;

public class ByteSerializer : IByteSerializer
{
    public static IByteSerializer Default { get; set; } = MessagePackByteSerializer.Default;

    public IByteReader Reader { get; }
    public IByteWriter Writer { get; }

    public ByteSerializer(IByteReader reader, IByteWriter writer)
    {
        Reader = reader;
        Writer = writer;
    }

    public virtual IByteSerializer<T> ToTyped<T>(Type? serializedType = null)
        => new ByteSerializer<T>(
            Reader.ToTyped<T>(serializedType),
            Writer.ToTyped<T>(serializedType));
}

public class ByteSerializer<T> : IByteSerializer<T>
{
    public static IByteSerializer<T> Default => ByteSerializer.Default.ToTyped<T>();

    public IByteReader<T> Reader { get; }
    public IByteWriter<T> Writer { get; }

    public ByteSerializer(IByteReader<T> reader, IByteWriter<T> writer)
    {
        Reader = reader;
        Writer = writer;
    }
}
