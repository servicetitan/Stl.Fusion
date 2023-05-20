using Microsoft.Toolkit.HighPerformance.Buffers;
using Stl.Interception;

namespace Stl.Rpc;

public record RpcConfiguration
{
    public Dictionary<Type, RpcServiceConfiguration> Services { get; init; } = new();

    public Func<Type, Symbol> ServiceNameBuilder { get; init; } = DefaultServiceNameBuilder;
    public Func<RpcMethodDef, Symbol> MethodNameBuilder { get; init; } = DefaultMethodNameBuilder;
    public RpcArgumentSerializer ArgumentSerializer { get; init; } = RpcArgumentSerializer.Default;

    public static Symbol DefaultServiceNameBuilder(Type serviceType)
        => serviceType.GetName();

    public static Symbol DefaultMethodNameBuilder(RpcMethodDef methodDef)
        => $"{methodDef.Method.Name}:{methodDef.RemoteParameterTypes.Length}";
}
