using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.EntityFramework.Npgsql.Operations;
using Stl.Fusion.EntityFramework.Operations;

namespace Stl.Fusion.EntityFramework.Npgsql;

public static class DbOperationsBuilderExt
{
    public static DbOperationsBuilder<TDbContext> AddNpgsqlOperationLogChangeTracking<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbContext>
        (this DbOperationsBuilder<TDbContext> dbOperations,
            Func<IServiceProvider, NpgsqlDbOperationLogChangeTrackingOptions<TDbContext>>? optionsFactory = null)
        where TDbContext : DbContext
    {
        var services = dbOperations.Services;
        services.AddSingleton(optionsFactory, _ => NpgsqlDbOperationLogChangeTrackingOptions<TDbContext>.Default);
        if (services.HasService<NpgsqlDbOperationLogChangeTracker<TDbContext>>())
            return dbOperations;

        services.AddSingleton(c => new NpgsqlDbOperationLogChangeTracker<TDbContext>(
            c.GetRequiredService<NpgsqlDbOperationLogChangeTrackingOptions<TDbContext>>(), c));
        services.AddHostedService(c =>
            c.GetRequiredService<NpgsqlDbOperationLogChangeTracker<TDbContext>>());
        services.AddAlias<
            IDbOperationLogChangeTracker<TDbContext>,
            NpgsqlDbOperationLogChangeTracker<TDbContext>>();

        // NpgsqlDbOperationLogChangeNotifier<TDbContext>
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IOperationCompletionListener,
                NpgsqlDbOperationLogChangeNotifier<TDbContext>>());
        return dbOperations;
    }
}
