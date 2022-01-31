using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;
using Stl.Fusion.EntityFramework.Redis.Operations;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Redis;

namespace Stl.Fusion.EntityFramework.Redis;

public static class DbContextBuilderExt
{
    // AddRedisDb

    public static IServiceCollection AddRedisDb<TDbContext>(
        this DbContextBuilder<TDbContext> dbContextBuilder,
        Func<IServiceProvider, string> configurationFactory,
        string? keyPrefix = null)
        where TDbContext : DbContext
        => dbContextBuilder.Services.AddRedisDb<TDbContext>(configurationFactory, keyPrefix);

    public static IServiceCollection AddRedisDb<TDbContext>(
        this DbContextBuilder<TDbContext> dbContextBuilder,
        string configuration,
        string? keyPrefix = null)
        where TDbContext : DbContext
        => dbContextBuilder.Services.AddRedisDb<TDbContext>(configuration, keyPrefix);

    public static IServiceCollection AddRedisDb<TDbContext>(
        this DbContextBuilder<TDbContext> dbContextBuilder,
        ConfigurationOptions configuration,
        string? keyPrefix = null)
        where TDbContext : DbContext
        => dbContextBuilder.Services.AddRedisDb<TDbContext>(configuration, keyPrefix);

    public static IServiceCollection AddRedisDb<TDbContext>(
        this DbContextBuilder<TDbContext> dbContextBuilder,
        IConnectionMultiplexer connectionMultiplexer,
        string? keyPrefix = null)
        where TDbContext : DbContext
        => dbContextBuilder.Services.AddRedisDb<TDbContext>(connectionMultiplexer, keyPrefix);

    // AddRedisOperationLogChangeTracking

    public static DbContextBuilder<TDbContext> AddRedisOperationLogChangeTracking<TDbContext>(
        this DbContextBuilder<TDbContext> dbContextBuilder,
        Action<IServiceProvider, RedisOperationLogChangeTrackingOptions<TDbContext>>? configureOptions = null)
        where TDbContext : DbContext
    {
        var services = dbContextBuilder.Services;
        services.TryAddSingleton(c => {
            var options = new RedisOperationLogChangeTrackingOptions<TDbContext>();
            configureOptions?.Invoke(c, options);
            return options;
        });

        // RedisOperationLogChangeTracker<TDbContext>
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
        return dbContextBuilder;
    }
}
