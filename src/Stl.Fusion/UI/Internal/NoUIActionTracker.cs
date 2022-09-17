namespace Stl.Fusion.UI.Internal;

public class NoUIActionTracker : UIActionTracker
{
    public NoUIActionTracker(int runningActionCount)
        : base(new(), DependencyInjection.ServiceProviderExt.Empty) 
        => Interlocked.Exchange(ref RunningActionCountValue, runningActionCount);

    public override void Register(UIAction action)
    { }
}
