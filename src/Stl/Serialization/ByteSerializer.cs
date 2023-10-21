using System.Buffers;
using Stl.Serialization.Internal;

namespace Stl.Serialization;

public static class ByteSerializer
{
    public static IByteSerializer Default { get; set; } =
#if !NETSTANDARD2_0
        MemoryPackByteSerializer.Default;
#else
        MessagePackByteSerializer.Default;
#endif
    public static readonly IByteSerializer None = NoneByteSerializer.Instance;
    public static readonly IByteSerializer Null = NullByteSerializer.Instance;

    public static IByteSerializer NewAsymmetric(IByteSerializer reader, IByteSerializer writer)
        => new AsymmetricByteSerializer(reader, writer);
}

public static class ByteSerializer<T>
{
    public static readonly IByteSerializer<T> Default = ByteSerializer.Default.ToTyped<T>();
    public static readonly IByteSerializer<T> None = NoneByteSerializer<T>.Instance;
    public static readonly IByteSerializer<T> Null = NullByteSerializer<T>.Instance;

    public static IByteSerializer<T> New(
        Func<ReadOnlyMemory<byte>, (T Value, int ReadLength)> reader,
        Action<IBufferWriter<byte>, T> writer)
        => new FuncByteSerializer<T>(reader, writer);

    public static IByteSerializer<T> NewAsymmetric(IByteSerializer<T> reader, IByteSerializer<T> writer)
        => new AsymmetricByteSerializer<T>(reader, writer);
}
