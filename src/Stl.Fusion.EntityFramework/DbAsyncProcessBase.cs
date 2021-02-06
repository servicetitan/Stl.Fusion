using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.CommandR;
using Stl.Time;

namespace Stl.Fusion.EntityFramework
{
    public abstract class DbAsyncProcessBase<TDbContext> : AsyncProcessBase
        where TDbContext : DbContext
    {
        protected IServiceProvider Services { get; }
        protected IDbContextFactory<TDbContext> DbContextFactory { get; }
        protected IMomentClock Clock { get; }
        protected ILogger Log { get; }

        protected DbAsyncProcessBase(IServiceProvider services)
        {
            Services = services;
            DbContextFactory = services.GetRequiredService<IDbContextFactory<TDbContext>>();
            Clock = services.GetService<IMomentClock>() ?? SystemClock.Instance;
            var loggerFactory = services.GetService<ILoggerFactory>();
            Log = loggerFactory?.CreateLogger(GetType()) ?? NullLogger.Instance;
        }

        protected TDbContext CreateDbContext(bool readWrite = false)
            => DbContextFactory.CreateDbContext().ReadWrite(readWrite);

        protected Task<TDbContext> CreateCommandDbContextAsync(CancellationToken cancellationToken = default)
            => CreateCommandDbContextAsync(true, cancellationToken);
        protected Task<TDbContext> CreateCommandDbContextAsync(bool readWrite = true, CancellationToken cancellationToken = default)
        {
            var commandContext = CommandContext.GetCurrent();
            var operationScope = commandContext.Items.Get<DbOperationScope<TDbContext>>();
            return operationScope.CreateDbContextAsync(readWrite, cancellationToken);
        }

        protected object[] ComposeKey(params object[] components)
            => components;
    }
}
