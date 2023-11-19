namespace Stl.Fusion;

#pragma warning disable CA1813 // Consider making sealed

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class ClientComputeMethodAttribute : ComputeMethodAttribute
{
    /// <summary>
    /// <see cref="RpcCom"/> behavior.
    /// <code>null</code> means "use default".
    /// </summary>
    public ClientCacheMode ClientCacheMode { get; set; }
}
