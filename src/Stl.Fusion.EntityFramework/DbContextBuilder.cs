using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.CommandR;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Fusion.Operations;
using Stl.IO;

namespace Stl.Fusion.EntityFramework
{
    public readonly struct DbContextBuilder<TDbContext>
        where TDbContext : DbContext
    {
        public IServiceCollection Services { get; }

        internal DbContextBuilder(IServiceCollection services) => Services = services;

        public DbContextBuilder<TDbContext> AddDbEntityResolver<TKey, TEntity>()
            where TKey : notnull
            where TEntity : class
        {
            Services.TryAddSingleton<DbEntityResolver<TDbContext, TKey, TEntity>>();
            return this;
        }

        public DbContextBuilder<TDbContext> AddDbOperations(
            Action<IServiceProvider, DbOperationLogReader<TDbContext>.Options>? logReaderOptionsBuilder = null,
            Action<IServiceProvider, DbOperationLogTrimmer<TDbContext>.Options>? logTrimmerOptionsBuilder = null)
            => AddDbOperations<DbOperation>(logReaderOptionsBuilder, logTrimmerOptionsBuilder);

        public DbContextBuilder<TDbContext> AddDbOperations<TDbOperation>(
            Action<IServiceProvider, DbOperationLogReader<TDbContext>.Options>? logReaderOptionsBuilder = null,
            Action<IServiceProvider, DbOperationLogTrimmer<TDbContext>.Options>? logTrimmerOptionsBuilder = null)
            where TDbOperation : class, IOperation, new()
        {
            // Common services
            Services.TryAddSingleton<AgentInfo>();
            Services.TryAddSingleton<OperationCompletionNotifier.Options>();
            Services.TryAddSingleton<IOperationCompletionNotifier, OperationCompletionNotifier>();
            Services.TryAddSingleton<IDbOperationLog<TDbContext>, DbOperationLog<TDbContext, TDbOperation>>();

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

            // DbOperationScope & its CommandR handler
            Services.TryAddTransient<IDbOperationScope<TDbContext>, DbOperationScope<TDbContext>>();
            Services.TryAddSingleton<DbOperationScopeHandler<TDbContext>.Options>();
            Services.TryAddSingleton<DbOperationScopeHandler<TDbContext>>();
            Services.AddCommander().AddHandlers<DbOperationScopeHandler<TDbContext>>();
            return this;
        }

        public DbContextBuilder<TDbContext> AddFileBasedDbOperationLogChangeMonitor(
            PathString filePath)
        {
            Services.TryAddSingleton<IDbOperationLogChangeMonitor<TDbContext>>(_ => {
                var watcher = new FileSystemWatcher(filePath.DirectoryPath, filePath.FileName);
                return new FileBasedDbOperationLogChangeMonitor<TDbContext>(watcher);
            });
            return this;
        }

        public DbContextBuilder<TDbContext> AddFileBasedDbOperationLogChangeMonitor(
            PathString dirPath, string filters, bool includeSubdirectories = false)
        {
            Services.TryAddSingleton<IDbOperationLogChangeMonitor<TDbContext>>(_ => {
                var watcher = new FileSystemWatcher(dirPath, filters) {
                    IncludeSubdirectories = includeSubdirectories,
                };
                return new FileBasedDbOperationLogChangeMonitor<TDbContext>(watcher);
            });
            return this;
        }

        public DbContextBuilder<TDbContext> AddFileBasedDbOperationLogChangeNotifier(
            PathString filePath)
        {
            Services.TryAddSingleton(_ => new FileBasedDbOperationLogChangeNotifier<TDbContext>.Options() {
                FilePath = filePath
            });
            Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<
                    IOperationCompletionListener,
                    FileBasedDbOperationLogChangeNotifier<TDbContext>>());
            return this;
        }
    }
}
