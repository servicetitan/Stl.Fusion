using Microsoft.EntityFrameworkCore;
using Stl.Multitenancy;
using Stl.OS;

namespace Stl.Fusion.EntityFramework.Operations;

public class DbOperationLogReader<TDbContext>(
        DbOperationLogReader<TDbContext>.Options settings,
        IServiceProvider services
        ) : DbTenantWorkerBase<TDbContext>(services)
    where TDbContext : DbContext
{
    public record Options
    {
        public TimeSpan MaxCommitAge { get; init; } = TimeSpan.FromMinutes(5);
        public TimeSpan MaxCommitDuration { get; init; } = TimeSpan.FromSeconds(1);
        public int MinBatchSize { get; init; } = 256;
        public int MaxBatchSize { get; init; } = 8192;
        public TimeSpan MinDelay { get; init; } = TimeSpan.FromMilliseconds(20);
        public RandomTimeSpan UnconditionalCheckPeriod { get; init; } = TimeSpan.FromSeconds(5).ToRandom(0.1);
        public RetryDelaySeq RetryDelays { get; init; } = RetryDelaySeq.Exp(1, 5);
    }

    protected Options Settings { get; } = settings;
    protected AgentInfo AgentInfo { get; } = services.GetRequiredService<AgentInfo>();
    protected IOperationCompletionNotifier OperationCompletionNotifier { get; }
        = services.GetRequiredService<IOperationCompletionNotifier>();
    protected IDbOperationLogChangeTracker<TDbContext>? OperationLogChangeTracker { get;  }
        = services.GetService<IDbOperationLogChangeTracker<TDbContext>>();
    protected IDbOperationLog<TDbContext> DbOperationLog { get; }
        = services.GetRequiredService<IDbOperationLog<TDbContext>>();
    protected override IReadOnlyMutableDictionary<Symbol, Tenant> TenantSet => TenantRegistry.AccessedTenants;

    protected override Task RunInternal(Tenant tenant, CancellationToken cancellationToken)
    {
        var maxKnownCommitTime = Clocks.SystemClock.Now;
        var batchSize = Settings.MinBatchSize;
        var lastOperationCount = 0;

        var activitySource = GetType().GetActivitySource();
        var runChain = new AsyncChain($"Read({tenant.Id})", async cancellationToken1 => {
            var now = Clocks.SystemClock.Now;

            // Adjusting maxKnownCommitTime to make sure we make progress no matter what
            var minMaxKnownCommitTime = now - Settings.MaxCommitAge;
            if (maxKnownCommitTime < minMaxKnownCommitTime) {
                Log.LogWarning("Read: shifting MaxCommitTime by {Delta}", minMaxKnownCommitTime - maxKnownCommitTime);
                maxKnownCommitTime = minMaxKnownCommitTime;
            }

            // Adjusting batch size
            batchSize = lastOperationCount == batchSize
                ? Math.Min(batchSize << 1, Settings.MaxBatchSize)
                : Settings.MinBatchSize;

            // Fetching potentially new operations
            var minCommitTime = (maxKnownCommitTime - Settings.MaxCommitDuration).ToDateTime();
            var operations = await DbOperationLog
                .ListNewlyCommitted(tenant, minCommitTime, batchSize, cancellationToken1)
                .ConfigureAwait(false);

            // Updating important stuff
            lastOperationCount = operations.Count;
            if (lastOperationCount == 0) {
                maxKnownCommitTime = now;
                return;
            }

            if (lastOperationCount == batchSize)
                Log.LogWarning("Read: fetched {Count}/{BatchSize} operation(s) (full batch), CommitTime >= {MinCommitTime}",
                    lastOperationCount, batchSize, minCommitTime);
            else
                Log.LogDebug("Read: fetched {Count}/{BatchSize} operation(s), CommitTime >= {MinCommitTime}",
                    lastOperationCount, batchSize, minCommitTime);

            var maxCommitTime = operations.Max(o => o.CommitTime).ToMoment();
            maxKnownCommitTime = Moment.Max(maxKnownCommitTime, maxCommitTime);

            // Run completion notifications:
            // Local completions are invoked by TransientOperationScopeProvider
            // _inside_ the command processing pipeline. Trying to trigger them here
            // means a tiny chance of running them _outside_ of command processing
            // pipeline, which makes it possible to see command completing
            // prior to its invalidation logic completion.
            var notifyTasks =
                from operation in operations
                let isLocal = StringComparer.Ordinal.Equals(operation.AgentId, AgentInfo.Id.Value)
                where !isLocal
                select OperationCompletionNotifier.NotifyCompleted(operation, null);
            await notifyTasks
                .Collect(HardwareInfo.GetProcessorCountFactor(64, 64))
                .ConfigureAwait(false);
        }).Trace(() => activitySource.StartActivity("Read").AddTenantTags(tenant), Log);

        var waitForChangesChain = new AsyncChain("WaitForChanges()", async cancellationToken1 => {
            if (lastOperationCount == batchSize) {
                await Clocks.CpuClock.Delay(Settings.MinDelay, cancellationToken1).ConfigureAwait(false);
                return;
            }

            var unconditionalCheckPeriod = Settings.UnconditionalCheckPeriod.Next();
            if (OperationLogChangeTracker == null) {
                var delayTask = Clocks.CpuClock.Delay(unconditionalCheckPeriod, cancellationToken1);
                await delayTask.ConfigureAwait(false);
                return;
            }

            var cts = cancellationToken1.CreateLinkedTokenSource();
            try {
                var notificationTask = OperationLogChangeTracker.WaitForChanges(tenant.Id, cts.Token);
                var delayTask = Clocks.CpuClock.Delay(unconditionalCheckPeriod, cts.Token);
                var completedTask = await Task.WhenAny(notificationTask, delayTask).ConfigureAwait(false);
                await completedTask.ConfigureAwait(false);
            }
            finally {
                cts.CancelAndDisposeSilently();
            }
        });

        var chain = runChain
            .RetryForever(Settings.RetryDelays, Clocks.CpuClock, Log)
            .Append(waitForChangesChain)
            .CycleForever()
            .Log(Log);

        return chain.Start(cancellationToken);
    }
}
