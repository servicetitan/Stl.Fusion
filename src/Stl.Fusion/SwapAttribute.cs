namespace Stl.Fusion;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class SwapAttribute : Attribute
{
    public bool IsEnabled { get; } = true;
    public Type? SwapServiceType { get; set; }

    /// <summary>
    /// Swap delay in seconds. When swapped, the <see cref="IComputed.Output"/>
    /// of a produced <see cref="IComputed"/> gets written by a swap service
    /// to some external storage and removed from RAM;
    /// any subsequent attempt to access the output would fetch it back,
    /// and swap delay will start ticking again.
    /// <code>double.NaN</code> means "use default".
    /// </summary>
    public double SwapDelay { get; set; } = double.NaN;

    public SwapAttribute() { }
    public SwapAttribute(bool isEnabled)
        => IsEnabled = isEnabled;
    public SwapAttribute(double swapTime)
        : this(null!, swapTime) { }
    public SwapAttribute(Type swapServiceType, double swapTime = double.NaN)
    {
        SwapServiceType = swapServiceType;
        SwapDelay = swapTime;
    }
}
