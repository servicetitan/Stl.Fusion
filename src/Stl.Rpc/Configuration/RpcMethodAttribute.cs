namespace Stl.Rpc;

[AttributeUsage(AttributeTargets.Method)]
public class RpcMethodAttribute : Attribute
{
    public Type? MethodDefType { get; set; } = null;
}
