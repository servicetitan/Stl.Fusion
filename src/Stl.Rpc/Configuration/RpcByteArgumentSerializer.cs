using Microsoft.Toolkit.HighPerformance.Buffers;
using Stl.Interception;

namespace Stl.Rpc;

public sealed class RpcByteArgumentSerializer : RpcArgumentSerializer<byte[]?>
{
    private readonly IByteSerializer _serializer;

    public RpcByteArgumentSerializer(IByteSerializer serializer)
        => _serializer = serializer;

    protected override byte[]? Serialize(ArgumentList arguments, Type argumentListType)
    {
        if (arguments.Length == 0)
            return null;

        using var bufferWriter = new ArrayPoolBufferWriter<byte>(256); // We intentionally do not dispose it here
        _serializer.Write(bufferWriter, arguments, argumentListType);
        return bufferWriter.WrittenSpan.ToArray();
    }

    protected override ArgumentList Deserialize(byte[]? data, Type argumentListType)
        => data is null
            ? ArgumentList.Empty
            : (ArgumentList)_serializer.Read(data, argumentListType)!;
}
