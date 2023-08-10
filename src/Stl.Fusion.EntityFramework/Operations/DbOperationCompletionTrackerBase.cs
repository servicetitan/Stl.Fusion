using Microsoft.EntityFrameworkCore;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Operations;

public abstract class DbOperationCompletionTrackerBase(IServiceProvider services) : WorkerBase
{
    private ILogger? _log;

    protected IServiceProvider Services { get; } = services;
    protected ConcurrentDictionary<Symbol, TenantWatcher>? TenantWatchers { get; set; } = new();
    protected ILogger Log => _log ??= Services.LogFor(GetType());

    protected override Task OnRun(CancellationToken cancellationToken)
        => TaskExt.NeverEndingTask.WaitAsync(cancellationToken);

    protected override async Task OnStop()
    {
        if (TenantWatchers == null)
            return;
        var tenantWatchers = TenantWatchers;
        TenantWatchers = null;
        await tenantWatchers.Values
            .Select(v => v.DisposeAsync().AsTask())
            .Collect()
            .ConfigureAwait(false);
    }

    // Protected methods

    protected abstract TenantWatcher CreateTenantWatcher(Symbol tenantId);

    // Nested types

    protected abstract class TenantWatcher : ProcessorBase
    {
        private TaskCompletionSource<Unit> _nextEventSource = null!;
        protected Tenant Tenant { get; }

        protected TenantWatcher(Tenant tenant)
        {
            Tenant = tenant;
            // ReSharper disable once VirtualMemberCallInConstructor
            ReplaceNextEventTask();
        }

        public Task WaitForChanges(CancellationToken cancellationToken)
        {
            lock (Lock) {
                var task = _nextEventSource;
                if (_nextEventSource.Task.IsCompleted)
                    ReplaceNextEventTask();
                return task.Task.WaitAsync(cancellationToken);
            }
        }

        protected void CompleteWaitForChanges()
        {
            lock (Lock)
                _nextEventSource.TrySetResult(default);
        }

        private void ReplaceNextEventTask()
            => _nextEventSource = TaskCompletionSourceExt.NewSynchronous<Unit>();
    }
}

public abstract class DbOperationCompletionTrackerBase<TDbContext, TOptions>(
        TOptions options,
        IServiceProvider services
        ) : DbOperationCompletionTrackerBase(services), IDbOperationLogChangeTracker<TDbContext>
    where TDbContext : DbContext
    where TOptions : DbOperationCompletionTrackingOptions, new()

{
    protected TOptions Options { get; init; } = options;
    protected ITenantRegistry<TDbContext> TenantRegistry { get; } = services.GetRequiredService<ITenantRegistry<TDbContext>>();

    public Task WaitForChanges(Symbol tenantId, CancellationToken cancellationToken = default)
    {
        var tenantWatchers = TenantWatchers;
        if (tenantWatchers == null)
            return TaskExt.NeverEndingTask;
        var tenantWatcher = tenantWatchers.GetOrAdd(tenantId,
            static (tenantId1, self) => self.CreateTenantWatcher(tenantId1),
            this);
        return tenantWatcher.WaitForChanges(cancellationToken);
    }
}
