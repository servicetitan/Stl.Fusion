using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Fusion.EntityFramework.Redis.Operations;

namespace Stl.Fusion.EntityFramework.Redis;

public static class DbOperationsBuilderExt 
{
    public static DbOperationsBuilder<TDbContext> AddRedisOperationLogChangeTracking<TDbContext>(
        this DbOperationsBuilder<TDbContext> dbOperations,
        Func<IServiceProvider, RedisOperationLogChangeTrackingOptions<TDbContext>>? optionsFactory = null)
        where TDbContext : DbContext
    {
        var services = dbOperations.Services;
        var isConfigured = services.HasService<RedisOperationLogChangeTracker<TDbContext>>();

        if (optionsFactory != null)
            services.AddSingleton(optionsFactory);
        if (isConfigured)
            return dbOperations;

        // RedisOperationLogChangeTracker<TDbContext>
        services.TryAddSingleton<RedisOperationLogChangeTrackingOptions<TDbContext>>();
        services.TryAddSingleton<RedisOperationLogChangeTracker<TDbContext>>();
        services.AddHostedService(c =>
            c.GetRequiredService<RedisOperationLogChangeTracker<TDbContext>>());
        services.TryAddSingleton<IDbOperationLogChangeTracker<TDbContext>>(c =>
            c.GetRequiredService<RedisOperationLogChangeTracker<TDbContext>>());

        // RedisOperationLogChangeNotifier<TDbContext>
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IOperationCompletionListener,
                RedisOperationLogChangeNotifier<TDbContext>>());
        return dbOperations;
    }
}
