using System.Data;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.CommandR.Internal;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.EntityFramework.Operations;

namespace Stl.Fusion.EntityFramework;

public readonly struct DbOperationsBuilder<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbContext>
    where TDbContext : DbContext
{
    public DbContextBuilder<TDbContext> DbContext { get; }
    public IServiceCollection Services => DbContext.Services;

    [RequiresUnreferencedCode(UnreferencedCode.Commander)]
    internal DbOperationsBuilder(
        DbContextBuilder<TDbContext> dbContext,
        Action<DbOperationsBuilder<TDbContext>>? configure)
    {
        DbContext = dbContext;
        var services = Services;
        if (services.HasService<IDbOperationLog<TDbContext>>()) {
            configure?.Invoke(this);
            return;
        }

        // Common services
        services.TryAddSingleton<IDbOperationLog<TDbContext>, DbOperationLog<TDbContext, DbOperation>>();

        // DbOperationScope & its CommandR handler
        services.TryAddSingleton<TransactionIdGenerator<TDbContext>>();
        services.TryAddSingleton<DbOperationScope<TDbContext>.Options>();
        if (!services.HasService<DbOperationScopeProvider<TDbContext>>()) {
            services.AddSingleton<DbOperationScopeProvider<TDbContext>>();
            services.AddCommander().AddHandlers<DbOperationScopeProvider<TDbContext>>();
        }

        // DbOperationLogReader - hosted service!
        services.TryAddSingleton<DbOperationLogReader<TDbContext>.Options>();
        services.TryAddSingleton<DbOperationLogReader<TDbContext>>();
        services.AddHostedService(c => c.GetRequiredService<DbOperationLogReader<TDbContext>>());

        // DbOperationLogTrimmer - hosted service!
        services.TryAddSingleton<DbOperationLogTrimmer<TDbContext>.Options>();
        services.TryAddSingleton<DbOperationLogTrimmer<TDbContext>>();
        services.AddHostedService(c => c.GetRequiredService<DbOperationLogTrimmer<TDbContext>>());

        configure?.Invoke(this);
    }

    // Core settings

    public DbOperationsBuilder<TDbContext> SetDbOperationType<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbOperation>()
        where TDbOperation : DbOperation, new()
    {
        Services.RemoveAll<IDbOperationLog<TDbContext>>();
        Services.AddSingleton<IDbOperationLog<TDbContext>, DbOperationLog<TDbContext, TDbOperation>>();
        return this;
    }

    public DbOperationsBuilder<TDbContext> ConfigureOperationScope(
        Func<IServiceProvider, DbOperationScope<TDbContext>.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }

    public DbOperationsBuilder<TDbContext> ConfigureOperationLogReader(
        Func<IServiceProvider, DbOperationLogReader<TDbContext>.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }

    public DbOperationsBuilder<TDbContext> ConfigureOperationLogTrimmer(
        Func<IServiceProvider, DbOperationLogTrimmer<TDbContext>.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }

    // DbIsolationLevelSelectors

    public DbOperationsBuilder<TDbContext> AddIsolationLevelSelector(
        Func<IServiceProvider, DbIsolationLevelSelector<TDbContext>> dbIsolationLevelSelector)
    {
        Services.AddSingleton(dbIsolationLevelSelector);
        return this;
    }

    public DbOperationsBuilder<TDbContext> AddIsolationLevelSelector(
        Func<IServiceProvider, CommandContext, IsolationLevel> dbIsolationLevelSelector)
    {
        Services.AddSingleton(c => new DbIsolationLevelSelector<TDbContext>(
            context => dbIsolationLevelSelector.Invoke(c, context)));
        return this;
    }

    public DbOperationsBuilder<TDbContext> TryAddIsolationLevelSelector(
        Func<IServiceProvider, DbIsolationLevelSelector<TDbContext>> dbIsolationLevelSelector)
    {
        Services.TryAddSingleton(dbIsolationLevelSelector);
        return this;
    }

    public DbOperationsBuilder<TDbContext> TryAddIsolationLevelSelector(
        Func<IServiceProvider, CommandContext, IsolationLevel> dbIsolationLevelSelector)
    {
        Services.TryAddSingleton(c => new DbIsolationLevelSelector<TDbContext>(
            context => dbIsolationLevelSelector.Invoke(c, context)));
        return this;
    }

    // File-based operation log change tracking

    public DbOperationsBuilder<TDbContext> AddFileBasedOperationLogChangeTracking(
        Func<IServiceProvider, FileBasedDbOperationLogChangeTrackingOptions<TDbContext>>? optionsFactory = null)
    {
        var services = Services;
        services.AddSingleton(optionsFactory, _ => FileBasedDbOperationLogChangeTrackingOptions<TDbContext>.Default);
        if (services.HasService<FileBasedDbOperationLogChangeTracker<TDbContext>>())
            return this;

        services.AddSingleton(c => new FileBasedDbOperationLogChangeTracker<TDbContext>(
            c.GetRequiredService<FileBasedDbOperationLogChangeTrackingOptions<TDbContext>>(), c));
        services.AddAlias<
            IDbOperationLogChangeTracker<TDbContext>,
            FileBasedDbOperationLogChangeTracker<TDbContext>>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IOperationCompletionListener,
                FileBasedDbOperationLogChangeNotifier<TDbContext>>());
        return this;
    }
}
