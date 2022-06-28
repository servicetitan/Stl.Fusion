using Microsoft.EntityFrameworkCore;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Operations;

public class DbOperationLogReader<TDbContext> : DbTenantWorkerBase<TDbContext>
    where TDbContext : DbContext
{
    public record Options
    {
        public TimeSpan MaxCommitDuration { get; init; } = TimeSpan.FromSeconds(1);
        public int BatchSize { get; init; } = 256;
        public RandomTimeSpan UnconditionalCheckPeriod { get; init; } = TimeSpan.FromSeconds(0.25).ToRandom(0.1);
        public RetryDelaySeq RetryDelays { get; init; } = (TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
    }

    protected Options Settings { get; }
    protected AgentInfo AgentInfo { get; }
    protected IOperationCompletionNotifier OperationCompletionNotifier { get; }
    protected IDbOperationLogChangeTracker<TDbContext>? OperationLogChangeTracker { get;  }
    protected IDbOperationLog<TDbContext> DbOperationLog { get; }
    protected override IReadOnlyMutableDictionary<Symbol, Tenant> TenantSet => TenantRegistry.AccessedTenants;

    public DbOperationLogReader(Options settings, IServiceProvider services)
        : base(services)
    {
        Settings = settings;
        AgentInfo = services.GetRequiredService<AgentInfo>();
        OperationLogChangeTracker = services.GetService<IDbOperationLogChangeTracker<TDbContext>>();
        OperationCompletionNotifier = services.GetRequiredService<IOperationCompletionNotifier>();
        DbOperationLog = services.GetRequiredService<IDbOperationLog<TDbContext>>();
    }

    protected override Task RunInternal(Tenant tenant, CancellationToken cancellationToken)
    {
        var maxKnownCommitTime = Clocks.SystemClock.Now;
        var minCommitTime = (maxKnownCommitTime - Settings.MaxCommitDuration).ToDateTime();
        var lastCount = 0L;

        var activitySource = GetType().GetActivitySource();
        var runChain = new AsyncChain($"Read({tenant.Id})", async cancellationToken1 => {
            // Fetching potentially new operations
            var operations = await DbOperationLog
                .ListNewlyCommitted(tenant, minCommitTime, Settings.BatchSize, cancellationToken1)
                .ConfigureAwait(false);

            // Processing them
            var tasks = new Task[operations.Count];
            for (var i = 0; i < operations.Count; i++) {
                var operation = operations[i];
                var isLocal = StringComparer.Ordinal.Equals(operation.AgentId, AgentInfo.Id.Value);
                // Local completions are invoked by TransientOperationScopeProvider
                // _inside_ the command processing pipeline. Trying to trigger them here
                // means a tiny chance of running them _outside_ of command processing
                // pipeline, which makes it possible to see command completing
                // prior to its invalidation logic completion.
                tasks[i] = isLocal
                    ? Task.CompletedTask // Skips local operation!
                    : OperationCompletionNotifier.NotifyCompleted(operation);
                var commitTime = operation.CommitTime.ToMoment();
                if (maxKnownCommitTime < commitTime)
                    maxKnownCommitTime = commitTime;
            }

            // Let's wait when all of these tasks complete, otherwise
            // we might end up creating too many tasks
            await Task.WhenAll(tasks).ConfigureAwait(false);
            lastCount = operations.Count;
        }).Trace(() => activitySource.StartActivity("Read")?.AddTag("TenantId", tenant.Id.Value), Log);

        var sleepChain = new AsyncChain("Sleep", cancellationToken1 => {
            if (lastCount == Settings.BatchSize)
                return Task.CompletedTask;
            if (OperationLogChangeTracker == null)
                return Clocks.CpuClock.Delay(Settings.UnconditionalCheckPeriod.Next(), cancellationToken1);
            return OperationLogChangeTracker
                .WaitForChanges(tenant.Id, cancellationToken1)
                .WithTimeout(Clocks.CpuClock, Settings.UnconditionalCheckPeriod.Next(), cancellationToken1);
        });

        var chain = runChain
            .RetryForever(Settings.RetryDelays, Clocks.CpuClock, Log)
            .Append(sleepChain)
            .Cycle();
        return chain.Start(cancellationToken);
    }
}
