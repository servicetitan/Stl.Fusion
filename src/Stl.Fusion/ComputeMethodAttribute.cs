using Stl.Fusion.Internal;

namespace Stl.Fusion;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class ComputeMethodAttribute : Attribute
{
    public Type? ComputeMethodDefType { get; set; } = null;

    /// <summary>
    /// Minimum time (in seconds) for any produced <see cref="IComputed"/> instance to stay in RAM.
    /// <code>double.NaN</code> means "use default".
    /// </summary>
    /// <remarks>
    /// <para>
    /// In fact, it's a duration of a period during which a strong reference to this
    /// <see cref="IComputed"/> instance is maintained via <see cref="Timeouts"/>.
    /// </para>
    /// <para>
    /// Invalidation trims this time to the invalidation point - there is no reason
    /// to cache an outdated computed; moreover, there is no way to get it
    /// (unless you already have a strong reference to it).
    /// </para>
    /// </remarks>
    public double MinCacheDuration { get; set; } = double.NaN;

    /// <summary>
    /// Auto-invalidation delay (in seconds) for any produced <see cref="IComputed"/> instance
    /// which stores an error, and it's a transient error.
    /// <code>double.NaN</code> means "use default".
    /// </summary>
    public double TransientErrorInvalidationDelay { get; set; } = double.NaN;

    /// <summary>
    /// Auto-invalidation delay (in seconds) for any produced <see cref="IComputed"/> instance.
    /// <code>double.NaN</code> means "use default".
    /// </summary>
    public double AutoInvalidationDelay { get; set; } = double.NaN;
}
