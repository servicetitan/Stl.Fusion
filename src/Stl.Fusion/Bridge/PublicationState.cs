namespace Stl.Fusion.Bridge;

public abstract class PublicationState
{
    protected readonly TaskSource<Unit> WhenInvalidatedSource;
    protected readonly TaskSource<Unit> WhenOutdatedSource;

    public abstract IPublication UntypedPublication { get; }
    public abstract IComputed UntypedComputed { get; }
    public bool IsDisposed { get; }

    protected PublicationState(bool isDisposed)
    {
        IsDisposed = isDisposed;
        WhenInvalidatedSource = isDisposed ? TaskSource.For(TaskExt.NeverEndingUnitTask) : TaskSource.New<Unit>(true);
        WhenOutdatedSource = isDisposed ? TaskSource.For(TaskExt.NeverEndingUnitTask) : TaskSource.New<Unit>(true);
    }

    public Task WhenInvalidated() => WhenInvalidatedSource.Task;
    public Task WhenOutdated() => WhenOutdatedSource.Task;
}

public class PublicationState<T> : PublicationState
{
    public IPublication<T> Publication { get; }
    public IComputed<T> Computed { get; }
    public override IPublication UntypedPublication => Publication;
    public override IComputed UntypedComputed => Computed;

    public PublicationState(IPublication<T> publication, IComputed<T> computed, bool isDisposed)
        : base(isDisposed)
    {
        Publication = publication;
        Computed = computed;
        if (!isDisposed)
            computed.Invalidated += _ => WhenInvalidatedSource.TrySetResult(default);
    }

    public bool TryMarkOutdated()
    {
        if (IsDisposed)
            return false;
        if (!WhenOutdatedSource.TrySetResult(default))
            return false;
        // WhenInvalidatedSource result must be set
        // after setting WhenOutdatedSource result to
        // make sure the code awaiting for both events
        // can optimize for this case.
        WhenInvalidatedSource.TrySetResult(default);
        return true;
    }
}
