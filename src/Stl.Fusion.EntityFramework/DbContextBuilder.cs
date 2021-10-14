using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Fusion.EntityFramework.Authentication;
using Stl.Fusion.EntityFramework.Extensions;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Fusion.Extensions;
using Stl.IO;

namespace Stl.Fusion.EntityFramework;

public readonly struct DbContextBuilder<TDbContext>
    where TDbContext : DbContext
{
    public IServiceCollection Services { get; }

    internal DbContextBuilder(IServiceCollection services)
        => Services = services;

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
        Action<IServiceProvider, DbEntityResolver<TDbContext, TKey, TDbEntity>.Options>? optionsBuilder = null)
        where TKey : notnull
        where TDbEntity : class
    {
        Services.TryAddSingleton(c => {
            var options = new DbEntityResolver<TDbContext, TKey, TDbEntity>.Options();
            optionsBuilder?.Invoke(c, options);
            return options;
        });
        Services.TryAddSingleton<
            IDbEntityResolver<TKey, TDbEntity>,
            DbEntityResolver<TDbContext, TKey, TDbEntity>>();
        return this;
    }

    public DbContextBuilder<TDbContext> AddEntityResolver<TKey, TDbEntity, TResolver>()
        where TKey : notnull
        where TDbEntity : class
        where TResolver : class, IDbEntityResolver<TKey, TDbEntity>
    {
        Services.TryAddSingleton<IDbEntityResolver<TKey, TDbEntity>, TResolver>();
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
        Action<IServiceProvider, DbOperationLogReader<TDbContext>.Options>? logReaderOptionsBuilder = null,
        Action<IServiceProvider, DbOperationLogTrimmer<TDbContext>.Options>? logTrimmerOptionsBuilder = null)
        => AddOperations<DbOperation>(logReaderOptionsBuilder, logTrimmerOptionsBuilder);

    public DbContextBuilder<TDbContext> AddOperations<TDbOperation>(
        Action<IServiceProvider, DbOperationLogReader<TDbContext>.Options>? logReaderOptionsBuilder = null,
        Action<IServiceProvider, DbOperationLogTrimmer<TDbContext>.Options>? logTrimmerOptionsBuilder = null)
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
        Services.TryAddSingleton(c => {
            var options = new DbOperationLogReader<TDbContext>.Options();
            logReaderOptionsBuilder?.Invoke(c, options);
            return options;
        });
        Services.TryAddSingleton<DbOperationLogReader<TDbContext>>();
        Services.AddHostedService(c => c.GetRequiredService<DbOperationLogReader<TDbContext>>());

        // DbOperationLogTrimmer - hosted service!
        Services.TryAddSingleton(c => {
            var options = new DbOperationLogTrimmer<TDbContext>.Options();
            logTrimmerOptionsBuilder?.Invoke(c, options);
            return options;
        });
        Services.TryAddSingleton<DbOperationLogTrimmer<TDbContext>>();
        Services.AddHostedService(c => c.GetRequiredService<DbOperationLogTrimmer<TDbContext>>());

        return this;
    }

    // File-based operation log change tracking

    public DbContextBuilder<TDbContext> AddFileBasedOperationLogChangeTracking(FilePath filePath)
        => AddFileBasedOperationLogChangeTracking((_, o) => { o.FilePath = filePath; });

    public DbContextBuilder<TDbContext> AddFileBasedOperationLogChangeTracking(
        Action<IServiceProvider, FileBasedDbOperationLogChangeTrackingOptions<TDbContext>>? configureOptions = null)
    {
        Services.TryAddSingleton(c => {
            var options = new FileBasedDbOperationLogChangeTrackingOptions<TDbContext>();
            configureOptions?.Invoke(c, options);
            return options;
        });
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
        Action<IServiceProvider, DbAuthService<TDbContext>.Options>? authServiceOptionsBuilder = null,
        Action<IServiceProvider, DbSessionInfoTrimmer<TDbContext>.Options>? sessionInfoTrimmerOptionsBuilder = null,
        Action<IServiceProvider, DbEntityResolver<TDbContext, long, DbUser<long>>.Options>? userEntityResolverOptionsBuilder = null)
        => AddAuthentication<long>(
            authServiceOptionsBuilder,
            sessionInfoTrimmerOptionsBuilder,
            userEntityResolverOptionsBuilder);

    public DbContextBuilder<TDbContext> AddAuthentication<TDbUserId>(
        Action<IServiceProvider, DbAuthService<TDbContext>.Options>? authServiceOptionsBuilder = null,
        Action<IServiceProvider, DbSessionInfoTrimmer<TDbContext>.Options>? sessionInfoTrimmerOptionsBuilder = null,
        Action<IServiceProvider, DbEntityResolver<TDbContext, TDbUserId, DbUser<TDbUserId>>.Options>? userEntityResolverOptionsBuilder = null)
        where TDbUserId : notnull
        => AddAuthentication<DbSessionInfo<TDbUserId>, DbUser<TDbUserId>, TDbUserId>(
            authServiceOptionsBuilder,
            sessionInfoTrimmerOptionsBuilder,
            userEntityResolverOptionsBuilder);

    public DbContextBuilder<TDbContext> AddAuthentication<TDbSessionInfo, TDbUser, TDbUserId>(
        Action<IServiceProvider, DbAuthService<TDbContext>.Options>? authServiceOptionsBuilder = null,
        Action<IServiceProvider, DbSessionInfoTrimmer<TDbContext>.Options>? sessionInfoTrimmerOptionsBuilder = null,
        Action<IServiceProvider, DbEntityResolver<TDbContext, TDbUserId, TDbUser>.Options>? userEntityResolverOptionsBuilder = null,
        Action<IServiceProvider, DbEntityResolver<TDbContext, string, TDbSessionInfo>.Options>? sessionEntityResolverOptionsBuilder = null)
        where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
        where TDbUser : DbUser<TDbUserId>, new()
        where TDbUserId : notnull
    {
        if (!Services.HasService<DbOperationScope<TDbContext>>())
            throw Errors.NoOperationsFrameworkServices();

        // DbUserIdHandler
        Services.AddSingleton<IDbUserIdHandler<TDbUserId>, DbUserIdHandler<TDbUserId>>();

        // DbAuthService
        Services.TryAddSingleton(c => {
            var options = new DbAuthService<TDbContext>.Options();
            authServiceOptionsBuilder?.Invoke(c, options);
            return options;
        });
        Services.AddFusion(fusion => {
            fusion.AddAuthentication(fusionAuth => {
                fusionAuth.AddServerSideAuthService<DbAuthService<TDbContext, TDbSessionInfo, TDbUser, TDbUserId>>();
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
            dbContext.AddEntityResolver<TDbUserId, TDbUser>((c, options) => {
                options.QueryTransformer = q => q.Include(u => u.Identities);
                userEntityResolverOptionsBuilder?.Invoke(c, options);
            });
            dbContext.AddEntityConverter<TDbSessionInfo, SessionInfo, DbSessionInfoConverter<TDbContext, TDbSessionInfo, TDbUserId>>();
            dbContext.AddEntityResolver<string, TDbSessionInfo>((c, options) => {
                sessionEntityResolverOptionsBuilder?.Invoke(c, options);
            });
        });

        // DbSessionInfoTrimmer - hosted service!
        Services.TryAddSingleton(c => {
            var options = new DbSessionInfoTrimmer<TDbContext>.Options();
            sessionInfoTrimmerOptionsBuilder?.Invoke(c, options);
            return options;
        });
        Services.TryAddSingleton<
            DbSessionInfoTrimmer<TDbContext>,
            DbSessionInfoTrimmer<TDbContext, TDbSessionInfo, TDbUserId>>();
        Services.AddHostedService(c => c.GetRequiredService<DbSessionInfoTrimmer<TDbContext>>());
        return this;
    }

    // KeyValueStore

    public DbContextBuilder<TDbContext> AddKeyValueStore(
        Action<IServiceProvider, DbKeyValueTrimmer<TDbContext, DbKeyValue>.Options>? keyValueTrimmerOptionsBuilder = null)
        => AddKeyValueStore<DbKeyValue>(keyValueTrimmerOptionsBuilder);

    public DbContextBuilder<TDbContext> AddKeyValueStore<TDbKeyValue>(
        Action<IServiceProvider, DbKeyValueTrimmer<TDbContext, TDbKeyValue>.Options>? keyValueTrimmerOptionsBuilder = null)
        where TDbKeyValue : DbKeyValue, new()
    {
        AddEntityResolver<string, TDbKeyValue>();
        var fusion = Services.AddFusion();
        fusion.AddComputeService<DbKeyValueStore<TDbContext, TDbKeyValue>>();
        Services.TryAddSingleton<IKeyValueStore>(c => c.GetRequiredService<DbKeyValueStore<TDbContext, TDbKeyValue>>());

        // DbKeyValueTrimmer - hosted service!
        Services.TryAddSingleton(c => {
            var options = new DbKeyValueTrimmer<TDbContext, TDbKeyValue>.Options();
            keyValueTrimmerOptionsBuilder?.Invoke(c, options);
            return options;
        });
        Services.TryAddSingleton<DbKeyValueTrimmer<TDbContext, TDbKeyValue>>();
        Services.AddHostedService(c => c.GetRequiredService<DbKeyValueTrimmer<TDbContext, TDbKeyValue>>());
        return this;
    }
}
