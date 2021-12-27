using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Stl.Fusion;

public interface IComputedState : IState, IDisposable, IHasDisposeStarted
{
    public new interface IOptions : IState.IOptions
    {
        IUpdateDelayer? UpdateDelayer { get; init; }
    }

    IUpdateDelayer UpdateDelayer { get; set; }
    Task UpdateTask { get; }
    CancellationToken DisposeToken { get; }
}

public interface IComputedState<T> : IState<T>, IComputedState
{ }

public abstract class ComputedState<T> : State<T>, IComputedState<T>
{
    public new record Options : State<T>.Options, IComputedState.IOptions
    {
        public IUpdateDelayer? UpdateDelayer { get; init; }
    }

    private volatile IUpdateDelayer _updateDelayer;
    private readonly CancellationTokenSource _disposeCts;

    protected ILogger Log { get; }

    public IUpdateDelayer UpdateDelayer {
        get => _updateDelayer;
        set => _updateDelayer = value;
    }

    public Task UpdateTask { get; private set; } = null!;
    public CancellationToken DisposeToken { get; }
    public bool IsDisposeStarted => DisposeToken.IsCancellationRequested;

    protected ComputedState(Options options, IServiceProvider services, bool initialize = true)
        : base(options, services, false)
    {
        Log = Services.GetService<ILoggerFactory>()?.CreateLogger(GetType()) ?? NullLogger.Instance;
        _disposeCts = new CancellationTokenSource();
        DisposeToken = _disposeCts.Token;
        _updateDelayer = options.UpdateDelayer ?? Services.GetRequiredService<IUpdateDelayer>();
        // ReSharper disable once VirtualMemberCallInConstructor
        if (initialize) Initialize(options);
    }

    protected override void Initialize(State<T>.Options options)
    {
        base.Initialize(options);
        UpdateTask = Task.Run(Update, CancellationToken.None);
    }

    // ~ComputedState() => Dispose();

    public virtual void Dispose()
    {
        if (DisposeToken.IsCancellationRequested)
            return;
        GC.SuppressFinalize(this);
        _disposeCts.CancelAndDisposeSilently();
    }

    protected virtual async Task Update()
    {
        var cancellationToken = DisposeToken;
        while (!cancellationToken.IsCancellationRequested) {
            try {
                var snapshot = Snapshot;
                var computed = snapshot.Computed;
                if (!computed.IsInvalidated())
                    await computed.WhenInvalidated(cancellationToken).ConfigureAwait(false);
                if (snapshot.UpdateCount != 0)
                    await UpdateDelayer.UpdateDelay(snapshot, cancellationToken).ConfigureAwait(false);
                if (!snapshot.WhenUpdated().IsCompleted)
                    await computed.Update(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                // Will break from "while" loop later if it's due to cancellationToken cancellation
            }
            catch (Exception e) {
                Log.LogError(e, "Failure inside Run()");
            }
        }
    }
}
