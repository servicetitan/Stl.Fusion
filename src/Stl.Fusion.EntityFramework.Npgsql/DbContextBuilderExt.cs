using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.EntityFramework.Npgsql.Operations;
using Stl.Fusion.EntityFramework.Operations;

namespace Stl.Fusion.EntityFramework.Npgsql;

public static class DbContextBuilderExt
{
    public static DbContextBuilder<TDbContext> AddNpgsqlOperationLogChangeTracking<TDbContext>(
        this DbContextBuilder<TDbContext> dbContextBuilder,
        Action<IServiceProvider, NpgsqlDbOperationLogChangeTrackingOptions<TDbContext>>? configureOptions = null)
        where TDbContext : DbContext
    {
        var services = dbContextBuilder.Services;
        services.TryAddSingleton(c => {
            var options = new NpgsqlDbOperationLogChangeTrackingOptions<TDbContext>();
            configureOptions?.Invoke(c, options);
            return options;
        });

        // NpgsqlDbOperationLogChangeTracker<TDbContext>
        services.TryAddSingleton<NpgsqlDbOperationLogChangeTracker<TDbContext>>();
        services.AddHostedService(c =>
            c.GetRequiredService<NpgsqlDbOperationLogChangeTracker<TDbContext>>());
        services.TryAddSingleton<IDbOperationLogChangeTracker<TDbContext>>(c =>
            c.GetRequiredService<NpgsqlDbOperationLogChangeTracker<TDbContext>>());

        // NpgsqlDbOperationLogChangeNotifier<TDbContext>
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IOperationCompletionListener,
                NpgsqlDbOperationLogChangeNotifier<TDbContext>>());
        return dbContextBuilder;
    }
}
