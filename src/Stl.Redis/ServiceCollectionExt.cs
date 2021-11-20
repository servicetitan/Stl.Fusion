using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Stl.Redis;

public static class ServiceCollectionExt
{
    // Single RedisDb (resolved via RedisDb w/o TContext parameter)

    public static IServiceCollection AddRedisDb(this IServiceCollection services,
        Func<IServiceProvider, string> configurationFactory,
        string keyPrefix = "")
    {
        services.AddSingleton(c => {
            var configuration = configurationFactory.Invoke(c);
            var multiplexer = ConnectionMultiplexer.Connect(configuration);
            return new RedisDb(multiplexer, keyPrefix);
        });
        return services;
    }

    public static IServiceCollection AddRedisDb(this IServiceCollection services,
        string configuration,
        string keyPrefix = "")
    {
        services.AddSingleton(_ => {
            var multiplexer = ConnectionMultiplexer.Connect(configuration);
            return new RedisDb(multiplexer, keyPrefix);
        });
        return services;
    }

    public static IServiceCollection AddRedisDb(this IServiceCollection services,
        IConnectionMultiplexer connectionMultiplexer,
        string keyPrefix = "")
    {
        services.AddSingleton(new RedisDb(connectionMultiplexer, keyPrefix));
        return services;
    }

    // Multiple RedisDb-s (resolved via RedisDb<TContext>)

    public static IServiceCollection AddRedisDb<TContext>(
        this IServiceCollection services,
        Func<IServiceProvider, string> configurationFactory,
        string? keyPrefix = null)
    {
        keyPrefix ??= typeof(TContext).Name;
        services.AddSingleton(c => {
            var configuration = configurationFactory.Invoke(c);
            var multiplexer = ConnectionMultiplexer.Connect(configuration);
            return new RedisDb<TContext>(multiplexer, keyPrefix);
        });
        return services;
    }

    public static IServiceCollection AddRedisDb<TContext>(
        this IServiceCollection services,
        string configuration,
        string? keyPrefix = null)
    {
        keyPrefix ??= typeof(TContext).Name;
        services.AddSingleton(_ => {
            var multiplexer = ConnectionMultiplexer.Connect(configuration);
            return new RedisDb<TContext>(multiplexer, keyPrefix);
        });
        return services;
    }

    public static IServiceCollection AddRedisDb<TContext>(
        this IServiceCollection services,
        IConnectionMultiplexer connectionMultiplexer,
        string? keyPrefix = null)
    {
        keyPrefix ??= typeof(TContext).Name;
        services.AddSingleton(new RedisDb<TContext>(connectionMultiplexer, keyPrefix));
        return services;
    }
}
