using Stl.Serialization.Internal;

namespace Stl.Serialization;

public static class ByteSerializer
{
    public static IByteSerializer Default { get; set; } = MessagePackByteSerializer.Default;
    public static IByteSerializer None { get; } = NoneByteSerializer.Instance;
    public static IByteSerializer Null { get; } = NullByteSerializer.Instance;

    public static IByteSerializer NewAsymmetric(IByteSerializer reader, IByteSerializer writer)
        => new AsymmetricByteSerializer(reader, writer);
}

public static class ByteSerializer<T>
{
    public static IByteSerializer<T> Default { get; } = ByteSerializer.Default.ToTyped<T>();
    public static IByteSerializer<T> None { get; } = NoneByteSerializer<T>.Instance;
    public static IByteSerializer<T> Null { get; } = NullByteSerializer<T>.Instance;

    public static IByteSerializer<T> NewAsymmetric(IByteSerializer<T> reader, IByteSerializer<T> writer)
        => new AsymmetricByteSerializer<T>(reader, writer);
}
