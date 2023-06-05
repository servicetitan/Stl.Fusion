using Stl.Interception;

namespace Stl.Rpc;

public sealed class RpcTextArgumentSerializer : RpcArgumentSerializer
{
    private readonly ITextSerializer _serializer;

    public RpcTextArgumentSerializer(ITextSerializer serializer)
        => _serializer = serializer;

    public override TextOrBytes Serialize(ArgumentList arguments)
    {
        if (arguments.Length == 0)
            return TextOrBytes.EmptyBytes;

        var text = _serializer.Write(arguments, arguments.GetType());
        return new TextOrBytes(text);
    }

    public override ArgumentList Deserialize(TextOrBytes argumentData, Type argumentListType)
    {
        if (!argumentData.IsText(out var text))
            throw new ArgumentOutOfRangeException(nameof(argumentData));

        return text.IsEmpty
            ? ArgumentList.Empty
            : (ArgumentList)_serializer.Read(text, argumentListType)!;
    }
}
