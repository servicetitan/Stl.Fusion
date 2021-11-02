using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.Redis;

public static class ServiceCollectionExt
{
    // Single RedisDb (resolved via RedisDb w/o TContext parameter)

    public static IServiceCollection AddRedisDb(this IServiceCollection services,
        Func<IServiceProvider, string> configurationFactory,
        string keyPrefix = "")
    {
        services.TryAddSingleton<RedisHub>();
        services.AddSingleton(c => {
            var redisHub = c.GetRequiredService<RedisHub>();
            var configuration = configurationFactory.Invoke(c);
            return new RedisDb(redisHub.GetMultiplexer(configuration), keyPrefix);
        });
        return services;
    }

    public static IServiceCollection AddRedisDb(this IServiceCollection services,
        string configuration,
        string keyPrefix = "")
    {
        services.TryAddSingleton<RedisHub>();
        services.AddSingleton(c => {
            var redisHub = c.GetRequiredService<RedisHub>();
            return new RedisDb(redisHub.GetMultiplexer(configuration), keyPrefix);
        });
        return services;
    }

    // Multiple RedisDb-s (resolved via RedisDb<TContext>)

    public static IServiceCollection AddRedisDb<TContext>(
        this IServiceCollection services,
        Func<IServiceProvider, string> configurationFactory,
        string? keyPrefix = null)
    {
        keyPrefix ??= typeof(TContext).Name;
        services.TryAddSingleton<RedisHub>();
        services.AddSingleton(c => {
            var redisHub = c.GetRequiredService<RedisHub>();
            var configuration = configurationFactory.Invoke(c);
            return new RedisDb<TContext>(redisHub.GetMultiplexer(configuration), keyPrefix);
        });
        return services;
    }

    public static IServiceCollection AddRedisDb<TContext>(
        this IServiceCollection services,
        string configuration,
        string? keyPrefix = null)
    {
        keyPrefix ??= typeof(TContext).Name;
        services.TryAddSingleton<RedisHub>();
        services.AddSingleton(c => {
            var redisHub = c.GetRequiredService<RedisHub>();
            return new RedisDb<TContext>(redisHub.GetMultiplexer(configuration), keyPrefix);
        });
        return services;
    }
}
