using Microsoft.Toolkit.HighPerformance.Buffers;
using Stl.Interception;

namespace Stl.Rpc;

public class RpcByteArgumentSerializer : RpcArgumentSerializer<byte[]?>
{
    public static RpcByteArgumentSerializer Default { get; set; } = new(ByteSerializer.Default);

    protected IByteSerializer Serializer { get; }

    public RpcByteArgumentSerializer(IByteSerializer serializer)
        => Serializer = serializer;

    public override byte[]? Serialize(ArgumentList arguments, Type argumentListType)
    {
        if (arguments.Length == 0)
            return null;

        using var bufferWriter = new ArrayPoolBufferWriter<byte>(256); // We intentionally do not dispose it here
        Serializer.Write(bufferWriter, arguments, argumentListType);
        return bufferWriter.WrittenSpan.ToArray();
    }

    public override ArgumentList Deserialize(byte[]? data, Type argumentListType)
        => data is null
            ? ArgumentList.Empty
            : (ArgumentList)Serializer.Read(data, argumentListType)!;
}
