using Microsoft.EntityFrameworkCore;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Operations;

public abstract class DbOperationCompletionTrackerBase : WorkerBase
{
    protected IServiceProvider Services { get; }
    protected ConcurrentDictionary<Symbol, TenantWatcher>? TenantWatchers { get; set; }
    protected ILogger Log { get; }

    protected DbOperationCompletionTrackerBase(IServiceProvider services)
    {
        Log = services.LogFor(GetType());
        Services = services;
        TenantWatchers = new();
    }

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
        private Task<Unit> _nextEventTask = null!;
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
                var task = _nextEventTask;
                if (_nextEventTask.IsCompleted)
                    ReplaceNextEventTask();
                return task.WaitAsync(cancellationToken);
            }
        }

        protected void CompleteWaitForChanges()
        {
            lock (Lock)
                TaskSource.For(_nextEventTask).TrySetResult(default);
        }

        private void ReplaceNextEventTask()
            => _nextEventTask = TaskSource.New<Unit>(false).Task;
    }
}

public abstract class DbOperationCompletionTrackerBase<TDbContext, TOptions> 
    : DbOperationCompletionTrackerBase, IDbOperationLogChangeTracker<TDbContext>
    where TDbContext : DbContext
    where TOptions : DbOperationCompletionTrackingOptions, new()

{
    protected TOptions Options { get; init; }
    protected ITenantRegistry<TDbContext> TenantRegistry { get; }

    protected DbOperationCompletionTrackerBase(TOptions options, IServiceProvider services)
        : base(services)
    {
        Options = options;
        TenantRegistry = services.GetRequiredService<ITenantRegistry<TDbContext>>();
    }

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
