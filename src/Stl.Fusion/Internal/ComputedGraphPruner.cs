namespace Stl.Fusion.Internal;

public sealed class ComputedGraphPruner : WorkerBase
{
    public record Options
    {
        public static Options Default { get; set; } = new();

        public bool AutoActivate { get; init; } = true;
        public bool MustPruneRegistry { get; init; } = true;
        public RandomTimeSpan CheckPeriod { get; init; } = TimeSpan.FromMinutes(5).ToRandom(0.1);
        public RandomTimeSpan NextBatchDelay { get; init; } = TimeSpan.FromSeconds(0.1).ToRandom(0.25);
        public RetryDelaySeq RetryDelays { get; init; } = RetryDelaySeq.Exp(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10));
        public int BatchSize { get; init; } = FusionSettings.ComputedGraphPrunerBatchSize;
    }

    private readonly TaskCompletionSource<Unit> _whenActivatedSource;

    public Options Settings { get; init; }
    public IMomentClock Clock { get; init; }
    public ILogger Log { get; init; }

    public Task<Unit> WhenActivated => _whenActivatedSource.Task;

    public ComputedGraphPruner(Options settings, ILogger<ComputedGraphPruner>? log = null)
        : this(settings, MomentClockSet.Default, log) { }
    public ComputedGraphPruner(Options settings, MomentClockSet clocks, ILogger<ComputedGraphPruner>? log = null)
    {
        Settings = settings;
        Clock = clocks.CpuClock;
        Log = log ?? NullLogger<ComputedGraphPruner>.Instance;
        _whenActivatedSource = TaskCompletionSourceExt.New<Unit>();

        if (settings.AutoActivate)
            this.Start();
    }

    public ComputedGraphPruner(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Clock = services.Clocks().CpuClock;
        Log = services.LogFor(GetType());
        _whenActivatedSource = TaskCompletionSourceExt.New<Unit>();

        if (settings.AutoActivate)
            this.Start();
    }

    public Task PruneOnce(CancellationToken cancellationToken)
        => CreatePruneOnceChain().Run(cancellationToken);

    public AsyncChain CreatePruneOnceChain()
        => CreatePruneDisposedInstancesChain()
            .Append(CreatePruneEdgesChain());

    // Protected methods

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        var computedRegistry = ComputedRegistry.Instance;
        if (Settings.AutoActivate) {
            // This prevents race condition when two pruners are assigned at almost
            // the same time - they'll both may end up activate themselves here
            var oldGraphPruner = computedRegistry.GraphPruner;
            while (oldGraphPruner != this) {
                await oldGraphPruner.WhenActivated.ConfigureAwait(false);
                oldGraphPruner = computedRegistry.ChangeGraphPruner(this, oldGraphPruner);
            }
        }
        else if (computedRegistry.GraphPruner != this) {
            Log.LogWarning("Terminating: ComputedRegistry.Instance.GraphPruner != this");
            return;
        }
        _whenActivatedSource.TrySetResult(default);

        var chain = CreatePruneOnceChain()
            .AppendDelay(Settings.CheckPeriod, Clock)
            .RetryForever(Settings.RetryDelays, Clock)
            .CycleForever()
            .Log(Log);
        cancellationToken.ThrowIfCancellationRequested();
        await chain.Start(cancellationToken).ConfigureAwait(false);
    }

    // Private methods

    private AsyncChain CreatePruneDisposedInstancesChain()
    {
        var activitySource = GetType().GetActivitySource();
        return new AsyncChain(nameof(PruneDisposedInstances) + "()", PruneDisposedInstances)
            .Trace(() => activitySource.StartActivity(nameof(PruneDisposedInstances)), Log)
            .Silence();
    }

    private AsyncChain CreatePruneEdgesChain()
    {
        var activitySource = GetType().GetActivitySource();
        return new AsyncChain(nameof(PruneEdges) + "()", PruneEdges)
            .Trace(() => activitySource.StartActivity(nameof(PruneEdges)), Log)
            .Silence();
    }

    private async Task PruneDisposedInstances(CancellationToken cancellationToken)
    {
        var registry = ComputedRegistry.Instance;
        using var keyEnumerator = registry.Keys.GetEnumerator();
        var disposedCount = 0L;
        var remainingBatchCapacity = Settings.BatchSize;
        var batchCount = 0;
        while (keyEnumerator.MoveNext()) {
            if (--remainingBatchCapacity <= 0) {
                await Clock.Delay(Settings.NextBatchDelay.Next(), cancellationToken).ConfigureAwait(false);
                remainingBatchCapacity = Settings.BatchSize;
                batchCount++;
            }

            var computedInput = keyEnumerator.Current!;
            if (registry.Get(computedInput) is IComputedImpl c && c.IsConsistent() && computedInput.IsDisposed) {
                disposedCount++;
                c.Invalidate();
            }
        }
        if (disposedCount == 0)
            return;

        Log.LogInformation(
            "Removed {DisposedCount} instances originating from disposed compute services " +
            "in {BatchCount} batches (x {BatchSize})",
            disposedCount, batchCount + 1, Settings.BatchSize);
    }

    private async Task PruneEdges(CancellationToken ct)
    {
        var registry = ComputedRegistry.Instance;
        using var keyEnumerator = registry.Keys.GetEnumerator();
        var computedCount = 0L;
        var consistentCount = 0L;
        var edgeCount = 0L;
        var removedEdgeCount = 0L;
        var remainingBatchCapacity = Settings.BatchSize;
        var batchCount = 0;
        while (keyEnumerator.MoveNext()) {
            if (--remainingBatchCapacity <= 0) {
                await Clock.Delay(Settings.NextBatchDelay.Next(), ct).ConfigureAwait(false);
                remainingBatchCapacity = Settings.BatchSize;
                batchCount++;
            }

            var computedInput = keyEnumerator.Current!;
            computedCount++;
            if (registry.Get(computedInput) is IComputedImpl c && c.IsConsistent()) {
                consistentCount++;
                var (oldEdgeCount, newEdgeCount) = c.PruneUsedBy();
                edgeCount += oldEdgeCount;
                removedEdgeCount += oldEdgeCount - newEdgeCount;
            }
        }
        await Clock.Delay(Settings.NextBatchDelay.Next(), ct).ConfigureAwait(false);
        if (Settings.MustPruneRegistry)
            await registry.Prune().ConfigureAwait(false);

        Log.LogInformation(
            "Processed {ConsistentCount}/{ComputedCount} instances, " +
            "removed {RemovedEdgeCount}/{EdgeCount} \"used by\" edges, " +
            "in {BatchCount} batches (x {BatchSize})",
            consistentCount, computedCount, removedEdgeCount, edgeCount, batchCount + 1, Settings.BatchSize);
    }
}
