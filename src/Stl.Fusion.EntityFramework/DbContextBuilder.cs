using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Authentication;
using Stl.Fusion.EntityFramework.Authentication;
using Stl.Fusion.EntityFramework.Extensions;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Fusion.Extensions;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework;

public readonly struct DbContextBuilder<TDbContext>
    where TDbContext : DbContext
{
    public IServiceCollection Services { get; }

    internal DbContextBuilder(IServiceCollection services)
    {
        Services = services;
        Services.TryAddSingleton<ITenantRegistry<TDbContext>, SingleTenantRegistry<TDbContext>>();
        Services.TryAddSingleton<IMultitenantDbContextFactory<TDbContext>, SingleTenantDbContextFactory<TDbContext>>();
        Services.TryAddSingleton<DbHub<TDbContext>>();
    }

    // Entity converters

    public DbContextBuilder<TDbContext> AddEntityConverter<TDbEntity, TEntity, TConverter>()
        where TDbEntity : class
        where TEntity : notnull
        where TConverter : class, IDbEntityConverter<TDbEntity, TEntity>
    {
        Services.TryAddSingleton<IDbEntityConverter<TDbEntity, TEntity>, TConverter>();
        return this;
    }

    // Entity resolvers

    public DbContextBuilder<TDbContext> AddEntityResolver<TKey, TDbEntity>(
        Func<IServiceProvider, DbEntityResolver<TDbContext, TKey, TDbEntity>.Options>? optionsFactory = null)
        where TKey : notnull
        where TDbEntity : class
    {
        Services.TryAddSingleton(c => optionsFactory?.Invoke(c) ?? new());
        Services.TryAddSingleton<
            IDbEntityResolver<TKey, TDbEntity>,
            DbEntityResolver<TDbContext, TKey, TDbEntity>>();
        return this;
    }

    public DbContextBuilder<TDbContext> AddEntityResolver<TKey, TDbEntity, TResolver>(
        Func<IServiceProvider, TResolver> resolverFactory)
        where TKey : notnull
        where TDbEntity : class
        where TResolver : class, IDbEntityResolver<TKey, TDbEntity>
    {
        Services.TryAddSingleton<IDbEntityResolver<TKey, TDbEntity>>(resolverFactory);
        return this;
    }

    // Operations

    public DbContextBuilder<TDbContext> AddOperations(
        Func<IServiceProvider, DbOperationLogReader<TDbContext>.Options>? logReaderOptionsFactory = null,
        Func<IServiceProvider, DbOperationLogTrimmer<TDbContext>.Options>? logTrimmerOptionsFactory = null)
        => AddOperations<DbOperation>(logReaderOptionsFactory, logTrimmerOptionsFactory);

    public DbContextBuilder<TDbContext> AddOperations<TDbOperation>(
        Func<IServiceProvider, DbOperationLogReader<TDbContext>.Options>? logReaderOptionsFactory = null,
        Func<IServiceProvider, DbOperationLogTrimmer<TDbContext>.Options>? logTrimmerOptionsFactory = null)
        where TDbOperation : DbOperation, new()
    {
        // Common services
        Services.TryAddSingleton<IDbOperationLog<TDbContext>, DbOperationLog<TDbContext, TDbOperation>>();

        // DbOperationScope & its CommandR handler
        Services.TryAddTransient<DbOperationScope<TDbContext>>();
        if (!Services.HasService<DbOperationScopeProvider<TDbContext>>()) {
            Services.AddSingleton<DbOperationScopeProvider<TDbContext>>();
            Services.AddCommander().AddHandlers<DbOperationScopeProvider<TDbContext>>();
        }

        // DbOperationLogReader - hosted service!
        Services.TryAddSingleton(c => logReaderOptionsFactory?.Invoke(c) ?? new());
        Services.TryAddSingleton<DbOperationLogReader<TDbContext>>();
        Services.AddHostedService(c => c.GetRequiredService<DbOperationLogReader<TDbContext>>());

        // DbOperationLogTrimmer - hosted service!
        Services.TryAddSingleton(c => logTrimmerOptionsFactory?.Invoke(c) ?? new());
        Services.TryAddSingleton<DbOperationLogTrimmer<TDbContext>>();
        Services.AddHostedService(c => c.GetRequiredService<DbOperationLogTrimmer<TDbContext>>());

        return this;
    }

    // File-based operation log change tracking

    public DbContextBuilder<TDbContext> AddFileBasedOperationLogChangeTracking(
        Func<IServiceProvider, FileBasedDbOperationLogChangeTrackingOptions<TDbContext>>? optionsFactory = null)
    {
        Services.TryAddSingleton(c => optionsFactory?.Invoke(c) ?? new());
        Services.TryAddSingleton<
            IDbOperationLogChangeTracker<TDbContext>,
            FileBasedDbOperationLogChangeTracker<TDbContext>>();
        Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IOperationCompletionListener,
                FileBasedDbOperationLogChangeNotifier<TDbContext>>());
        return this;
    }

    // Authentication

    public DbContextBuilder<TDbContext> AddAuthentication(
        Func<IServiceProvider, DbAuthService<TDbContext>.Options>? authServiceOptionsFactory = null,
        Func<IServiceProvider, DbSessionInfoTrimmer<TDbContext>.Options>? sessionInfoTrimmerOptionsFactory = null,
        Func<IServiceProvider, DbEntityResolver<TDbContext, long, DbUser<long>>.Options>? userEntityResolverOptionsFactory = null)
        => AddAuthentication<long>(
            authServiceOptionsFactory,
            sessionInfoTrimmerOptionsFactory,
            userEntityResolverOptionsFactory);

    public DbContextBuilder<TDbContext> AddAuthentication<TDbUserId>(
        Func<IServiceProvider, DbAuthService<TDbContext>.Options>? authServiceOptionsFactory = null,
        Func<IServiceProvider, DbSessionInfoTrimmer<TDbContext>.Options>? sessionInfoTrimmerOptionsFactory = null,
        Func<IServiceProvider, DbEntityResolver<TDbContext, TDbUserId, DbUser<TDbUserId>>.Options>? userEntityResolverOptionsFactory = null)
        where TDbUserId : notnull
        => AddAuthentication<DbSessionInfo<TDbUserId>, DbUser<TDbUserId>, TDbUserId>(
            authServiceOptionsFactory,
            sessionInfoTrimmerOptionsFactory,
            userEntityResolverOptionsFactory);

    public DbContextBuilder<TDbContext> AddAuthentication<TDbSessionInfo, TDbUser, TDbUserId>(
        Func<IServiceProvider, DbAuthService<TDbContext>.Options>? authServiceOptionsFactory = null,
        Func<IServiceProvider, DbSessionInfoTrimmer<TDbContext>.Options>? sessionInfoTrimmerOptionsFactory = null,
        Func<IServiceProvider, DbEntityResolver<TDbContext, TDbUserId, TDbUser>.Options>? userEntityResolverOptionsFactory = null,
        Func<IServiceProvider, DbEntityResolver<TDbContext, string, TDbSessionInfo>.Options>? sessionEntityResolverOptionsFactory = null)
        where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
        where TDbUser : DbUser<TDbUserId>, new()
        where TDbUserId : notnull
    {
        if (!Services.HasService<DbOperationScope<TDbContext>>())
            throw Errors.NoOperationsFrameworkServices();

        // DbUserIdHandler
        Services.AddSingleton<IDbUserIdHandler<TDbUserId>, DbUserIdHandler<TDbUserId>>();

        // DbAuthService
        Services.TryAddSingleton(c => authServiceOptionsFactory?.Invoke(c) ?? new());
        Services.AddFusion(fusion => {
            fusion.AddAuthentication(fusionAuth => {
                fusionAuth.AddAuthBackend<DbAuthService<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>>();
            });
        });

        // Repositories, entity resolvers & converters
        Services.TryAddSingleton<
            IDbUserRepo<TDbContext, TDbUser, TDbUserId>,
            DbUserRepo<TDbContext, TDbUser, TDbUserId>>();
        Services.TryAddSingleton<
            IDbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId>,
            DbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId>>();
        Services.AddDbContextServices<TDbContext>(dbContext => {
            dbContext.AddEntityConverter<TDbUser, User, DbUserConverter<TDbContext, TDbUser, TDbUserId>>();
            dbContext.AddEntityResolver<TDbUserId, TDbUser>(c => {
                var options = userEntityResolverOptionsFactory?.Invoke(c) ?? new();
                return options with {
                    QueryTransformer = query => options.QueryTransformer.Invoke(query)
                        .Include(u => u.Identities),
                };
            });
            dbContext.AddEntityConverter<TDbSessionInfo, SessionInfo, DbSessionInfoConverter<TDbContext, TDbSessionInfo, TDbUserId>>();
            dbContext.AddEntityResolver(sessionEntityResolverOptionsFactory);
        });

        // DbSessionInfoTrimmer - hosted service!
        Services.TryAddSingleton(c => sessionInfoTrimmerOptionsFactory?.Invoke(c) ?? new());
        Services.TryAddSingleton<
            DbSessionInfoTrimmer<TDbContext>,
            DbSessionInfoTrimmer<TDbContext, TDbSessionInfo, TDbUserId>>();
        Services.AddHostedService(c => c.GetRequiredService<DbSessionInfoTrimmer<TDbContext>>());
        return this;
    }

    // KeyValueStore

    public DbContextBuilder<TDbContext> AddKeyValueStore(
        Func<IServiceProvider, DbKeyValueTrimmer<TDbContext, DbKeyValue>.Options>? keyValueTrimmerOptionsFactory = null)
        => AddKeyValueStore<DbKeyValue>(keyValueTrimmerOptionsFactory);

    public DbContextBuilder<TDbContext> AddKeyValueStore<TDbKeyValue>(
        Func<IServiceProvider, DbKeyValueTrimmer<TDbContext, TDbKeyValue>.Options>? keyValueTrimmerOptionsFactory = null)
        where TDbKeyValue : DbKeyValue, new()
    {
        AddEntityResolver<string, TDbKeyValue>();
        var fusion = Services.AddFusion();
        fusion.AddComputeService<DbKeyValueStore<TDbContext, TDbKeyValue>>();
        Services.TryAddSingleton<IKeyValueStore>(c => c.GetRequiredService<DbKeyValueStore<TDbContext, TDbKeyValue>>());

        // DbKeyValueTrimmer - hosted service!
        Services.TryAddSingleton(c => keyValueTrimmerOptionsFactory?.Invoke(c) ?? new());
        Services.TryAddSingleton<DbKeyValueTrimmer<TDbContext, TDbKeyValue>>();
        Services.AddHostedService(c => c.GetRequiredService<DbKeyValueTrimmer<TDbContext, TDbKeyValue>>());
        return this;
    }
}
