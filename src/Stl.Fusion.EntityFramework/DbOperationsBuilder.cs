using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.EntityFramework.Operations;

namespace Stl.Fusion.EntityFramework;

public readonly struct DbOperationsBuilder<TDbContext>
    where TDbContext : DbContext
{
    public DbContextBuilder<TDbContext> DbContext { get; }
    public IServiceCollection Services => DbContext.Services;

    internal DbOperationsBuilder(
        DbContextBuilder<TDbContext> dbContext,
        Action<DbOperationsBuilder<TDbContext>>? configure)
    {
        DbContext = dbContext;
        if (Services.HasService<IDbOperationLog<TDbContext>>()) {
            configure?.Invoke(this);
            return;
        }

        // Common services
        Services.TryAddSingleton<IDbOperationLog<TDbContext>, DbOperationLog<TDbContext, DbOperation>>();

        // DbOperationScope & its CommandR handler
        Services.TryAddSingleton<TransactionIdGenerator<TDbContext>>();
        Services.TryAddSingleton<DbOperationScope<TDbContext>.Options>();
        if (!Services.HasService<DbOperationScopeProvider<TDbContext>>()) {
            Services.AddSingleton<DbOperationScopeProvider<TDbContext>>();
            Services.AddCommander().AddHandlers<DbOperationScopeProvider<TDbContext>>();
        }

        // DbOperationLogReader - hosted service!
        Services.TryAddSingleton<DbOperationLogReader<TDbContext>.Options>();
        Services.TryAddSingleton<DbOperationLogReader<TDbContext>>();
        Services.AddHostedService(c => c.GetRequiredService<DbOperationLogReader<TDbContext>>());

        // DbOperationLogTrimmer - hosted service!
        Services.TryAddSingleton<DbOperationLogTrimmer<TDbContext>.Options>();
        Services.TryAddSingleton<DbOperationLogTrimmer<TDbContext>>();
        Services.AddHostedService(c => c.GetRequiredService<DbOperationLogTrimmer<TDbContext>>());

        configure?.Invoke(this);
    }

    // Core settings

    public DbOperationsBuilder<TDbContext> SetDbOperationType<TDbOperation>()
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
        var isConfigured = Services.HasService<FileBasedDbOperationLogChangeTracker<TDbContext>>();

        if (optionsFactory != null)
            Services.AddSingleton(optionsFactory);
        if (isConfigured)
            return this;

        Services.TryAddSingleton(c => optionsFactory?.Invoke(c) ?? new());
        Services.TryAddSingleton<FileBasedDbOperationLogChangeTracker<TDbContext>>();
        Services.TryAddSingleton<IDbOperationLogChangeTracker<TDbContext>>(c =>
            c.GetRequiredService<FileBasedDbOperationLogChangeTracker<TDbContext>>());
        Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IOperationCompletionListener,
                FileBasedDbOperationLogChangeNotifier<TDbContext>>());
        return this;
    }
}
