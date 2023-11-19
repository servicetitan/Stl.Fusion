using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Multitenancy;
using Stl.Redis;

namespace Stl.Fusion.EntityFramework.Redis.Operations;

public class RedisOperationLogChangeNotifier<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbContext>
    : DbOperationCompletionNotifierBase<TDbContext, RedisOperationLogChangeTrackingOptions<TDbContext>>
    where TDbContext : DbContext
{
    protected RedisDb RedisDb { get; }
    protected ConcurrentDictionary<Symbol, RedisPub<TDbContext>> RedisPubCache { get; }

    public RedisOperationLogChangeNotifier(
        RedisOperationLogChangeTrackingOptions<TDbContext> options,
        IServiceProvider services)
        : base(options, services)
    {
        RedisDb = services.GetService<RedisDb<TDbContext>>() ?? services.GetRequiredService<RedisDb>();
        RedisPubCache = new();
        // ReSharper disable once VirtualMemberCallInConstructor
#pragma warning disable CA2214
        Log.LogInformation("Using pub/sub key = '{Key}'", GetRedisPub(Tenant.Default).FullKey);
#pragma warning restore CA2214
    }

    protected override async Task Notify(Tenant tenant)
    {
        var redisPub = GetRedisPub(tenant);
        await redisPub.Publish(AgentInfo.Id.Value).ConfigureAwait(false);
    }

    protected virtual RedisPub<TDbContext> GetRedisPub(Tenant tenant)
        => RedisPubCache.GetOrAdd(tenant.Id,
            static (_, state) => {
                var (self, tenant1) = state;
                var key = self.Options.PubSubKeyFactory.Invoke(tenant1);
                return self.RedisDb.GetPub<TDbContext>(key);
            },
            (this, tenant));
}
