using Stl.Fusion.Internal;

namespace Stl.Fusion;

public interface IStateSnapshot
{
    IState State { get; }
    IComputed Computed { get; }
    IComputed LatestNonErrorComputed { get; }
    int UpdateCount { get; }
    int ErrorCount { get; }
    int RetryCount { get; }

    Task WhenInvalidated(CancellationToken cancellationToken = default);
    Task WhenUpdating();
    Task WhenUpdated();
}

public interface IStateSnapshot<T> : IStateSnapshot
{
    new IState<T> State { get; }
    new IComputed<T> Computed { get; }
    new IComputed<T> LatestNonErrorComputed { get; }
}

public class StateSnapshot<T> : IStateSnapshot<T>
{
    private TaskSource<Unit> WhenUpdatingSource { get; }
    private TaskSource<Unit> WhenUpdatedSource { get; }

    public IState<T> State { get; }
    public IComputed<T> Computed { get; }
    public IComputed<T> LatestNonErrorComputed { get; }
    public int UpdateCount { get; }
    public int ErrorCount { get; }
    public int RetryCount { get; }

    IState IStateSnapshot.State => State;
    IComputed IStateSnapshot.Computed => Computed;
    IComputed IStateSnapshot.LatestNonErrorComputed => LatestNonErrorComputed;

    public StateSnapshot(IState<T> state, IComputed<T> computed)
    {
        State = state;
        Computed = computed;
        LatestNonErrorComputed = computed;
        WhenUpdatingSource = TaskSource.New<Unit>(true);
        WhenUpdatedSource = TaskSource.New<Unit>(true);
        UpdateCount = 0;
        ErrorCount = 0;
        RetryCount = 0;
    }

    public StateSnapshot(StateSnapshot<T> prevSnapshot, IComputed<T> computed)
    {
        State = prevSnapshot.State;
        Computed = computed;
        WhenUpdatingSource = TaskSource.New<Unit>(true);
        WhenUpdatedSource = TaskSource.New<Unit>(true);
        var error = computed.Error;
        if (error == null) {
            LatestNonErrorComputed = computed;
            UpdateCount = 1 + prevSnapshot.UpdateCount;
            ErrorCount = prevSnapshot.ErrorCount;
            RetryCount = 0;
        }
        else {
            var computedImpl = (IComputedImpl) computed;
            if (!computedImpl.IsTransientError(error)) {
                // Non-transient error
                LatestNonErrorComputed = prevSnapshot.LatestNonErrorComputed;
                UpdateCount = 1 + prevSnapshot.UpdateCount;
                ErrorCount = 1 + prevSnapshot.ErrorCount;
                RetryCount = 0;
            }
            else {
                // Transient error
                LatestNonErrorComputed = prevSnapshot.LatestNonErrorComputed;
                UpdateCount = 1 + prevSnapshot.UpdateCount;
                ErrorCount = 1 + prevSnapshot.ErrorCount;
                RetryCount = 1 + prevSnapshot.RetryCount;
            }
        }
    }

    public override string ToString()
        => $"{GetType().Name}({Computed}, [{UpdateCount} update(s) / {ErrorCount} failure(s)])";

    public Task WhenInvalidated(CancellationToken cancellationToken = default)
        => Computed.WhenInvalidated(cancellationToken);
    public Task WhenUpdating() => WhenUpdatingSource.Task;
    public Task WhenUpdated() => WhenUpdatedSource.Task;

    protected internal void OnUpdating()
        => WhenUpdatingSource.TrySetResult(default);

    protected internal void OnUpdated()
    {
        WhenUpdatingSource.TrySetResult(default);
        WhenUpdatedSource.TrySetResult(default);
    }
}
