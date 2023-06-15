namespace Stl.Fusion;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class ClientComputeMethodAttribute : ComputeMethodAttribute
{
    /// <summary>
    /// <see cref="RpcCom"/> behavior.
    /// <code>null</code> means "use default".
    /// </summary>
    public ClientCacheBehavior ClientCacheBehavior { get; set; }
}
