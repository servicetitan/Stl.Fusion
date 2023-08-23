namespace Stl.Fusion;

public interface IAnonymousComputed: IComputed
{
    public IAnonymousComputedSource Source { get; }
}

public sealed class AnonymousComputed<T> : Computed<T>, IAnonymousComputed
{
    IAnonymousComputedSource IAnonymousComputed.Source => Source;
    public AnonymousComputedSource<T> Source { get; }

    public AnonymousComputed(
        ComputedOptions options,
        AnonymousComputedSource<T> source, LTag version)
        : base(options, source, version)
    {
        Source = source;
        ComputedRegistry.Instance.PseudoRegister(this);
    }

    protected override void OnInvalidated()
    {
        ComputedRegistry.Instance.PseudoUnregister(this);
        CancelTimeouts();
        Source.OnInvalidated(this);
    }
}
