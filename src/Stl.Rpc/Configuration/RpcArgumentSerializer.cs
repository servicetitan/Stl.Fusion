using System.Diagnostics.CodeAnalysis;
using Stl.Interception;
using Stl.Internal;

namespace Stl.Rpc;

public abstract class RpcArgumentSerializer
{
    public static RpcArgumentSerializer Default { get; set; } = new RpcByteArgumentSerializer(ByteSerializer.Default);

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public abstract TextOrBytes Serialize(ArgumentList arguments, bool allowPolymorphism);
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public abstract void Deserialize(ref ArgumentList arguments, bool allowPolymorphism, TextOrBytes data);
}
