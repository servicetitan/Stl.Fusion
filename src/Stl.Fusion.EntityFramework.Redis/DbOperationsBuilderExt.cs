using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Fusion.EntityFramework.Redis.Operations;

namespace Stl.Fusion.EntityFramework.Redis;

public static class DbOperationsBuilderExt
{
    public static DbOperationsBuilder<TDbContext> AddRedisOperationLogChangeTracking<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbContext>
        (this DbOperationsBuilder<TDbContext> dbOperations,
            Func<IServiceProvider, RedisOperationLogChangeTrackingOptions<TDbContext>>? optionsFactory = null)
        where TDbContext : DbContext
    {
        var services = dbOperations.Services;
        services.AddSingleton(optionsFactory, _ => RedisOperationLogChangeTrackingOptions<TDbContext>.Default);
        if (services.HasService<RedisOperationLogChangeTracker<TDbContext>>())
            return dbOperations;

        services.AddSingleton(c => new RedisOperationLogChangeTracker<TDbContext>(
            c.GetRequiredService<RedisOperationLogChangeTrackingOptions<TDbContext>>(), c));
        services.AddHostedService(c =>
            c.GetRequiredService<RedisOperationLogChangeTracker<TDbContext>>());
        services.AddAlias<
            IDbOperationLogChangeTracker<TDbContext>,
            RedisOperationLogChangeTracker<TDbContext>>();

        // RedisOperationLogChangeNotifier<TDbContext>
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IOperationCompletionListener,
                RedisOperationLogChangeNotifier<TDbContext>>());
        return dbOperations;
    }
}
