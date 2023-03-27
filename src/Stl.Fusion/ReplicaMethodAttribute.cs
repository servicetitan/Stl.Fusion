namespace Stl.Fusion;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class ReplicaMethodAttribute : ComputeMethodAttribute
{
    /// <summary>
    /// <see cref="Bridge.ReplicaCache"/> behavior.
    /// <code>null</code> means "use default".
    /// </summary>
    public ReplicaCacheBehavior CacheBehavior { get; set; }
}
