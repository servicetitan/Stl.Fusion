using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.EntityFramework.Npgsql.Operations;
using Stl.Fusion.EntityFramework.Operations;

namespace Stl.Fusion.EntityFramework.Npgsql;

public static class DbOperationsBuilderExt
{
    public static DbOperationsBuilder<TDbContext> AddNpgsqlOperationLogChangeTracking<TDbContext>(
        this DbOperationsBuilder<TDbContext> dbOperations,
        Func<IServiceProvider, NpgsqlDbOperationLogChangeTrackingOptions<TDbContext>>? optionsFactory = null)
        where TDbContext : DbContext
    {
        var services = dbOperations.Services;
        var isConfigured = services.HasService<NpgsqlDbOperationLogChangeTracker<TDbContext>>();

        if (optionsFactory != null)
            services.AddSingleton(optionsFactory);
        if (isConfigured)
            return dbOperations;

        // NpgsqlDbOperationLogChangeTracker<TDbContext>
        services.TryAddSingleton<NpgsqlDbOperationLogChangeTrackingOptions<TDbContext>>();
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
        return dbOperations;
    }
}
