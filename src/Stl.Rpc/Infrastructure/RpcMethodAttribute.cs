namespace Stl.Rpc.Infrastructure;

[AttributeUsage(AttributeTargets.Method)]
public class RpcMethodAttribute : Attribute
{
    public Type? MethodDefType { get; set; } = null;
}
