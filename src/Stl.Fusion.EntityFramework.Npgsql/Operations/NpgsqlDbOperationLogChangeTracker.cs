using Microsoft.EntityFrameworkCore;
using Npgsql;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Npgsql.Operations;

public class NpgsqlDbOperationLogChangeTracker<TDbContext> 
    : DbOperationCompletionTrackerBase<TDbContext, NpgsqlDbOperationLogChangeTrackingOptions<TDbContext>>
    where TDbContext : DbContext
{
    public NpgsqlDbOperationLogChangeTracker(
        NpgsqlDbOperationLogChangeTrackingOptions<TDbContext> options, 
        IServiceProvider services) 
        : base(options, services) { }

    protected override DbOperationCompletionTrackerBase.TenantWatcher CreateTenantWatcher(Symbol tenantId) 
        => new TenantWatcher(this, tenantId);

    protected new class TenantWatcher : DbOperationCompletionTrackerBase.TenantWatcher
    {
        public TenantWatcher(NpgsqlDbOperationLogChangeTracker<TDbContext> owner, Symbol tenantId) 
            : base(owner.TenantRegistry.Get(tenantId))
        {
            var dbHub = owner.Services.DbHub<TDbContext>();
            var agentInfo = owner.Services.GetRequiredService<AgentInfo>();

            var watchChain = new AsyncChain($"Watch({tenantId})", async cancellationToken => {
                var dbContext = dbHub.CreateDbContext(Tenant);
                await using var _ = dbContext.ConfigureAwait(false);

                var database = dbContext.Database;
                await database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                var dbConnection = (NpgsqlConnection) database.GetDbConnection()!;
                dbConnection.Notification += (_, eventArgs) => {
                    if (eventArgs.Payload != agentInfo.Id)
                        CompleteWaitForChanges();
                };
                await dbContext.Database
                    .ExecuteSqlRawAsync($"LISTEN {owner.Options.ChannelName}", cancellationToken)
                    .ConfigureAwait(false);
                while (!cancellationToken.IsCancellationRequested)
                    await dbConnection.WaitAsync(cancellationToken).ConfigureAwait(false);
            }).RetryForever(owner.Options.TrackerRetryDelays, owner.Log);

            watchChain.RunIsolated(StopToken);
        }
    }
}
