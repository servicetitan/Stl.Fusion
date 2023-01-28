namespace Stl.Fusion;

public sealed class AnonymousComputed<T> : Computed<T>
{
    public AnonymousComputedSource<T> Source { get; }

    public AnonymousComputed(
        ComputedOptions options,
        AnonymousComputedSource<T> source, LTag version)
        : base(options, source, version)
        => Source = source;

    protected override void OnInvalidated()
    {
        Source.OnInvalidated(this);
        base.OnInvalidated();
    }
}