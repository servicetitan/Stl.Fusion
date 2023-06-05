using Microsoft.Toolkit.HighPerformance.Buffers;
using Stl.Interception;

namespace Stl.Rpc;

public sealed class RpcByteArgumentSerializer : RpcArgumentSerializer
{
    private readonly IByteSerializer _serializer;

    public RpcByteArgumentSerializer(IByteSerializer serializer)
        => _serializer = serializer;

    public override TextOrBytes Serialize(ArgumentList arguments)
    {
        if (arguments.Length == 0)
            return TextOrBytes.EmptyBytes;

        using var bufferWriter = new ArrayPoolBufferWriter<byte>(256); // We intentionally do not dispose it here
        _serializer.Write(bufferWriter, arguments, arguments.GetType());
        return new TextOrBytes(bufferWriter.WrittenSpan.ToArray());
    }

    public override ArgumentList Deserialize(TextOrBytes argumentData, Type argumentListType)
    {
        if (argumentData.Format != DataFormat.Bytes)
            throw new ArgumentOutOfRangeException(nameof(argumentData));

        var data = argumentData.Data;
        return data.IsEmpty
            ? ArgumentList.Empty
            : (ArgumentList)_serializer.Read(data, argumentListType)!;
    }
}
