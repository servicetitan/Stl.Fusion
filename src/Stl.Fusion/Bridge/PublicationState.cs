namespace Stl.Fusion.Bridge;

public abstract class PublicationState
{
    public abstract IPublication UntypedPublication { get; }
    public abstract IComputed UntypedComputed { get; }
    public bool IsDisposed { get; }

    protected PublicationState(bool isDisposed)
    {
        IsDisposed = isDisposed;
    }
}

public class PublicationState<T> : PublicationState
{
    public IPublication<T> Publication { get; }
    public Computed<T> Computed { get; }
    public override IPublication UntypedPublication => Publication;
    public override IComputed UntypedComputed => Computed;

    public PublicationState(IPublication<T> publication, Computed<T> computed, bool isDisposed)
        : base(isDisposed)
    {
        Publication = publication;
        Computed = computed;
    }
}
