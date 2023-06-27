using Stl.Interception;

namespace Stl.Rpc;

public abstract class RpcArgumentSerializer
{
    public static RpcArgumentSerializer Default { get; set; } = new RpcByteArgumentSerializer(ByteSerializer.Default);

    public abstract TextOrBytes Serialize(ArgumentList arguments, bool allowPolymorphism);
    public abstract void Deserialize(ref ArgumentList arguments, bool allowPolymorphism, TextOrBytes data);
}
