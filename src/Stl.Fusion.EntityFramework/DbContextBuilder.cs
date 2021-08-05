using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.CommandR;
using Stl.DependencyInjection;
using Stl.Fusion.EntityFramework.Authentication;
using Stl.Fusion.EntityFramework.Extensions;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Fusion.Extensions;
using Stl.Fusion.Operations;
using Stl.IO;

namespace Stl.Fusion.EntityFramework
{
    public readonly struct DbContextBuilder<TDbContext>
        where TDbContext : DbContext
    {
        public IServiceCollection Services { get; }

        internal DbContextBuilder(IServiceCollection services) => Services = services;

        public DbContextBuilder<TDbContext> AddDbEntityResolver<TKey, TEntity>(
            Action<IServiceProvider, DbEntityResolver<TDbContext, TKey, TEntity>.Options>? entityResolverOptionsBuilder = null)
            where TKey : notnull
            where TEntity : class
        {
            Services.TryAddSingleton(c => {
                var options = new DbEntityResolver<TDbContext, TKey, TEntity>.Options();
                entityResolverOptionsBuilder?.Invoke(c, options);
                return options;
            });
            Services.TryAddSingleton<DbEntityResolver<TDbContext, TKey, TEntity>>();
            return this;
        }

        // Operations

        public DbContextBuilder<TDbContext> AddDbOperations(
            Action<IServiceProvider, DbOperationLogReader<TDbContext>.Options>? logReaderOptionsBuilder = null,
            Action<IServiceProvider, DbOperationLogTrimmer<TDbContext>.Options>? logTrimmerOptionsBuilder = null)
            => AddDbOperations<DbOperation>(logReaderOptionsBuilder, logTrimmerOptionsBuilder);

        public DbContextBuilder<TDbContext> AddDbOperations<TDbOperation>(
            Action<IServiceProvider, DbOperationLogReader<TDbContext>.Options>? logReaderOptionsBuilder = null,
            Action<IServiceProvider, DbOperationLogTrimmer<TDbContext>.Options>? logTrimmerOptionsBuilder = null)
            where TDbOperation : DbOperation, new()
        {
            // Common services
            Services.TryAddSingleton<IDbOperationLog<TDbContext>, DbOperationLog<TDbContext, TDbOperation>>();

            // DbOperationScope & its CommandR handler
            Services.TryAddTransient<DbOperationScope<TDbContext>>();
            Services.TryAddSingleton<DbOperationScopeProvider<TDbContext>>();
            Services.AddCommander().AddHandlers<DbOperationScopeProvider<TDbContext>>();

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

        public DbContextBuilder<TDbContext> AddFileBasedDbOperationLogChangeTracking(PathString filePath)
            => AddFileBasedDbOperationLogChangeTracking((_, o) => { o.FilePath = filePath; });

        public DbContextBuilder<TDbContext> AddFileBasedDbOperationLogChangeTracking(
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

        public DbContextBuilder<TDbContext> AddDbAuthentication(
            Action<IServiceProvider, DbAuthService<TDbContext>.Options>? authServiceOptionsBuilder = null,
            Action<IServiceProvider, DbSessionInfoTrimmer<TDbContext>.Options>? sessionInfoTrimmerOptionsBuilder = null,
            Action<IServiceProvider, DbEntityResolver<TDbContext, long, DbUser>.Options>? userEntityResolverOptionsBuilder = null)
            => AddDbAuthentication<DbUser, DbSessionInfo>(
                authServiceOptionsBuilder,
                sessionInfoTrimmerOptionsBuilder,
                userEntityResolverOptionsBuilder);

        public DbContextBuilder<TDbContext> AddDbAuthentication<TDbUser, TDbSessionInfo>(
            Action<IServiceProvider, DbAuthService<TDbContext>.Options>? authServiceOptionsBuilder = null,
            Action<IServiceProvider, DbSessionInfoTrimmer<TDbContext>.Options>? sessionInfoTrimmerOptionsBuilder = null,
            Action<IServiceProvider, DbEntityResolver<TDbContext, long, TDbUser>.Options>? userEntityResolverOptionsBuilder = null,
            Action<IServiceProvider, DbEntityResolver<TDbContext, string, TDbSessionInfo>.Options>? sessionEntityResolverOptionsBuilder = null)
            where TDbSessionInfo : DbSessionInfo, new()
            where TDbUser : DbUser, new()
        {
            if (!Services.HasService<DbOperationScope<TDbContext>>())
                throw Errors.NoOperationsFrameworkServices();

            // DbAuthService
            Services.TryAddSingleton(c => {
                var options = new DbAuthService<TDbContext>.Options();
                authServiceOptionsBuilder?.Invoke(c, options);
                return options;
            });
            Services.AddFusion(fusion => {
                fusion.AddAuthentication(fusionAuth => {
                    fusionAuth.AddServerSideAuthService<DbAuthService<TDbContext>>();
                });
            });

            // Repositories and entity resolvers
            Services.TryAddSingleton<IDbUserRepo<TDbContext>, DbUserRepo<TDbContext, TDbUser>>();
            Services.TryAddSingleton<IDbSessionInfoRepo<TDbContext>, DbSessionInfoRepo<TDbContext, TDbSessionInfo>>();
            Services.AddDbContextServices<TDbContext>(dbContext => {
                dbContext.AddDbEntityResolver<long, TDbUser>((c, options) => {
                    options.QueryTransformer = q => q.Include(u => u.Identities);
                    userEntityResolverOptionsBuilder?.Invoke(c, options);
                });
                dbContext.AddDbEntityResolver<string, TDbSessionInfo>((c, options) => {
                    sessionEntityResolverOptionsBuilder?.Invoke(c, options);
                });
            });

            // DbSessionInfoTrimmer - hosted service!
            Services.TryAddSingleton(c => {
                var options = new DbSessionInfoTrimmer<TDbContext>.Options();
                sessionInfoTrimmerOptionsBuilder?.Invoke(c, options);
                return options;
            });
            Services.TryAddSingleton<DbSessionInfoTrimmer<TDbContext>>();
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
            AddDbEntityResolver<string, TDbKeyValue>();
            var fusion = Services.AddFusion();
            fusion.AddComputeService<IDbKeyValueStore<TDbContext>, DbKeyValueStore<TDbContext, TDbKeyValue>>();
            Services.TryAddSingleton<IKeyValueStore>(c => c.GetRequiredService<IDbKeyValueStore<TDbContext>>());

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
}
