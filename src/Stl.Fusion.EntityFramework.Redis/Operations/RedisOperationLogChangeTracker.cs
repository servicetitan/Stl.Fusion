using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Multitenancy;
using Stl.Redis;

namespace Stl.Fusion.EntityFramework.Redis.Operations;

public class RedisOperationLogChangeTracker<TDbContext> 
    : DbOperationCompletionTrackerBase<TDbContext, RedisOperationLogChangeTrackingOptions<TDbContext>>
    where TDbContext : DbContext
{
    protected RedisDb RedisDb { get; }

    public RedisOperationLogChangeTracker(
        RedisOperationLogChangeTrackingOptions<TDbContext> options, 
        IServiceProvider services) 
        : base(options, services)
    {
        RedisDb = services.GetService<RedisDb<TDbContext>>() ?? services.GetRequiredService<RedisDb>();
        var redisPub = RedisDb.GetPub<TDbContext>(Options.PubSubKeyFactory.Invoke(Tenant.Default));
        Log.LogInformation("Using pub/sub key = '{Key}'", redisPub.FullKey);
    }

    protected override DbOperationCompletionTrackerBase.TenantWatcher CreateTenantWatcher(Symbol tenantId) 
        => new TenantWatcher(this, tenantId);

    protected new class TenantWatcher : DbOperationCompletionTrackerBase.TenantWatcher
    {
        public TenantWatcher(RedisOperationLogChangeTracker<TDbContext> owner, Symbol tenantId) 
            : base(owner.TenantRegistry.Get(tenantId))
        {
            var agentInfo = owner.Services.GetRequiredService<AgentInfo>();
            var key = owner.Options.PubSubKeyFactory.Invoke(Tenant);

            var watchChain = new AsyncChain($"Watch({tenantId})", async cancellationToken => {
                var redisSub = owner.RedisDb.GetChannelSub(key);
                await using var _ = redisSub.ConfigureAwait(false);

                await redisSub.Subscribe().ConfigureAwait(false);
                while (!cancellationToken.IsCancellationRequested) {
                    var value = await redisSub.Messages
                        .ReadAsync(cancellationToken)
                        .ConfigureAwait(false);
                    if (!StringComparer.Ordinal.Equals(agentInfo.Id.Value, value))
                        CompleteWaitForChanges();
                }
            }).RetryForever(owner.Options.TrackerRetryDelays, owner.Log);

            _ = watchChain.RunIsolated(StopToken);
        }
    }
}
