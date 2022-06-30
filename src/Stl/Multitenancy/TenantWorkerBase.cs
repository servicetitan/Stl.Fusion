namespace Stl.Multitenancy;

public abstract class TenantWorkerBase<TContext> : WorkerBase
{
    protected ITenantRegistry<TContext> TenantRegistry { get; }
    protected abstract IReadOnlyMutableDictionary<Symbol, Tenant> TenantSet { get; }

    protected TenantWorkerBase(
        ITenantRegistry<TContext> tenantRegistry, 
        CancellationTokenSource? stopTokenSource = null) 
        : base(stopTokenSource)
        => TenantRegistry = tenantRegistry;

    protected override async Task RunInternal(CancellationToken cancellationToken)
    {
        var tenantSet = TenantSet;
        var tasks = new Dictionary<Symbol, (CancellationTokenSource Cts, Task Task)>();
        var tasksToStop = new ConcurrentDictionary<Task, Unit>();
        try {
            while (!cancellationToken.IsCancellationRequested) {
                var whenChanged = tenantSet.WhenChanged; // We should read this property first 
                var tenants = tenantSet.Items;

                // Starting tasks for new tenants
                foreach (var (tenantId, tenant) in tenants) {
                    if (tasks.ContainsKey(tenantId))
                        continue;
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    var task = RunInternal(tenant, cancellationToken);
                    tasks.Add(tenantId, (cts, task));
                    tasksToStop.TryAdd(task, default);
                    _ = task.ContinueWith(
                        t => tasksToStop.TryRemove(t, out _), 
                        CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }

                // Stopping old tasks
                var removedTenantIds = new List<Symbol>();
                foreach (var (tenantId, (cts, _)) in tasks) {
                    if (tenants.ContainsKey(tenantId))
                        continue;
                    removedTenantIds.Add(tenantId);
                    cts.CancelAndDisposeSilently();
                }
                foreach (var tenantId in removedTenantIds)
                    tasks.Remove(tenantId);

                // Waiting for changes
                await whenChanged.WithFakeCancellation(cancellationToken).ConfigureAwait(false);
            }
        }
        finally {
            // Gracefully terminating
            foreach (var (_, (cts, _)) in tasks)
                cts.CancelAndDisposeSilently();
            await Task.WhenAll(tasksToStop.Keys).ConfigureAwait(false);
        }
    }

    protected abstract Task RunInternal(Tenant tenant, CancellationToken cancellationToken);
}
