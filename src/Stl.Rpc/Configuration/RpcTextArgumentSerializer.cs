using Stl.Interception;

namespace Stl.Rpc;

public sealed class RpcTextArgumentSerializer : RpcArgumentSerializer<string?>
{
    private readonly ITextSerializer _serializer;

    public RpcTextArgumentSerializer(ITextSerializer serializer)
        => _serializer = serializer;

    protected override string? Serialize(ArgumentList arguments, Type argumentListType)
        => arguments.Length == 0
            ? null
            : _serializer.Write(arguments, argumentListType);

    protected override ArgumentList Deserialize(string? data, Type argumentListType)
        => data is null
            ? ArgumentList.Empty
            : (ArgumentList)_serializer.Read(data, argumentListType)!;
}
