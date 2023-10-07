namespace Stl.Fusion;

public interface IComputedState : IState, IDisposable, IHasWhenDisposed
{
    public static class DefaultOptions
    {
        public static bool MustFlowExecutionContext { get; set; } = false;
    }

    public interface IOptions : IState.IOptions
    {
        IUpdateDelayer? UpdateDelayer { get; init; }
        public bool MustFlowExecutionContext { get; init; }
    }

    IUpdateDelayer UpdateDelayer { get; set; }
    Task UpdateCycleTask { get; }
    CancellationToken DisposeToken { get; }
}

public interface IComputedState<T> : IState<T>, IComputedState;

public abstract class ComputedState<T> : State<T>, IComputedState<T>
{
    public new record Options : State<T>.Options, IComputedState.IOptions
    {
        public IUpdateDelayer? UpdateDelayer { get; init; }
        public bool MustFlowExecutionContext { get; init; } = IComputedState.DefaultOptions.MustFlowExecutionContext;
    }

    private volatile Computed<T>? _computingComputed;
    private volatile IUpdateDelayer _updateDelayer = null!;
    private volatile Task? _whenDisposed;
    private readonly CancellationTokenSource _disposeTokenSource;

    public IUpdateDelayer UpdateDelayer {
        get => _updateDelayer;
        set => _updateDelayer = value;
    }

    public CancellationToken DisposeToken { get; }
    public Task UpdateCycleTask { get; private set; } = null!;
    public Task? WhenDisposed => _whenDisposed;
    public override bool IsDisposed => _whenDisposed != null;

    protected ComputedState(Options settings, IServiceProvider services, bool initialize = true)
        : base(settings, services, false)
    {
        _disposeTokenSource = new CancellationTokenSource();
        DisposeToken = _disposeTokenSource.Token;

        // ReSharper disable once VirtualMemberCallInConstructor
        if (initialize)
            Initialize(settings);
    }

    protected override void Initialize(State<T>.Options settings)
    {
        base.Initialize(settings);
        var computedStateOptions = (Options)settings;
        _updateDelayer = computedStateOptions.UpdateDelayer ?? Services.GetRequiredService<IUpdateDelayer>();

        // Ideally we want to suppress execution context flow here,
        // because the Update is ~ a worker-style task.
        if (computedStateOptions.MustFlowExecutionContext) {
            UpdateCycleTask = UpdateCycle();
        }
        else {
            using var _ = ExecutionContextExt.SuppressFlow();
            UpdateCycleTask = Task.Run(UpdateCycle, DisposeToken);
        }
    }

    // ~ComputedState() => Dispose();

    public virtual void Dispose()
    {
        if (_whenDisposed != null)
            return;
        lock (Lock) {
            if (_whenDisposed != null)
                return;

            _whenDisposed = UpdateCycleTask ?? Task.CompletedTask;
        }
        GC.SuppressFinalize(this);
        _disposeTokenSource.CancelAndDisposeSilently();
    }

    protected virtual async Task UpdateCycle()
    {
        var cancellationToken = DisposeToken;
        while (!cancellationToken.IsCancellationRequested) {
            try {
                var snapshot = Snapshot;
                var computed = snapshot.Computed;
                if (!computed.IsInvalidated())
                    await computed.WhenInvalidated(cancellationToken).ConfigureAwait(false);
                if (snapshot.UpdateCount != 0)
                    await UpdateDelayer.Delay(snapshot.RetryCount, cancellationToken).ConfigureAwait(false);
                if (!snapshot.WhenUpdated().IsCompleted)
                    await computed.Update(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                // Will break from "while" loop later if it's due to cancellationToken cancellation
            }
            catch (Exception e) {
                Log.LogError(e, "Failure inside UpdateCycle()");
            }
        }
        Computed.Invalidate();
    }

    public override IComputed? GetExistingComputed()
    {
        lock (Lock)
            return _computingComputed ?? base.GetExistingComputed();
    }

    protected override StateBoundComputed<T> CreateComputed()
    {
        var computed = new StateBoundComputed<T>(ComputedOptions, this, VersionGenerator.NextVersion());
        lock (Lock)
            _computingComputed = computed;
        return computed;
    }

    protected override void OnSetSnapshot(StateSnapshot<T> snapshot, StateSnapshot<T>? prevSnapshot)
    {
        // This method is called inside lock (Lock)
        _computingComputed = null;
        base.OnSetSnapshot(snapshot, prevSnapshot);
    }
}
