using Stl.Fusion.Internal;

namespace Stl.Fusion;

public interface IStateSnapshot
{
    IState State { get; }
    IComputed Computed { get; }
    IComputed LastNonErrorComputed { get; }
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
    new Computed<T> Computed { get; }
    new Computed<T> LastNonErrorComputed { get; }
}

public class StateSnapshot<T> : IStateSnapshot<T>
{
    private TaskCompletionSource<Unit> WhenUpdatingSource { get; }
    private TaskCompletionSource<Unit> WhenUpdatedSource { get; }

    public IState<T> State { get; }
    public Computed<T> Computed { get; }
    public Computed<T> LastNonErrorComputed { get; }
    public int UpdateCount { get; }
    public int ErrorCount { get; }
    public int RetryCount { get; }

    IState IStateSnapshot.State => State;
    IComputed IStateSnapshot.Computed => Computed;
    IComputed IStateSnapshot.LastNonErrorComputed => LastNonErrorComputed;

    public StateSnapshot(IState<T> state, Computed<T> computed)
    {
        State = state;
        Computed = computed;
        LastNonErrorComputed = computed;
        WhenUpdatingSource = TaskCompletionSourceExt.New<Unit>();
        WhenUpdatedSource = TaskCompletionSourceExt.New<Unit>();
        UpdateCount = 0;
        ErrorCount = 0;
        RetryCount = 0;
    }

    public StateSnapshot(StateSnapshot<T> prevSnapshot, Computed<T> computed)
    {
        State = prevSnapshot.State;
        Computed = computed;
        WhenUpdatingSource = TaskCompletionSourceExt.New<Unit>();
        WhenUpdatedSource = TaskCompletionSourceExt.New<Unit>();
        var error = computed.Error;
        if (error == null) {
            LastNonErrorComputed = computed;
            UpdateCount = 1 + prevSnapshot.UpdateCount;
            ErrorCount = prevSnapshot.ErrorCount;
            RetryCount = 0;
        }
        else {
            var computedImpl = (IComputedImpl) computed;
            if (!computedImpl.IsTransientError(error)) {
                // Non-transient error
                LastNonErrorComputed = prevSnapshot.LastNonErrorComputed;
                UpdateCount = 1 + prevSnapshot.UpdateCount;
                ErrorCount = 1 + prevSnapshot.ErrorCount;
                RetryCount = 0;
            }
            else {
                // Transient error
                LastNonErrorComputed = prevSnapshot.LastNonErrorComputed;
                UpdateCount = 1 + prevSnapshot.UpdateCount;
                ErrorCount = 1 + prevSnapshot.ErrorCount;
                RetryCount = 1 + prevSnapshot.RetryCount;
            }
        }
    }

    public override string ToString()
        => $"{GetType().GetName()}({Computed}, [{UpdateCount} update(s) / {ErrorCount} failure(s)])";

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
