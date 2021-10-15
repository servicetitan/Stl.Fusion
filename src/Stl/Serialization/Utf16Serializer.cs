namespace Stl.Serialization;

public class Utf16Serializer : IUtf16Serializer
{
    public static IUtf16Serializer Default { get; set; } = SystemJsonSerializer.Default;

    public IUtf16Reader Reader { get; }
    public IUtf16Writer Writer { get; }

    public Utf16Serializer(IUtf16Reader reader, IUtf16Writer writer)
    {
        Reader = reader;
        Writer = writer;
    }

    public virtual IUtf16Serializer<T> ToTyped<T>(Type? serializedType = null)
        => new Utf16Serializer<T>(
            Reader.ToTyped<T>(serializedType),
            Writer.ToTyped<T>(serializedType));
}

public class Utf16Serializer<T> : IUtf16Serializer<T>
{
    public static IUtf16Serializer<T> Default => Utf16Serializer.Default.ToTyped<T>();

    public IUtf16Reader<T> Reader { get; }
    public IUtf16Writer<T> Writer { get; }

    public Utf16Serializer(IUtf16Reader<T> reader, IUtf16Writer<T> writer)
    {
        Reader = reader;
        Writer = writer;
    }
}
