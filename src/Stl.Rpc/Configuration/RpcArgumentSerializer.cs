using Stl.Interception;

namespace Stl.Rpc;

public abstract class RpcArgumentSerializer
{
    public abstract TextOrBytes Serialize(ArgumentList arguments);
    public abstract ArgumentList Deserialize(TextOrBytes argumentData, Type argumentListType);
}
